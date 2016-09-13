using System;
using System.Collections.Generic;

namespace Brimstone
{
	public class Hero : Character<Hero>
	{
		// TODO: Add hero powers

		public Hero(Hero cloneFrom) : base(cloneFrom) { }
		public Hero(Card card, Dictionary<GameTag, int> tags = null) : base(card, tags) { }

		public HeroPower Power { get; set; }

		public override bool IsPlayable => false;

		// Create Hero Power at start of game
		// TODO: Argument to allow overriding default hero power
		public void Start() {
			Power = new HeroPower(Cards.FromAssetId(this[GameTag.SHOWN_HERO_POWER]), new Dictionary<GameTag, int> {
				[GameTag.CREATOR] = Id
			}) { Zone = Controller.Board };
		}

		public override object Clone() {
			return new Hero(this);
		}
	}
}
