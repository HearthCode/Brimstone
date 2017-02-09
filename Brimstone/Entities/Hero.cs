/*
	Copyright 2016, 2017 Katy Coe

	This file is part of Brimstone.

	Brimstone is free software: you can redistribute it and/or modify
	it under the terms of the GNU Lesser General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	Brimstone is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU Lesser General Public License for more details.

	You should have received a copy of the GNU Lesser General Public License
	along with Brimstone.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;

namespace Brimstone.Entities
{
	public class Hero : Character<Hero>
	{
		public Hero(Card card, Dictionary<GameTag, int> tags = null) : base(card, tags) { }

		internal Hero(Hero cloneFrom) : base(cloneFrom) {
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
