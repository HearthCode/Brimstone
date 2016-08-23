﻿using System;
using System.Collections.Generic;

namespace Brimstone
{
	public partial class Minion : CanBeDamaged, IMinion
	{
		public Minion(Minion cloneFrom) : base(cloneFrom) { }
		public Minion(Card card, Dictionary<GameTag, int> tags = null) : base(card, tags) { }

		public IPlayable Play() {
			if (Game.Player1.Choice != null || Game.Player2.Choice != null)
				throw new PendingChoiceException();

			return (IPlayable) (Entity) Game.Action(this, Actions.Play(this));
		}

		public override object Clone() {
			return new Minion(this);
		}
	}
}