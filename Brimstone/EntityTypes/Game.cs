using System;
using System.Collections.Generic;

namespace Brimstone
{
	public class Game : Entity
	{
		public int NextEntityId = 1;

		public Player Player1 { get; set; }
		public Player Player2 { get; set; }
		public Player CurrentPlayer { get; set; }
		public Player Opponent { get; set; }

		public PowerHistory PowerHistory = new PowerHistory();
		public Queue<QueueAction> ActionQueue = new Queue<QueueAction>();
		public Stack<ActionResult> ActionResultStack = new Stack<ActionResult>();

		// Required by IEntity
		public Game(Game cloneFrom) : base(cloneFrom) {
			NextEntityId = 1;
			Player1 = (Player)cloneFrom.Player1.Clone();
			Player2 = (Player)cloneFrom.Player2.Clone();
			// Yeah, fix this...
			CurrentPlayer = Player1;
			Opponent = Player2;
			// Change ownership
			foreach (var e in Entities)
				e.Game = this;
			// NOTE: Don't clone PowerHistory!
		}

		public Game(Dictionary<GameTag, int?> tags = null,
					bool PowerHistory = false) : base(null, Cards.Find["Game"], tags) {
			if (PowerHistory) {
				AttachPowerHistory();
				this.PowerHistory.Add(new CreateEntity(this) { EntityId = NextEntityId, Tags = tags });
			}
			NextEntityId++;
		}

		public void AttachPowerHistory() {
			PowerHistory.Game = this;
		}

		public override string ToString() {
			string s = "Board state: ";
			var players = new List<Player> { Player1, Player2 };
			foreach (var player in players) {
				s += "Player " + player.Card.Id + " - ";
				s += "HAND: ";
				foreach (var entity in player.ZoneHand) {
					s += entity.ToString() + ", ";
				}
				s += "PLAY: ";
				foreach (var entity in player.ZonePlay) {
					s += entity.ToString() + ", ";
				}
			}
			s = s.Substring(0, s.Length - 2) + "\nPower log: ";
			foreach (var item in PowerHistory)
				s += item + "\n";
			return s;
		}

		public void Enqueue(ActionGraph g) {
			// Don't queue unimplemented cards
			if (g != null)
				g.Queue(this);
		}

		public void ResolveQueue() {
			while (ActionQueue.Count > 0) {
				var action = ActionQueue.Dequeue();
				Console.WriteLine(action);
				var args = new List<ActionResult>();
				for (int i = 0; i < action.Args.Count; i++)
					args.Add(ActionResultStack.Pop());
				args.Reverse();
				var result = action.Run(args);
				if (result.HasResult)
					ActionResultStack.Push(result);
			}
		}

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

		public override object Clone() {
			return new Game(this);
		}
	}

}