using System;
using System.Collections.Generic;

namespace Brimstone
{
	public partial class Hero : CanBeDamaged
	{
		// TODO: Add hero powers

		public Hero(Hero cloneFrom) : base(cloneFrom) { }
		public Hero(Card card, Dictionary<GameTag, int> tags = null) : base(card, tags) { }

		public override object Clone() {
			return new Hero(this);
		}
	}
}