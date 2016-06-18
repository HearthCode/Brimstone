using System;
using System.Collections.Generic;

namespace Brimstone
{
	public partial class Game : Entity, IZones
	{
		public EntityController Entities;

		public Player[] Players { get; private set; } = new Player[2];
		public Player Player1 { get; private set; }
		public Player Player2 { get; private set; }

		public ZoneEntities Setaside { get; private set; }
		public ZoneGroup Zones { get; } = new ZoneGroup();

		public PowerHistory PowerHistory = new PowerHistory();
		public ActionQueue ActionQueue = new ActionQueue();

		// Required by IEntity
		public Game(Game cloneFrom) : base(cloneFrom) {
			// Clone queue and stack but not PowerHistory; keep PowerHistory disabled
			ActionQueue.Queue = new Queue<QueueAction>(cloneFrom.ActionQueue.Queue);
			ActionQueue.ResultStack = new Stack<ActionResult>(cloneFrom.ActionQueue.ResultStack);
			ActionQueue.Attach(this);
		}

		public Game(Card Hero1, Card Hero2, string Player1Name = "", string Player2Name = "", bool PowerHistory = false)
					: base(null, null, Cards.Find["Game"], new Dictionary<GameTag, int> {
						{ GameTag.TURN, 1 },
						{ GameTag.ZONE, (int) Zone.PLAY },
						{ GameTag.NEXT_STEP, (int) Step.BEGIN_MULLIGAN },
						{ GameTag.STATE, (int) GameState.RUNNING }
					}) {
			// Start Power log
			if (PowerHistory) {
				this.PowerHistory.Attach(this);
			}
			ActionQueue.Attach(this);

			Entities = new EntityController(this);

			// Generate game
			Controller = this;
			Entities.Add(this);

			// Generate players and empty decks
			SetPlayers(
				new Player(this, Hero1, (Player1Name.Length > 0) ? Player1Name : "Player 1"),
				new Player(this, Hero2, (Player2Name.Length > 0) ? Player2Name : "Player 2")
			);

			// Generate zones owned by game
			Zones[Zone.SETASIDE] = new ZoneEntities(this, this, Zone.SETASIDE);
			Zones[Zone.PLAY] = new ZoneEntities(this, this, Zone.PLAY);
			Setaside = Zones[Zone.SETASIDE];
		}

		public void Start() {
			// Pick a random starting player
			FirstPlayer = Players[RNG.Between(0, 1)];
			CurrentPlayer = FirstPlayer;
			Step = Step.BEGIN_MULLIGAN;
			foreach (var p in Players)
				p.Start();
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
				foreach (var entity in player.InPlay) {
					s += entity.ToString() + ", ";
				}
			}
			s = s.Substring(0, s.Length - 2) + "\nPower log: ";
			foreach (var item in PowerHistory)
				s += item + "\n";
			return s;
		}

		public void BeginTurn() {
			ActionQueue.Enqueue(CardBehaviour.BeginTurn);
		}

		public override IEntity CloneState() {
			var entities = ((EntityController)Entities.Clone());
			Game game = entities.FindGame();
			// Set references to the new player proxies (no additional cloning)
			game.Player1 = entities.FindPlayer(1);
			game.Player2 = entities.FindPlayer(2);
			game.Players[0] = game.Player1;
			game.Players[1] = game.Player2;
			game.CurrentPlayer = (CurrentPlayer.Id == game.Player1.Id ? game.Player1 : game.Player2);
			game.Entities = entities;
			// Re-assign zone references
			foreach (var p in game.Players)
				p.Attach(game);
			return game;
		}

		public override object Clone() {
			return new Game(this);
		}
	}
}