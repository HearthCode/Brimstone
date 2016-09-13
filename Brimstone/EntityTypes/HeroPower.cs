 using System;
using System.Collections.Generic;
using System.Linq;

namespace Brimstone
{
	public class HeroPower : CanTarget
	{
		public HeroPower(HeroPower cloneFrom) : base(cloneFrom) { }
		public HeroPower(Card card, Dictionary<GameTag, int> tags = null) : base(card, tags) { }

		public override IEnumerable<ICharacter> ValidTargets => GetValidAbilityTargets();

		protected override bool MeetsGenericTargetingRequirements(ICharacter target) {
			Minion minion = target as Minion;

			// Spells and hero powers can't target CantBeTargetedByAbilities minions
			return (minion != null && minion.CantBeTargetedByAbilities ? false : base.MeetsGenericTargetingRequirements(target));
		}

		public override object Clone() {
			return new HeroPower(this);
		}
	}
}
