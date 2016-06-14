using System;
using System.Collections.Generic;

namespace Brimstone
{
	public class Player : BaseEntity
	{
		public string FriendlyName { get; set; }
		public int Health { get; private set; } = 30;
		public List<IPlayable> ZoneHand { get; private set; } = new List<IPlayable>();
		public List<IMinion> ZonePlay { get; private set; } = new List<IMinion>();

		public Player(Player cloneFrom) : base(cloneFrom) {
			Health = cloneFrom.Health;
			foreach (var e in cloneFrom.ZoneHand)
				ZoneHand.Add(e.Clone() as IPlayable);
			foreach (var e in cloneFrom.ZonePlay)
				ZonePlay.Add(e.Clone() as IMinion);
		}

		public Player(Game game, Dictionary<GameTag, int?> tags = null) : base(game, Cards.Find["Player"], tags) { }

		public IPlayable Give(Card card) {
			Game.ActionQueue.Enqueue(CardBehaviour.Give(this, card));
			return (IPlayable)(BaseEntity)Game.ActionQueue.Process()[0];
		}

		public override string ToString() {
			return FriendlyName;
		}

		public override object Clone() {
			return new Player(this);
		}
	}
}