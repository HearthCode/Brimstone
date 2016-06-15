using System;
using System.Collections.Generic;

namespace Brimstone
{
	public class Game : Entity
	{
		public EntitySequence Entities;

		public Player Player1 { get; }
		public Player Player2 { get; }
		public Player CurrentPlayer { get; set; }
		public Player Opponent { get; set; }

		public PowerHistory PowerHistory = new PowerHistory();
		public ActionQueue ActionQueue = new ActionQueue();

		// Required by IEntity
		public Game(Game cloneFrom) : base(cloneFrom) {
			// This makes a copy-on-write proxy clone of all the entities
			// and changes ownership to this game while preserving the entity IDs
			Entities = (EntitySequence)cloneFrom.Entities.Clone();
			// Set references to the new player proxies (no additional cloning)
			Player1 = (Player)Entities[cloneFrom.Player1.Id];
			Player2 = (Player)Entities[cloneFrom.Player2.Id];
			CurrentPlayer = (Player)Entities[cloneFrom.CurrentPlayer.Id];
			Opponent = (Player)Entities[cloneFrom.Opponent.Id];
			// NOTE: Don't clone or enable PowerHistory!
			ActionQueue.Queue = new Queue<QueueAction>(cloneFrom.ActionQueue.Queue);
			ActionQueue.ResultStack = new Stack<ActionResult>(cloneFrom.ActionQueue.ResultStack);
			ActionQueue.Attach(this);
		}

		public Game(Player Player1 = null, Player Player2 = null,
					Dictionary<GameTag, int?> Tags = null,
					bool PowerHistory = false) : base(null, Cards.Find["Game"], Tags) {
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
			Entities.Add(Player1);
			Entities.Add(Player2);
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
		/*
		public IEnumerable<IEntity> Entities {
			get {
				yield return this;
				yield return Player1;
				yield return Player2;
				foreach (var e in Player1.ZoneHand)
					yield return e;
				foreach (var e in Player1.ZonePlay)
					yield return e;
				foreach (var e in Player2.ZoneHand)
					yield return e;
				foreach (var e in Player2.ZonePlay)
					yield return e;
			}
		}
		*/
		public void BeginTurn() {
			ActionQueue.Enqueue(CardBehaviour.BeginTurn);
			ActionQueue.Process();
		}

		public override object Clone() {
			return new Game(this);
		}
	}
}