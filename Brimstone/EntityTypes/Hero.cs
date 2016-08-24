using System;
using System.Collections.Generic;

namespace Brimstone
{
	public partial class Hero : Character, ICanTarget
	{
		// TODO: Add hero powers

		public Hero(Hero cloneFrom) : base(cloneFrom) { }
		public Hero(Card card, Dictionary<GameTag, int> tags = null) : base(card, tags) { }

		public List<IEntity> ValidTargets {
			get {
				// TODO: Same attack logic as Minion. Factor this out somewhere?
				return null;
			}
		}

		public override object Clone() {
			return new Hero(this);
		}
	}
}
