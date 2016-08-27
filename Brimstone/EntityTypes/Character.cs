using System.Collections.Generic;

namespace Brimstone
{
	public partial interface ICharacter : ICanTarget
	{
		void Hit(int amount);
		void CheckForDeath();
	}

	public abstract partial class Character<T> : Playable<T>, ICharacter where T : Entity
	{
		protected Character(Card card, Dictionary<GameTag, int> tags = null) : base(card, tags) { }
		protected Character(Character<T> cloneFrom) : base(cloneFrom) { }

		// TODO: Allow UserData for ActionQueue
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
