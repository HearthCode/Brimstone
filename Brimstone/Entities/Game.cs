using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Brimstone.Exceptions;
using Brimstone.PowerActions;
using Brimstone.QueueActions;

namespace Brimstone.Entities
{
	public partial class Game : Entity, IZoneController, IFormattable
	{
		// Game settings
		public int MaxMinionsOnBoard { get; set; } = 7;

		// TODO: Replace internal set with a static factory which creates a game from an EntityController
		public EntityController Entities { get; internal set; }
		public TriggerManager ActiveTriggers { get; private set; }
		public Environment Environment { get; private set; }

		public Player[] Players { get; } = new Player[2];
		public Player Player1 {
			get {
				return Players[0];
			}
			private set {
				Players[0] = value;
			}
		}
		public Player Player2 {
			get {
				return Players[1];
			}
			private set {
				Players[1] = value;
			}
		}
		public int FirstPlayerNum { get; private set; }
		public bool SkipMulligan { get; private set; }

		// TODO: Other common set selectors
		public IEnumerable<Minion> Minions => Player1.Board.Concat(Player2.Board);
		public IEnumerable<ICharacter> Characters => Minions.Concat(new List<ICharacter> {Player1.Hero, Player2.Hero});

		public Zone<IPlayable> Setaside { get { return (Zone<IPlayable>) Zones[Brimstone.Zone.SETASIDE]; } }
		public Zone<Minion> Board { get { return (Zone<Minion>) Zones[Brimstone.Zone.PLAY]; } }
		public Zone<ICharacter> Graveyard { get { return null; } }
		public Zone<IPlayable> Hand { get { return null; } }
		public Zone<Spell> Secrets { get { return null; } }
		public Deck Deck { get { return null; } set { throw new NotImplementedException(); } }

		public Zones Zones { get; }

		public PowerHistory PowerHistory { get; private set; }
		public ActionQueue ActionQueue { get; private set; }

		// Game events (used for triggers and packet transmission)
		public event Action<Game, IEntity> OnEntityCreated;
		public event Action<Game, IEntity, GameTag, int, int> OnEntityChanged;

		// Game clones n-tree traversal
		private static int SequenceNumber { get; set; }
		public int GameId { get; }
		public int Depth { get; } = 0;

		// Required by IEntity
		internal Game(Game cloneFrom) : base(cloneFrom) {
			// Settings
			FirstPlayerNum = cloneFrom.FirstPlayerNum;
			SkipMulligan = cloneFrom.SkipMulligan;
			MaxMinionsOnBoard = cloneFrom.MaxMinionsOnBoard;
			// Generate zones owned by game
			Zones = new Zones(this, this);
			_deathCheckQueue = new HashSet<int>(cloneFrom._deathCheckQueue);
			// Update tree
			GameId = ++SequenceNumber;
			Depth = cloneFrom.Depth + 1;
			// Fuzzy hashing
			_gameHash = cloneFrom._gameHash;
			Changed = cloneFrom.Changed;
		}

