using System.Collections.Generic;

namespace Brimstone
{
	public partial interface ICharacter : IPlayable
	{
		ICanTarget Attack(ICharacter Target = null);

		void Hit(int amount);

		void CheckForDeath();
	}

	public abstract partial class Character<T> : Playable<T>, ICharacter where T : Entity
	{
		protected Character(Card card, Dictionary<GameTag, int> tags = null) : base(card, tags) { }
		protected Character(Character<T> cloneFrom) : base(cloneFrom) { }

		public ICanTarget Attack(ICharacter target = null) {
			if (Game.Player1.Choice != null || Game.Player2.Choice != null)
				throw new ChoiceException();

			// TODO: Check targeting validity
			Target = target;
			return (ICanTarget)(Entity)Game.Action(this, Actions.Attack(this, (Entity)target));
		}

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
