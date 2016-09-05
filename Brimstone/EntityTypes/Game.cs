using System;
using System.Collections.Generic;
using System.Linq;
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
		public int FirstPlayerNum { get; private set; }
		public bool SkipMulligan { get; private set; }

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
		public event EntityChangeEventDelegate OnEntityChanged;

		// Game clones n-tree traversal
		private static int SequenceNumber { get; set; }
		public int GameId { get; }
		public int Depth { get; } = 0;

		// Required by IEntity
		public Game(Game cloneFrom) : base(cloneFrom) {
			// Settings
			FirstPlayerNum = cloneFrom.FirstPlayerNum;
			SkipMulligan = cloneFrom.SkipMulligan;
			// Generate zones owned by game
			Zones = new Zones(this, this);
			_deathCheckQueue = new List<int>(cloneFrom._deathCheckQueue);
			// Update tree
			GameId = ++SequenceNumber;
			Depth = cloneFrom.Depth + 1;
			// Fuzzy hashing
			_gameHash = cloneFrom._gameHash;
			Changed = cloneFrom.Changed;
		}

		public Game(HeroClass Hero1, HeroClass Hero2, string Player1Name = "", string Player2Name = "", bool PowerHistory = false)
					: base(Cards.FromId("Game"), new Dictionary<GameTag, int> {
						{ GameTag.ZONE, (int) Brimstone.Zone.PLAY }
					}) {
			// Start Power log
			if (PowerHistory) {
				this.PowerHistory = new PowerHistory(this);
			}

			ActionQueue = new ActionQueue(this);
			ActiveTriggers = new TriggerManager(this);
			Entities = new EntityController(this);
			Environment = new Environment(this);
			_deathCheckQueue = new List<int>();

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

		// Action block indexing rules:
		// TRIGGER: 0 for normal entities; -1 for player if not specified; always -1 for Game; otherwise index in Triggers.cs
		// PLAY: Always 0
		// DEATHS: Always 0
		// POWER: Always -1
		// FATIGUE: Always 0
		// ATTACK: Always -1
		// JOUST: Always 0
		// RITUAL: Always 0
		public void ActionBlock(BlockType Type, IEntity Source, ActionGraph Actions, IEntity Target = null, int Index = -2) {
			ActionBlock(Type, Source, Actions.Unravel(), Target, Index);
		}

		public void ActionBlock(BlockType Type, IEntity Source, List<QueueAction> Actions, IEntity Target = null, int Index = -2) {
#if _GAME_DEBUG
			DebugLog.WriteLine("Queueing " + Type + " for " + Source.ShortDescription + " => " + (Target?.ShortDescription ?? "no target"));
#endif
			int index = Index != -2 ? Index : (Type == BlockType.POWER || Type == BlockType.ATTACK ? -1 : 0);
			var block = new BlockStart(Type, Source, Target, index);
			Queue(Source, new Actions.GameBlock(block, Actions));
		}

		public void OnBlockEmpty(BlockStart Block) {
#if _GAME_DEBUG
			DebugLog.WriteLine("Action block " + Block.Type + " for " + Entities[Block.Source].ShortDescription + " resolved");
#endif
			PowerHistory?.Add(new BlockEnd(Block.Type));
		}

		private List<int> _deathCheckQueue;
		public void OnQueueEmpty() {
#if _GAME_DEBUG
			DebugLog.WriteLine("Action queue resolved");
#endif
			// Don't do anything if the game state hasn't changed

			// Check if one of the other players is waiting for the other one to mulligan
			var step = Step;
			if (step == Step.BEGIN_MULLIGAN)
				foreach (var p in Players)
					if (p.MulliganState == MulliganState.WAITING) {
						ActiveTriggers.Queue(TriggerType.OnMulliganWaiting, p);
						return;
					}

			// Death checking phase
#if _GAME_DEBUG
			DebugLog.WriteLine("Death processing phase");
#endif
			foreach (var eId in _deathCheckQueue)
				((ICharacter)Entities[eId]).CheckForDeath();
			_deathCheckQueue.Clear();

			// Advance game step if necessary (probably setting off new triggers)
			var nextStep = NextStep;

			// Only advance to end turn when current player chooses to
			// TODO: Fix this displaying at wrong time when queue is not empty after death triggers are queued
			if (nextStep == Step.MAIN_END) {
#if _GAME_DEBUG
				DebugLog.WriteLine("Waiting for player to select next option");
#endif
			}
			else if (nextStep != step) {
#if _GAME_DEBUG
					DebugLog.WriteLine("Advancing game step from " + step + " to " + nextStep);
#endif
					Step = nextStep;
			}
		}

		public void Start(int FirstPlayer = 0, bool SkipMulligan = false) {
			// Override settings
			FirstPlayerNum = FirstPlayer;
			this.SkipMulligan = SkipMulligan;

			// Configure players
			foreach (var p in Players)
				p.Start();

			// TODO: Insert event call precisely here so our server can iterate all created entities

			// Set game state
			State = GameState.RUNNING;
			foreach (var p in Players)
				p.PlayState = PlayState.PLAYING;

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
			OnEntityCreated?.Invoke(this, entity);
		}

		public void EntityChanging(IEntity entity, GameTag tag, int oldValue, int newValue, int previousHash) {
			if (Settings.GameHashCaching)
				_changed = true;
		}

		public void EntityChanged(IEntity entity, GameTag tag, int oldValue, int newValue) {
			if (tag == GameTag.DAMAGE && ((ICharacter) entity).Health <= 0)
				_deathCheckQueue.Add(entity.Id);
			OnEntityChanged?.Invoke(this, entity, tag, oldValue, newValue);
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
