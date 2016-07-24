using System;
using System.Collections.Generic;

namespace Brimstone
{
	public partial class Minion : CanBeDamaged, IMinion
	{
		public Minion(Minion cloneFrom) : base(cloneFrom) { }
		public Minion(IEntity controller, Card card, Dictionary<GameTag, int> tags = null) : base(controller, card, tags) { }

		public IPlayable Play() {
			return (IPlayable) (Entity) Game.ActionQueue.Enqueue(this, CardBehaviour.Play(this));
		}

		public override object Clone() {
			return new Minion(this);
		}
	}
}