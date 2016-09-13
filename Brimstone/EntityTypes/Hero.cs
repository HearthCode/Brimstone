using System;
using System.Collections.Generic;

namespace Brimstone
{
	public class Hero : Character<Hero>
	{
		// TODO: Add hero powers

		public Hero(Hero cloneFrom) : base(cloneFrom) { }
		public Hero(Card card, Dictionary<GameTag, int> tags = null) : base(card, tags) { }

		public override bool IsPlayable => false;

		public override IEnumerable<ICharacter> ValidTargets => GetValidAttackTargets();

		public override object Clone() {
			return new Hero(this);
		}
	}
}
