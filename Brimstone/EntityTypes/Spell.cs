using System;
using System.Collections.Generic;

namespace Brimstone
{
	public class Spell : Entity, ISpell
	{
		public Spell(Spell cloneFrom) : base(cloneFrom) { }
		public Spell(Card card, Dictionary<GameTag, int> tags = null) : base(card, tags) { }

		public IPlayable Play() {
			if (Game.Player1.Choice != null || Game.Player2.Choice != null)
				throw new PendingChoiceException();

			var played = (Entity)Game.Action(this, Actions.Play(this));

			// TODO: This needs to go after the spell action completes, and before any triggers;
			// it also needs to be part of an action
			// Spells go to the graveyard after they are played
			((Player)played.Controller).Graveyard.MoveTo(played);

			return (IPlayable) played;
		}

		public override object Clone() {
			return new Spell(this);
		}
	}
}