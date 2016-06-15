using System;
using System.Collections.Generic;

namespace Brimstone
{
	public class Game : Entity
	{
		public EntitySequence Entities;

		public Player Player1 { get; private set; }
		public Player Player2 { get; private set; }
		public Player CurrentPlayer { get; set; }
		public Player Opponent { get; set; }

		public PowerHistory PowerHistory = new PowerHistory();
		public ActionQueue ActionQueue = new ActionQueue();

		// Required by IEntity
		public Game(Game cloneFrom) : base(cloneFrom) {
			Entities = cloneFrom.Entities;
			// Set references to the new player proxies (no additional cloning)
			Player1 = Entities.FindPlayer(1);
			Player2 = Entities.FindPlayer(2);
			// TODO: Fix this (we'll implement it using tags later)
			CurrentPlayer = Player1;
			Opponent = Player2;
			// NOTE: Don't clone or enable PowerHistory!
			ActionQueue.Queue = new Queue<QueueAction>(cloneFrom.ActionQueue.Queue);
			ActionQueue.ResultStack = new Stack<ActionResult>(cloneFrom.ActionQueue.ResultStack);
			ActionQueue.Attach(this);
		}

		public Game(Player Player1 = null, Player Player2 = null,
					Dictionary<GameTag, int?> Tags = null,
					bool PowerHistory = false) : base(null, null, Cards.Find["Game"], Tags) {
			Controller = this;
			if (PowerHistory) {
				this.PowerHistory.Attach(this);
			}
			ActionQueue.Attach(this);
			Entities = new EntitySequence(this);
			Entities.Add(this);
			if (Player1 != null && Player2 != null) {
				SetPlayers(Player1, Player2);
			}
		}

		public void SetPlayers(Player Player1, Player Player2) {
			Player1.Controller = this;
			Player2.Controller = this;
			Entities.Add(Player1);
			Entities.Add(Player2);
			this.Player1 = Player1;
			this.Player2 = Player2;
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

		public void BeginTurn() {
			ActionQueue.Enqueue(CardBehaviour.BeginTurn);
			ActionQueue.Process();
		}

		public override IEntity CloneState() {
			var entities = ((EntitySequence)Entities.Clone());
			return entities.FindGame();
		}

		public override object Clone() {
			return new Game(this);
		}
	}
}