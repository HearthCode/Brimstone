using System.Collections.Generic;

namespace Brimstone
{
	public abstract partial class Character : Entity
	{
		public Character(Card card, Dictionary<GameTag, int> tags = null) : base(card, tags) { }
		public Character(Character cloneFrom) : base(cloneFrom) { }

		public void Hit(int amount) {
			Game.Action(this, Actions.Damage(this, amount));
		}

		public void CheckForDeath() {
			if (Health <= 0) {
				Game.Queue(this, Actions.Death(this));
			}
		}
	}
}
