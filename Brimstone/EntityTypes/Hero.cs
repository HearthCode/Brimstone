using System;
using System.Collections.Generic;

namespace Brimstone
{
	public class Hero : Character<Hero>
	{
		public Hero(Card card, Dictionary<GameTag, int> tags = null) : base(card, tags) { }

		public Hero(Hero cloneFrom) : base(cloneFrom) {
			_heroPowerId = cloneFrom._heroPowerId;
		}

		private int _heroPowerId;
		public HeroPower Power {
			get { return (HeroPower) Game.Entities[_heroPowerId]; }
			set { _heroPowerId = value.Id; }
		}

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
