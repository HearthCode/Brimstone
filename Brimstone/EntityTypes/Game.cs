#define _GAME_DEBUG

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;

namespace Brimstone
{
	public class FuzzyGameComparer : IEqualityComparer<Game>
	{
		// Used when adding to and fetching from HashSet, and testing for equality
		public bool Equals(Game x, Game y) {
			if (ReferenceEquals(x, y))
				return true;
			if (Settings.UseGameHashForEquality)
				return x.FuzzyGameHash == y.FuzzyGameHash;
			return x.PowerHistory.EquivalentTo(y.PowerHistory);
		}

		public int GetHashCode(Game obj) {
			return obj.FuzzyGameHash;
		}
	}

	public partial class Game : Entity, IZoneController, IFormattable
	{
		// Game settings
		public int MaxMinionsOnBoard { get; private set; } = 7;

		public EntityController Entities;
		public TriggerManager ActiveTriggers;
		public Environment Environment;

		public Player[] Players { get; private set; } = new Player[2];
		public Player Player1 {
			get {
				return Players[0];
			}
			set {
				Players[0] = value;
			}
		}
		public Player Player2 {
			get {
				return Players[1];
			}
			set {
				Players[1] = value;
			}
		}

		// TODO: Other common set selectors
		public IEnumerable<ICharacter> Characters => Player1.Board.Concat(Player2.Board).Concat(new List<ICharacter> {Player1.Hero, Player2.Hero});

		public Zone<IPlayable> Setaside { get { return (Zone<IPlayable>) Zones[Brimstone.Zone.SETASIDE]; } }
		public Zone<Minion> Board { get { return (Zone<Minion>) Zones[Brimstone.Zone.PLAY]; } }
		public Zone<ICharacter> Graveyard { get { return null; } }
		public Zone<IPlayable> Hand { get { return null; } }
		public Zone<Spell> Secrets { get { return null; } }
		public Deck Deck { get { return null; } set { throw new NotImplementedException(); } }

		public Zones Zones { get; }

		public PowerHistory PowerHistory;
		public ActionQueue ActionQueue;

		// Game events (used for triggers and packet transmission)
		public delegate void EntityCreateEventDelegate(Game Game, IEntity Entity);
		public delegate void EntityChangeEventDelegate(Game Game, IEntity Entity, GameTag Tag, int OldValue, int NewValue);

		public event EntityCreateEventDelegate OnEntityCreated;
		public event EntityChangeEventDelegate OnEntityChanging;
		public event EntityChangeEventDelegate OnEntityChanged;

		// Game clones n-tree traversal
		private static int SequenceNumber { get; set; }
		public int GameId { get; }
		public int Depth { get; } = 0;

		// Required by IEntity
		public Game(Game cloneFrom) : base(cloneFrom) {
			// Generate zones owned by game
			Zones = new Zones(this, this);
			// Update tree
			GameId = ++SequenceNumber;
			Depth = cloneFrom.Depth + 1;
			// Fuzzy hashing
			_gameHash = cloneFrom._gameHash;
			Changed = cloneFrom.Changed;
		}

		public Game(HeroClass Hero1, HeroClass Hero2, string Player1Name = "", string Player2Name = "", bool PowerHistory = false)
					: base(Cards.FromId("Game"), new Dictionary<GameTag, int> {
						{ GameTag.ZONE, (int) Brimstone.Zone.PLAY },
						{ GameTag.STATE, (int) GameState.LOADING }
					}) {
			// Start Power log
			if (PowerHistory) {
				this.PowerHistory = new PowerHistory(this);
			}

			ActionQueue = new ActionQueue(this);
			ActiveTriggers = new TriggerManager(this);
			Entities = new EntityController(this);
			Environment = new Environment(this);

			// Generate zones owned by game
			Zones = new Zones(this, this);

			// Generate players and empty decks
			Player1 = new Player(Hero1, (Player1Name.Length > 0) ? Player1Name : "Player 1", 1);
			Player2 = new Player(Hero2, (Player2Name.Length > 0) ? Player2Name : "Player 2", 2);
			Add(Player1, Player1);
			Add(Player2, Player2);
			for (int i = 0; i < 2; i++) {
				Players[i].Deck = new Deck(this, Players[i].HeroClass, Players[i]);
			}

			// No parent or children
			GameId = ++SequenceNumber;

			// Fuzzy hashing
			Changed = false;
		}

		public IEntity Add(IEntity newEntity, IZoneController controller) {
			if (newEntity != null) {
				newEntity.ZoneController = controller;
				return Entities.Add(newEntity);
			}
			return null;
		}

		public ActionResult Action(IEntity source, ActionGraph g) {
			return ActionQueue.Run(source, g);
		}

