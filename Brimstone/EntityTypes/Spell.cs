using System;
using System.Collections.Generic;
using System.Linq;

namespace Brimstone
{
	public class Spell : Playable<Spell>, ICanTarget
	{
		public Spell(Spell cloneFrom) : base(cloneFrom) { }
		public Spell(Card card, Dictionary<GameTag, int> tags = null) : base(card, tags) { }

		public override IEnumerable<ICharacter> ValidTargets => GetValidAbilityTargets();

		public override object Clone() {
			return new Spell(this);
		}
	}
}
