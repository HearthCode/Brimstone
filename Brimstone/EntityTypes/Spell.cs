﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Brimstone
{
	public class Spell : Playable<Spell>, ICanTarget
	{
		public Spell(Spell cloneFrom) : base(cloneFrom) { }
		public Spell(Card card, Dictionary<GameTag, int> tags = null) : base(card, tags) { }

		private bool isValidTarget(ICharacter targetable) {
			Minion minion = targetable as Minion;
			if (minion != null && minion.CantBeTargetedByAbilities)
				return false;

			return MeetsGenericTargetingRequirements(targetable);
		}

		public override IEnumerable<ICharacter> ValidTargets {
			get {
				// If this is an untargeted spell, return an empty list
				if (!Card.RequiresTargetIfAvailable && !Card.RequiresTarget)
					return new List<ICharacter>();

				return Game.Characters.Where(isValidTarget);
			}
		}

		public override object Clone() {
			return new Spell(this);
		}
	}
}
