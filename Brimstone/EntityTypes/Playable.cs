using System.Collections.Generic;

namespace Brimstone
{
	public partial interface IPlayable : ICanTarget
	{
		IPlayable Play(ICharacter target = null);
	}

	public abstract partial class Playable<T> : CanTarget, IPlayable where T : Entity
	{
		protected Playable(Playable<T> cloneFrom) : base(cloneFrom) { }
		protected Playable(Card card, Dictionary<GameTag, int> tags = null) : base(card, tags) { }

		public T GiveTo(Player player)
		{
			Zone = player.Hand;
			return (T) (IEntity) this;
		}

		// Return IPlayable when calling Play from interface
		IPlayable IPlayable.Play(ICharacter target) { return (IPlayable) Play(target); }

		// Return T when calling Play on concrete class
		public T Play(ICharacter target = null) {
			if (Game.Player1.Choice != null || Game.Player2.Choice != null)
				throw new ChoiceException();

			// TODO: Check card is in player's hand

			// TODO: Check targeting validity
			Target = target;
			return (T) Game.Action(this, Actions.Play(this));
		}
	}
}
