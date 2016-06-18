using System;
using System.Collections.Generic;

namespace Brimstone
{
	public partial class Hero : CanBeDamaged
	{
		public Hero(Hero cloneFrom) : base(cloneFrom) { }
		public Hero(Game game, IEntity controller, Card card, Dictionary<GameTag, int> tags = null) : base(game, controller, card, tags) {
			// Set player's hero entity tag
			controller[GameTag.HERO_ENTITY] = Id;
		}

		public override object Clone() {
			return new Hero(this);
		}
	}
}