		public void Queue(IEntity source, ActionGraph g) {
			ActionQueue.EnqueueDeferred(source, g);
		}

		public void Queue(IEntity source, List<QueueAction> l) {
			ActionQueue.EnqueueDeferred(source, l);
		}

		public void Queue(IEntity source, QueueAction a) {
			ActionQueue.EnqueueDeferred(source, a);
		}

		public void Queue(Action a) {
			ActionQueue.EnqueueDeferred(a);
		}

		public void ActionBlock(BlockType Type, IEntity Source, ActionGraph Actions, IEntity Target = null, int Index = -1) {
			ActionBlock(Type, Source, Actions.Unravel(), Target, Index);
		}

		public void ActionBlock(BlockType Type, IEntity Source, List<QueueAction> Actions, IEntity Target = null, int Index = -1) {
#if _GAME_DEBUG
			DebugLog.WriteLine("Queueing " + Type + " for " + Source.ShortDescription + " => " + (Target?.ShortDescription ?? "no target"));
#endif
			var block = new BlockStart(Type, Source, Target, Index);
			Queue(Source, new GameBlock(block, Actions));
		}

		public void OnBlockEmpty(BlockStart Block) {
#if _GAME_DEBUG
			DebugLog.WriteLine("Action block " + Block.Type + " for " + Entities[Block.Source].ShortDescription + " resolved");
#endif
			PowerHistory?.Add(new BlockEnd(Block.Type));
		}

		public void OnQueueEmpty() {
#if _GAME_DEBUG
			DebugLog.WriteLine("Action queue resolved");
#endif
			// Death checking phase
			// TODO: Only do this if game state has changed
			foreach (var e in Characters)
				e?.CheckForDeath();

			// Advance game step if necessary (probably setting off new triggers)
			var nextStep = NextStep;
			var step = Step;
			if (nextStep != step) {
				// Only advance to end turn when current player chooses to
				if (nextStep != Step.MAIN_END)
				{
#if _GAME_DEBUG
					DebugLog.WriteLine("Advancing game step from " + step + " to " + nextStep);
#endif
					Step = nextStep;
				} else {
#if _GAME_DEBUG
					DebugLog.WriteLine("Waiting for player to select next option");
#endif
				}
			}

			if (nextStep == Step.MAIN_START_TRIGGERS) {
				// TODO: Do something exciting with turn start triggers for the current player here
				NextStep = Step.MAIN_START;
				Step = Step.MAIN_START;
				// TODO: DEATHs block for eg. Doomsayer
			}
		}


		public void Start(int FirstPlayer = 0, bool SkipMulligan = false) {
			// Shuffle player decks
			foreach (var p in Players)
				p.Deck.Shuffle();

			// Add players to board
			foreach (var p in Players)
				p.Zone = p.Board;

			// Generate player heroes
			// TODO: Add Start() parameters for non-default hero skins
			foreach (var p in Players)
				p.Hero = Add(new Hero(DefaultHero.For(p.HeroClass)), p) as Hero;

			// TODO: Insert event call precisely here so our server can iterate all created entities

			// Attach all game triggers
			// TODO: Clean these up into named functions
			ActiveTriggers.At<IEntity, IEntity>(TriggerType.GameStart, (Action<IEntity>)(_ =>
			{;
				// Pick a random starting player
				if (FirstPlayer == 0)
					this.FirstPlayer = Players[RNG.Between(0, 1)];
				else
					this.FirstPlayer = Players[FirstPlayer - 1];
				CurrentPlayer = this.FirstPlayer;

				// Set turn counter
				Turn = 1;

				// Draw cards
				foreach (var p in Players)
				{
					p.Draw((this.FirstPlayer == p ? 3 : 4));
					p.NumTurnsLeft = 1;

					// Give 2nd player the coin
					if (p != this.FirstPlayer)
						p.Give("The Coin");
				}

				// TODO: Set TIMEOUT for each player here if desired

				if (!SkipMulligan)
					NextStep = Step.BEGIN_MULLIGAN;
				else
					NextStep = Step.MAIN_READY;
			}));

			ActiveTriggers.At<IEntity, IEntity>(TriggerType.BeginMulligan, (Action<IEntity>) (_ =>
			{
				foreach (var p in Players)
					p.StartMulligan();
			}));
			ActiveTriggers.At<IEntity, IEntity>(TriggerType.PhaseMainReady, Actions.BeginTurn);

			ActiveTriggers.At<IEntity, IEntity>(TriggerType.PhaseMainNext, Actions.EndTurn);

			// Set game state
			State = GameState.RUNNING;
			foreach (var p in Players)
				p.Start();

			ActionQueue.ProcessAll();
			// TODO: POWERED_UP settings and stuff go here
		}

