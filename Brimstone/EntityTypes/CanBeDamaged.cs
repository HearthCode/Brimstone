using System;

namespace Brimstone
{
	public abstract partial class CanBeDamaged : Entity
	{
		public void Hit(int amount) {
			Game.ActionQueue.Enqueue(this, CardBehaviour.Damage(this, amount));
		}

		public void CheckForDeath() {
			if (Health <= 0) {
				Game.ActionQueue.EnqueuePaused(this, CardBehaviour.Death(this));
			}
		}
	}
}
