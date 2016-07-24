using System;
using System.Collections.Generic;

namespace Brimstone
{
	public class FuzzyGameComparer : IEqualityComparer<Game>
	{
		// Used when adding to and fetching from HashSet, and testing for equality
		public bool Equals(Game x, Game y) {
			return x.Entities.FuzzyGameHash == y.Entities.FuzzyGameHash;
		}

		public int GetHashCode(Game obj) {
			return obj.Entities.FuzzyGameHash;
		}
	}

	// TODO: Abstract this so that Game is just another Entity (GameEntity) and make a new Game class that manages a game
	public partial class Game : Entity, IZones
	{
		public EntityController Entities;
		public TriggerManager ActiveTriggers;

		public Player[] Players { get; private set; } = new Player[2];
		public Player Player1 { get; private set; }
		public Player Player2 { get; private set; }

		public ZoneEntities Setaside { get; private set; }
		public ZoneGroup Zones { get; } = new ZoneGroup();

		public PowerHistory PowerHistory = new PowerHistory();
		public ActionQueue ActionQueue;

		// Game clones n-tree traversal
		private static int SequenceNumber { get; set; }
		public int GameId { get; }
		public Game Parent { get; }
		public HashSet<int> Children { get; } = new HashSet<int>();

		// Required by IEntity
		public Game(Game cloneFrom) : base(cloneFrom) {
			// Generate zones owned by game
			Zones[Zone.SETASIDE] = new ZoneEntities(this, this, Zone.SETASIDE);
			Zones[Zone.PLAY] = new ZoneEntities(this, this, Zone.PLAY);
			Setaside = Zones[Zone.SETASIDE];

			// Update tree
			GameId = ++SequenceNumber;
			Parent = cloneFrom;
			cloneFrom.Children.Add(GameId);
		}

		public Game(HeroClass Hero1, HeroClass Hero2, string Player1Name = "", string Player2Name = "", bool PowerHistory = false)
					: base(null, Cards.FromId("Game"), new Dictionary<GameTag, int> {
						{ GameTag.TURN, 1 },
						{ GameTag.ZONE, (int) Zone.PLAY },
						{ GameTag.NEXT_STEP, (int) Step.BEGIN_MULLIGAN },
						{ GameTag.STATE, (int) GameState.RUNNING }
					}) {
			// Start Power log
			if (PowerHistory) {
				this.PowerHistory.Attach(this);
			}

			ActionQueue = new ActionQueue(this);
			ActiveTriggers = new TriggerManager(this);

			Controller = this;
			Entities = new EntityController(this);

			// Generate players and empty decks
			SetPlayers(
				(Player) Add(new Player(this, Hero1, (Player1Name.Length > 0) ? Player1Name : "Player 1", 1)),
				(Player) Add(new Player(this, Hero2, (Player2Name.Length > 0) ? Player2Name : "Player 2", 2))
			);

			// Generate zones owned by game
			Zones[Zone.SETASIDE] = new ZoneEntities(this, this, Zone.SETASIDE);
			Zones[Zone.PLAY] = new ZoneEntities(this, this, Zone.PLAY);
			Setaside = Zones[Zone.SETASIDE];

			// No parent or children
			GameId = ++SequenceNumber;
			Parent = null;
		}

		public IEntity Add(IEntity newEntity) {
			return Entities.Add(newEntity);
		}

		public void Start() {
			// Pick a random starting player
			FirstPlayer = Players[RNG.Between(0, 1)];
			CurrentPlayer = FirstPlayer;
			foreach (var p in Players)
				p.Start();

			// TODO: Insert event call here so KettleSharp can iterate all created entities

			StartMulligan();
		}

		public void StartMulligan() {
			// TODO: Put the output into choices
			Step = Step.BEGIN_MULLIGAN;
			foreach (var p in Players)
				p.StartMulligan();
		}

		public void SetPlayers(Player Player1, Player Player2) {
			this.Player1 = Player1;
			this.Player2 = Player2;
			Players[0] = Player1;
			Players[1] = Player2;
			foreach (var p in Players) {
				p.Attach(this);
			}
		}

		public void BeginTurn() {
			ActionQueue.Enqueue(this, CardBehaviour.BeginTurn);
		}

		// Perform a fuzzy equivalence between two game states
		public bool EquivalentTo(Game game) {
			return Entities.FuzzyGameHash == game.Entities.FuzzyGameHash;
		}

		public override string ToString() {
			string s = "Board state: ";
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
		}

		public override IEntity CloneState() {
			var entities = ((EntityController)Entities.Clone());
			Game game = entities.FindGame();
			game.Entities = entities;
			// Set references to the new player proxies (no additional cloning)
			game.Player1 = entities.FindPlayer(1);
			game.Player2 = entities.FindPlayer(2);
			game.Players[0] = game.Player1;
			game.Players[1] = game.Player2;
			game.CurrentPlayer = (CurrentPlayer.Id == game.Player1.Id ? game.Player1 : game.Player2);
			// Re-assign zone references
			foreach (var p in game.Players)
				p.Attach(game);
			// Clone queue, stack and events
			game.ActionQueue = ((ActionQueue)ActionQueue.Clone());
			game.ActionQueue.Attach(game);
			// Clone triggers
			game.ActiveTriggers = ((TriggerManager)ActiveTriggers.Clone());
			game.ActiveTriggers.Game = game;
			// Link PowerHistory
			game.PowerHistory.Attach(game, this);
			return game;
		}

		public override object Clone() {
			return new Game(this);
		}
	}
}