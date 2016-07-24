using System;
using System.Collections.Generic;

namespace Brimstone
{
	public class Spell : Entity, ISpell
	{
		public Spell(Spell cloneFrom) : base(cloneFrom) { }
		public Spell(IEntity controller, Card card, Dictionary<GameTag, int> tags = null) : base(controller, card, tags) { }

		public IPlayable Play() {
			var played = (Entity)Game.ActionQueue.Enqueue(this, CardBehaviour.Play(this));

			// Spells go to the graveyard after they are played
			((Player)played.Controller).Graveyard.MoveTo(played);

			return (IPlayable) played;
		}

		public override object Clone() {
			return new Spell(this);
		}
	}
}