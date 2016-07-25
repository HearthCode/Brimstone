using System.Collections.Generic;

namespace Brimstone
{
	public abstract partial class CanBeDamaged : Entity
	{
		public CanBeDamaged(Card card, Dictionary<GameTag, int> tags = null) : base(card, tags) { }
		public CanBeDamaged(CanBeDamaged cloneFrom) : base(cloneFrom) { }

		public void Hit(int amount) {
			Game.Action(this, Actions.Damage(this, amount));
		}

		public void CheckForDeath() {
			if (Health <= 0) {
				Game.ActionQueue.EnqueuePaused(this, Actions.Death(this));
			}
		}
	}
}
