using System;
using System.Collections.Generic;

namespace Brimstone
{
	public class Minion : Entity, IMinion
	{
		public int Health { get; set; }

		public Minion(Minion cloneFrom) : base(cloneFrom) {
			Health = cloneFrom.Health;
		}
		public Minion(Game game, Card card, Dictionary<GameTag, int?> tags = null) : base(game, card, tags) { }

		public IPlayable Play() {
			Health = (int)Card[GameTag.HEALTH];
			Console.WriteLine("{0} is playing {1}", Game.CurrentPlayer, Card.Name);
			Game.CurrentPlayer.ZoneHand.Remove(this);
			Game.CurrentPlayer.ZonePlay.Add(this);
			this[GameTag.ZONE] = (int)Zone.PLAY;
			this[GameTag.ZONE_POSITION] = Game.CurrentPlayer.ZonePlay.Count;
			Game.ActionQueue.Enqueue(Card.Behaviour.Battlecry);
			Game.ActionQueue.Process();
			return this;
		}

		public void Damage(int amount) {
			Console.WriteLine("{0} gets hit for {1} points of damage!", this, amount);
			Health -= amount;
			this[GameTag.DAMAGE] = Card[GameTag.HEALTH] - Health;
			CheckForDeath();
		}

		public void CheckForDeath() {
			if (Health <= 0) {
				Console.WriteLine(this + " dies!");
				Game.Opponent.ZonePlay.Remove(this);
				Game.ActionQueue.Enqueue(Card.Behaviour.Deathrattle);
				Game.ActionQueue.Process();
				this[GameTag.ZONE] = (int)Zone.GRAVEYARD;
				this[GameTag.ZONE_POSITION] = 0;
				this[GameTag.DAMAGE] = 0;
			}
		}

		public override string ToString() {
			string s = Card.Name + " (Health=" + Health + ", ";
			foreach (var tag in Tags) {
				s += tag.Key + ": " + tag.Value + ", ";
			}
			return s.Substring(0, s.Length - 2) + ")";
		}

		public override object Clone() {
			return new Minion(this);
		}
	}
}