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
				return x.Entities.FuzzyGameHash == y.Entities.FuzzyGameHash;
			return x.PowerHistory.EquivalentTo(y.PowerHistory);
		}

		public int GetHashCode(Game obj) {
			return obj.Entities.FuzzyGameHash;
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
			Player1 = new Player(Hero1, (Player1Name.Length > 0) ? Player1Name : "Player 1", 1) {Zone = Board};
			Player2 = new Player(Hero2, (Player2Name.Length > 0) ? Player2Name : "Player 2", 2) {Zone = Board};
			for (int i = 0; i < 2; i++) {
				Players[i].Deck = new Deck(this, Players[i].HeroClass, Players[i]);
			}

			// No parent or children
			GameId = ++SequenceNumber;
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

		public void TriggerBlock(IEntity Source, ActionGraph Actions, int Index = -1) {
			TriggerBlock(Source, Actions.Unravel(), Index);
		}

		public void TriggerBlock(IEntity Source, List<QueueAction> Actions, int Index = -1) {
			DebugLog.WriteLine("Queueing trigger for " + Source.ShortDescription);
			Queue(Source, new GameBlockStart {Block = new BlockStart(BlockType.TRIGGER, Source, null, Index), Actions = Actions });
		}

		public void Start(int FirstPlayer = 0, bool SkipMulligan = false) {
			// Shuffle player decks
			foreach (var p in Players)
				p.Deck.Shuffle();

			// Generate player heroes
			// TODO: Add Start() parameters for non-default hero skins
			foreach (var p in Players)
				p.Hero = Add(new Hero(DefaultHero.For(p.HeroClass)), p) as Hero;

			// TODO: Insert event call precisely here so our server can iterate all created entities

			// Attach all game triggers
			ActiveTriggers.At(TriggerType.GameStart, (Action<IEntity>)(_ =>
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

			ActiveTriggers.At(TriggerType.BeginMulligan, (Action<IEntity>) (_ =>
			{
				foreach (var p in Players)
					p.StartMulligan();
			}));
			ActiveTriggers.At(TriggerType.PhaseMainReady, Actions.BeginTurn);

			// Set game state
			State = GameState.RUNNING;
			foreach (var p in Players)
				p.Start();

			ActionQueue.ProcessAll();
			// TODO: POWERED_UP settings and stuff go here
		}

		public void OnQueueEmpty()
		{
			// Death checking phase
			foreach (var e in Characters)
				e?.CheckForDeath();

			// Advance game step if necessary (probably setting off new triggers)
			if (NextStep != Step) {
				DebugLog.WriteLine("Advancing game step to " + NextStep);
				Step = NextStep;
			}
		}

		public void NextTurn() {
			if (Player1.Choice != null || Player2.Choice != null)
				throw new ChoiceException();

			Action(this, Actions.EndTurn);
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

		// Perform a fuzzy equivalence between two game states
		public bool EquivalentTo(Game game) {
			if (Settings.UseGameHashForEquality)
				return Entities.FuzzyGameHash == game.Entities.FuzzyGameHash;
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

			string s = string.Format("Game hash: {0:x8}", Entities.FuzzyGameHash) + "\r\n";

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
			return game;
		}

		public override object Clone() {
			return new Game(this);
		}
	}
}