		public void EndTurn() {
			if (Player1.Choice != null || Player2.Choice != null)
				throw new ChoiceException();
#if _GAME_DEBUG
			DebugLog.WriteLine("Advancing game step from " + Step + " to " + NextStep);
#endif
			Step = Step.MAIN_END;
			ActionQueue.ProcessAll();
		}

		public void GameWon()
		{
			foreach (var p in Players) {
				if (p.PlayState != PlayState.LOSING) continue;

				p.PlayState = PlayState.LOST;
				p.Opponent.PlayState = PlayState.WON;
				End();
			}
		}

		public void End() {
			NextStep = Step.FINAL_WRAPUP;
			Step = Step.FINAL_WRAPUP;
			NextStep = Step.FINAL_GAMEOVER;
			Step = Step.FINAL_GAMEOVER;
			State = GameState.COMPLETE;

			// TODO: Gold reward state
		}

		public void EntityCreated(IEntity entity) {
			PowerHistory?.Add(new CreateEntity(entity));
			OnEntityCreated?.Invoke(this, entity);
			ActiveTriggers.Add(entity);
		}

		public void EntityChanging(IEntity entity, GameTag tag, int oldValue, int newValue, int previousHash) {
			if (Settings.GameHashCaching)
				_changed = true;
			OnEntityChanging?.Invoke(this, entity, tag, oldValue, newValue);
		}

		// TODO: Change this to a delegate event
		public void EntityChanged(IEntity entity, GameTag tag, int oldValue, int newValue) {
			PowerHistory?.Add(new TagChange(entity, tag, newValue));
			OnEntityChanged?.Invoke(this, entity, tag, oldValue, newValue);

			// Tag change triggers
			switch (tag) {
				case GameTag.STATE:
					if (newValue == (int)GameState.RUNNING)
						Game.ActiveTriggers.Queue(TriggerType.GameStart, entity);
					break;

				case GameTag.STEP:
					switch ((Step)newValue) {
						case Step.BEGIN_MULLIGAN:
							Game.ActiveTriggers.Queue(TriggerType.BeginMulligan, entity);
							break;
						case Step.MAIN_NEXT:
							Game.ActiveTriggers.Queue(TriggerType.PhaseMainNext, entity);
							break;
						case Step.MAIN_READY:
							Game.ActiveTriggers.Queue(TriggerType.PhaseMainReady, entity);
							break;
						case Step.MAIN_START_TRIGGERS:
							Game.ActiveTriggers.Queue(TriggerType.PhaseMainStartTriggers, entity);
							break;
						case Step.MAIN_START:
							Game.ActiveTriggers.Queue(TriggerType.PhaseMainStart, entity);
							break;
						case Step.MAIN_ACTION:
							Game.ActiveTriggers.Queue(TriggerType.PhaseMainAction, entity);
							break;
						case Step.MAIN_END:
							Game.ActiveTriggers.Queue(TriggerType.PhaseMainEnd, entity);
							break;
						case Step.MAIN_CLEANUP:
							Game.ActiveTriggers.Queue(TriggerType.PhaseMainCleanup, entity);
							break;
					}
					break;

				case GameTag.MULLIGAN_STATE:
					switch ((MulliganState)newValue) {
						case MulliganState.DEALING:
							Game.ActiveTriggers.Queue(TriggerType.DealMulligan, entity);
							break;
						case MulliganState.WAITING:
							Game.ActiveTriggers.Queue(TriggerType.MulliganWaiting, entity);
							break;
					}
					break;

				case GameTag.JUST_PLAYED:
					if (newValue == 1)
						Game.ActiveTriggers.Queue(TriggerType.Play, entity);
					break;

				case GameTag.DAMAGE:
					if (newValue != 0) { // TODO: Replace with checking if the value increased
						Game.ActiveTriggers.Queue(TriggerType.Damage, entity);
					}
					break;
			}
		}

		private int _gameHash = 0;
		private bool _changed = false;

		public bool Changed {
			get { return _changed; }
			set {
				int dummy;
				if (!value)
					dummy = FuzzyGameHash;
				else
					_changed = true;
			}
		}

		// Calculate a fuzzy hash for the whole game state
		// WARNING: The hash algorithm MUST be designed in such a way that the order
		// in which the entities are hashed doesn't matter
		public int FuzzyGameHash {
			get {
				// TODO: Take order-of-play semantics into account
				if (!Settings.GameHashCaching || _changed) {
					_gameHash = 0;
					// Hash board states (play zones) for both players in order, hash rest of game entities in any order
					foreach (var entity in Entities)
						if (entity.Zone.Type != Brimstone.Zone.PLAY || entity.ZonePosition == 0)
							_gameHash += entity.FuzzyHash;
						else
							_gameHash += (entity.ZoneController.Id * 8 + entity.ZonePosition) * entity.FuzzyHash;
					_changed = false;
				}
				return _gameHash;
			}
		}

