using System;
using System.Collections.Generic;

namespace Brimstone
{
	public class Player : Entity
	{
		public string FriendlyName { get; set; }
		public int Health { get; private set; } = 30;
		public List<IPlayable> ZoneHand { get; private set; } = new List<IPlayable>();
		public List<IMinion> ZonePlay { get; private set; } = new List<IMinion>();

		public Player(Player cloneFrom) : base(cloneFrom) {
			Health = cloneFrom.Health;
			ZoneHand = new List<IPlayable>();
			ZonePlay = new List<IMinion>();
			foreach (var e in cloneFrom.ZoneHand)
				ZoneHand.Add(e.Clone() as IPlayable);
			foreach (var e in cloneFrom.ZonePlay)
				ZonePlay.Add(e.Clone() as IMinion);
		}

		public Player(Game game, Dictionary<GameTag, int?> tags = null) : base(game, Cards.Find["Player"], tags) { }

		public IPlayable Give(Card card) {
			Console.WriteLine("Giving {0} to {1}", card, this);

			Game.ActionQueue.Enqueue(CardBehaviour.Give(this, card));
			return (IPlayable)(Entity)Game.ActionQueue.Process()[0];
		}

		public override string ToString() {
			return FriendlyName;
		}

		public override object Clone() {
			return new Player(this);
		}
	}
}