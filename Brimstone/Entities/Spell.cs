using System;
using System.Collections.Generic;
using System.Linq;

namespace Brimstone.Entities
{
	public class Spell : Playable<Spell>, ICanTarget
	{
		internal Spell(Spell cloneFrom) : base(cloneFrom) { }
		public Spell(Card card, Dictionary<GameTag, int> tags = null) : base(card, tags) { }

		public override IEnumerable<ICharacter> ValidTargets => GetValidAbilityTargets();

		protected internal override bool MeetsGenericTargetingRequirements(ICharacter target) {
			Minion minion = target as Minion;

			// Spells and hero powers can't target CantBeTargetedByAbilities minions
			return (minion != null && minion.CantBeTargetedByAbilities ? false : base.MeetsGenericTargetingRequirements(target));
		}

		public override object Clone() {
			return new Spell(this);
		}
	}
}