		/// <summary>
		/// Create a new game
		/// </summary>
		/// <param name="Hero1"></param>
		/// <param name="Hero2"></param>
		/// <param name="Player1Name"></param>
		/// <param name="Player2Name"></param>
		/// <param name="PowerHistory"></param>
		/// <param name="ActionHistory"></param>
		public Game(HeroClass Hero1, HeroClass Hero2, string Player1Name = "", string Player2Name = "", bool PowerHistory = false, bool ActionHistory = false)
					: base(Cards.FromId("Game"), new Dictionary<GameTag, int> {
						{ GameTag.ZONE, (int) Brimstone.Zone.PLAY }
					}) {
			// Start Power log
			if (PowerHistory) {
				this.PowerHistory = new PowerHistory(this);
			}

			ActionQueue = new ActionQueue(this, ActionHistory);
			ActiveTriggers = new TriggerManager(this);
			Entities = new EntityController(this);
			Environment = new Environment(this);
			_deathCheckQueue = new HashSet<int>();

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

		public IEntity Add(IEntity newEntity, Player controller) {
			if (newEntity != null) {
				Entities.Add(newEntity);
				newEntity.Controller = controller;
				return newEntity;
			}
			return null;
		}

		public async Task<ActionResult> ActionAsync(IEntity source, ActionGraph g) {
			return await ActionQueue.RunAsync(source, g);
		}

		public async Task<ActionResult> ActionAsync(IEntity source, List<QueueAction> l) {
			return await ActionQueue.RunAsync(source, l);
		}

		public async Task<ActionResult> ActionAsync(IEntity source, QueueAction a) {
			return await ActionQueue.RunAsync(source, a);
		}

		public ActionResult Action(IEntity source, ActionGraph g) {
			return ActionQueue.Run(source, g);
		}

		public ActionResult Action(IEntity source, List<QueueAction> l) {
			return ActionQueue.Run(source, l);
		}

		public ActionResult Action(IEntity source, QueueAction a) {
			return ActionQueue.Run(source, a);
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
		public void QueueActionBlock(BlockType Type, IEntity Source, ActionGraph Actions, IEntity Target = null, int Index = -2) {
			QueueActionBlock(Type, Source, Actions.Unravel(), Target, Index);
		}

		public void QueueActionBlock(BlockType Type, IEntity Source, List<QueueAction> Actions, IEntity Target = null, int Index = -2) {
#if _GAME_DEBUG
			DebugLog.WriteLine("Game " + GameId + ": Queueing " + Type + " for " + Source.ShortDescription + " => " + (Target?.ShortDescription ?? "no target"));
#endif
			int index = Index != -2 ? Index : (Type == BlockType.POWER || Type == BlockType.ATTACK ? -1 : 0);
			var block = new BlockStart(Type, Source, Target, index);
			Queue(Source, new QueueActions.GameBlock(block, Actions));
		}

		public ActionResult RunActionBlock(BlockType Type, IEntity Source, ActionGraph Actions, IEntity Target = null, int Index = -2) {
			return RunActionBlockAsync(Type, Source, Actions.Unravel(), Target, Index).Result;
		}

		public ActionResult RunActionBlock(BlockType Type, IEntity Source, List<QueueAction> Actions, IEntity Target = null, int Index = -2) {
			return RunActionBlockAsync(Type, Source, Actions, Target, Index).Result;
		}

		public async Task<ActionResult> RunActionBlockAsync(BlockType Type, IEntity Source, ActionGraph Actions, IEntity Target = null, int Index = -2) {
			return await RunActionBlockAsync(Type, Source, Actions.Unravel(), Target, Index);
		}

		public async Task<ActionResult> RunActionBlockAsync(BlockType Type, IEntity Source, List<QueueAction> Actions, IEntity Target = null, int Index = -2) {
#if _GAME_DEBUG
			DebugLog.WriteLine("Game " + GameId + ": Running " + Type + " for " + Source.ShortDescription + " => " + (Target?.ShortDescription ?? "no target"));
#endif
			int index = Index != -2 ? Index : (Type == BlockType.POWER || Type == BlockType.ATTACK ? -1 : 0);
			var block = new BlockStart(Type, Source, Target, index);
			PowerHistory?.Add(block);
			ActionQueue.StartBlock(Source, Actions, block);
			return (await ActionQueue.ProcessBlockAsync())?.FirstOrDefault() ?? ActionResult.None;
		}

		internal void OnBlockEmpty(BlockStart Block) {
			OnBlockEmptyAsync(Block).Wait();
		}

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		internal async Task OnBlockEmptyAsync(BlockStart Block) {
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
#if _GAME_DEBUG
			DebugLog.WriteLine("Game " + GameId + ": Action block " + Block.Type + " for " + Entities[Block.Source].ShortDescription + " resolved");
#endif
			PowerHistory?.Add(new BlockEnd(Block.Type));

			if (Block.Type == BlockType.TRIGGER)
				ActiveTriggers.TriggerResolved();

			// Post-ATTACK or Post-final TRIGGER DEATHS block
			if (Block.Type == BlockType.ATTACK || (Block.Type == BlockType.TRIGGER && ActiveTriggers.QueuedTriggersCount == 0))
				RunDeathCreationStepIfNeeded();
		}

		private readonly HashSet<int> _deathCheckQueue;
		internal void OnQueueEmpty() {
#if _GAME_DEBUG
			DebugLog.WriteLine("Game " + GameId + ": Action queue resolved");
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

			// Advance game step if necessary (probably setting off new triggers)
			var nextStep = NextStep;

			// Only advance to end turn when current player chooses to
#if _GAME_DEBUG
			if (nextStep == Step.MAIN_END && ActionQueue.IsEmpty) {
				DebugLog.WriteLine("Game " + GameId + ": Waiting for player to select next option");
			}
#endif
			if (nextStep != step && nextStep != Step.MAIN_END) {
#if _GAME_DEBUG
					DebugLog.WriteLine("Game " + GameId + ": Advancing game step from " + step + " to " + nextStep);
#endif
					Step = nextStep;
			}
		}

		// Death checking phase
		internal void RunDeathCreationStepIfNeeded() {
#if _GAME_DEBUG
			DebugLog.WriteLine("Game " + GameId + ": Checking for death creation step");
#endif
			if (_deathCheckQueue.Count == 0)
				return;

			// We only have to check health because ToBeDestroyed cannot be reversed without the minion leaving play
			var dyingEntities =
				_deathCheckQueue.Where(
					id => ((ICharacter) Entities[id]).MortallyWounded && Entities[id].Zone.Type == Brimstone.Zone.PLAY)
					.Select(id => (ICharacter) Entities[id]).ToList();

			if (dyingEntities.Count > 0) {
#if _GAME_DEBUG
				DebugLog.WriteLine("Game " + GameId + ": Running death creation step");
#endif
				PowerHistory?.Add(new BlockStart(BlockType.DEATHS, this));
			}

			// Death Creation Step
			bool gameEnd = false;
			foreach (var e in dyingEntities) {
#if _ACTIONS_DEBUG
				DebugLog.WriteLine("Game {0}: {1} dies", GameId, e.ShortDescription);
#endif
				// Queue deathrattles and OnDeath triggers before moving mortally wounded minion to graveyard
				// (they will be executed after the zone move)
				// TODO: Test that each queue resolves before the next one populates. If it doesn't, we can make queue populating lazy
				if (e is Minion) {
					ActiveTriggers.Queue(TriggerType.OnDeath, e);
				}

				// NumMinionsPlayerKilledThisTurn seems to be the number of minions that died this turn
				// regardless of who or what killed what
				e.Controller.NumMinionsPlayerKilledThisTurn++;
				NumMinionsKilledThisTurn++;
				e.IsExhausted = false;

				// Move dead character to graveyard
				e.Zone = e.Controller.Graveyard;

				// TODO: Reset all minion tags to default
				if (e is Minion) {
					var minion = ((Minion)e);
					minion.Damage = 0;
				}

				// Hero death
				if (e is Hero) {
					e.Controller.PlayState = PlayState.LOSING;
					gameEnd = true;
				}
			}
			if (gameEnd)
				GameWon();

			if (dyingEntities.Count > 0) {
				PowerHistory?.Add(new BlockEnd(BlockType.DEATHS));
			}
			_deathCheckQueue.Clear();
		}

		/// <summary>
		/// Start a new game
		/// </summary>
		/// <remarks>
		/// Starting the game changes the <see cref="GameState"/> to RUNNING and each player's <see cref="PlayState"/> to PLAYING,
		/// which triggers the game start sequence. The method returns once the game is running.
		/// </remarks>
		/// <param name="FirstPlayer">The player to act first (1 or 2) - the default is to choose randomly</param>
		/// <param name="SkipMulligan">True to skip the initial mulligan, false otherwise - the default is to perform a mulligan phase</param>
		/// <param name="Shuffle">True to shuffle the players' decks, false otherwise - the default is to shuffle the decks</param>
		public void Start(int FirstPlayer = 0, bool SkipMulligan = false, bool Shuffle = true) {
			// Override settings
			FirstPlayerNum = FirstPlayer;
			this.SkipMulligan = SkipMulligan;

			// Configure players
			foreach (var p in Players)
				p.Start(Shuffle);

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
			DebugLog.WriteLine("Game " + GameId + ": Advancing game step from " + Step + " to " + NextStep);
#endif
			Step = Step.MAIN_END;
			ActionQueue.ProcessAll();
		}

		internal void GameWon() {
			foreach (var p in Players) {
				if (p.PlayState != PlayState.LOSING) continue;

				p.PlayState = PlayState.LOST;
				p.Opponent.PlayState = PlayState.WON;
				End();
			}
		}

		internal void End() {
			NextStep = Step.FINAL_WRAPUP;
			Step = Step.FINAL_WRAPUP;
			NextStep = Step.FINAL_GAMEOVER;
			Step = Step.FINAL_GAMEOVER;
			State = GameState.COMPLETE;

			// TODO: Gold reward state
		}

		internal void EntityCreated(IEntity entity) {
			OnEntityCreated?.Invoke(this, entity);
		}

		internal void EntityChanging(IEntity entity, GameTag tag, int oldValue, int newValue, int previousHash) {
			if (Settings.GameHashCaching)
				_changed = true;
		}

		internal void EntityChanged(IEntity entity, GameTag tag, int oldValue, int newValue) {
			if ((tag == GameTag.DAMAGE && ((ICharacter)entity).Health <= 0) || (tag == GameTag.TO_BE_DESTROYED && newValue == 1))
				_deathCheckQueue.Add(entity.Id);
			// TODO: Minions who reach 0 current Health because their maximum Health becomes 0 (such as due to Confuse) also need to be added to _deathCheckQueue
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
							_gameHash += (entity.Controller.Id * 8 + entity.ZonePosition) * entity.FuzzyHash;
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
					if (PowerHistory != null)
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