		// Perform a fuzzy equivalence between two game states
		public bool EquivalentTo(Game game) {
			if (Settings.UseGameHashForEquality)
				return FuzzyGameHash == game.FuzzyGameHash;
			return PowerHistory.EquivalentTo(game.PowerHistory);
		}

		public override string ToString() {
			return ToString("G", null);
		}

		public string ToString(string format, IFormatProvider formatProvider) {
			if (format == null)
				format = "G";

			if (formatProvider != null) {
				ICustomFormatter formatter = formatProvider.GetFormat(this.GetType()) as ICustomFormatter;
				if (formatter != null)
					return formatter.Format(format, this, formatProvider);
			}

			string s = string.Format("Game hash: {0:x8}", FuzzyGameHash) + "\r\n";

			switch (format) {
				case "G":
					s += "Board state: ";
					var players = new List<Player> { Player1, Player2 };
					foreach (var player in players) {
						s += "Player " + player.Card.Id + " - ";
						s += "HAND: ";
						foreach (var entity in player.Hand) {
							s += entity.ToString() + ", ";
						}
						s += "PLAY: ";
						foreach (var entity in player.Board) {
							s += entity.ToString() + ", ";
						}
					}
					s = s.Substring(0, s.Length - 2) + "\nPower log: ";
					foreach (var item in PowerHistory)
						s += item + "\n";
					return s;

				case "S":
					s += "Player 1 (health " + Player1.Hero.Health + "): ";
					foreach (var e in Player1.Board)
						s += "[" + e.ZonePosition + ":" + e.Card.AbbrieviatedName + "](" + e.Damage + ") ";
					s += "\r\nPlayer 2 (health " + Player2.Hero.Health + "): ";
					foreach (var e in Player2.Board)
						s += "[" + e.ZonePosition + ":" + e.Card.AbbrieviatedName + "](" + e.Damage + ") ";
					s += "\r\n";
					foreach (var pa in PowerHistory.Skip(PowerHistory.Count() - 20))
						s += pa + "\r\n";
					return s;

				case "s":
					s += "Player 1 (health " + Player1.Hero.Health + "): ";
					foreach (var e in Player1.Board)
						s += "[" + e.ZonePosition + ":" + e.Card.AbbrieviatedName + "](" + e.Damage + ") ";
					s += "\r\nPlayer 2 (health " + Player2.Hero.Health + "): ";
					foreach (var e in Player2.Board)
						s += "[" + e.ZonePosition + ":" + e.Card.AbbrieviatedName + "](" + e.Damage + ") ";
					s += "\r\n";
					return s;
				default:
					return "Game (no format specified)";
			}
		}

		// TODO: Async cloning
		public List<Game> CloneStates(int qty) {
			var clones = new List<Game>();

			// Sequential cloning
			if (!Settings.ParallelClone) {
				for (int i = 0; i < qty; i++)
					clones.Add(CloneState());
				return clones;
			}

			// Parallel cloning
			Parallel.For(0, qty,
				() => new List<Game>(),
				(i, state, localSet) => { localSet.Add(CloneState()); return localSet; },
				(localSet) => { lock (clones) clones.AddRange(localSet); }
			);
			return clones;
		}

		// WARNING: The Game must not be changing during this,
		// ie. it is not thread-safe unless the game is inactive
		public Game CloneState() {
			var entities = new EntityController(Entities);
			Game game = entities.FindGame();
			// Set references to the new player proxies (no additional cloning)
			game.Player1 = entities.FindPlayer(1);
			game.Player2 = entities.FindPlayer(2);
			if (game.CurrentPlayer != null)
				game.CurrentPlayer = (CurrentPlayer.Id == game.Player1.Id ? game.Player1 : game.Player2);
			// Generate zones owned by game
			for (int i = 0; i < 2; i++) {
				game.Players[i].Deck = new Deck(game, Players[i].Deck.HeroClass, game.Players[i]);
			}
			// Clone queue, stack and events
			game.ActionQueue = ((ActionQueue)ActionQueue.Clone());
			game.ActionQueue.Attach(game);
			// Clone triggers
			game.ActiveTriggers = ((TriggerManager)ActiveTriggers.Clone());
			game.ActiveTriggers.Game = game;
			// Clone environment
			game.Environment = (Environment) Environment.Clone();
			// Link PowerHistory
			if (PowerHistory != null) {
				game.PowerHistory = new PowerHistory(game, this);
			}
#if _GAME_DEBUG
			DebugLog.WriteLine("Cloned game " + GameId + " => " + game.GameId);
#endif
			return game;
		}

		public override object Clone() {
			return new Game(this);
		}
	}
}
