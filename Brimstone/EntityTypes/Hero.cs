using System;
using System.Collections.Generic;

namespace Brimstone
{
	public partial class Hero : CanBeDamaged
	{
		// TODO: Add hero powers

		public Hero(Hero cloneFrom) : base(cloneFrom) { }
		public Hero(IEntity controller, Card card, Dictionary<GameTag, int> tags = null) : base(controller, card, tags) {
			// Set player's hero entity tag
			controller[GameTag.HERO_ENTITY] = Id;
		}

		public override object Clone() {
			return new Hero(this);
		}
	}
}