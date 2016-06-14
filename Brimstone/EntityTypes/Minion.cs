using System;

namespace Brimstone
{
	public class Minion : Entity, IMinion
	{
		public int Health { get; set; }

		public IPlayable Play() {
			Health = (int)Card[GameTag.HEALTH];
			Console.WriteLine("Player {0} is playing {1}", Game.CurrentPlayer, Card.Name);
			Game.CurrentPlayer.ZoneHand.Remove(this);
			Game.CurrentPlayer.ZonePlay.Add(this);
			this[GameTag.ZONE] = (int)Zone.PLAY;
			this[GameTag.ZONE_POSITION] = Game.CurrentPlayer.ZonePlay.Count;
			Game.Enqueue(Card.Behaviour.Battlecry);
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
				Game.Enqueue(Card.Behaviour.Deathrattle);
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

		protected override BaseEntity OnClone() {
			return new Minion();
		}
		public override object Clone() {
			Minion clone = (Minion)base.Clone();
			clone.Health = Health;
			return clone;
		}
	}
}