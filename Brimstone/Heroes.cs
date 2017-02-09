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

// TODO: Separate Hearthstone and common

using System.Collections.Generic;

namespace Brimstone
{
	public static class DefaultHero {
		private static readonly Dictionary<HeroClass, Card> cards = new Dictionary<HeroClass, Card>() {
			{ HeroClass.Druid, Cards.FromId("HERO_06") },
			{ HeroClass.Hunter, Cards.FromId("HERO_05") },
			{ HeroClass.Mage, Cards.FromId("HERO_08") },
			{ HeroClass.Paladin, Cards.FromId("HERO_04") },
			{ HeroClass.Priest, Cards.FromId("HERO_09") },
			{ HeroClass.Rogue, Cards.FromId("HERO_03") },
			{ HeroClass.Shaman, Cards.FromId("HERO_02") },
			{ HeroClass.Warlock, Cards.FromId("HERO_07") },
			{ HeroClass.Warrior, Cards.FromId("HERO_01") }
		};

		public static Card For(HeroClass heroClass) {
			return cards[heroClass];
		}

		public static Card Warrior = Cards.FromId("HERO_01");
		public static Card Shaman = Cards.FromId("HERO_02");
		public static Card Rogue = Cards.FromId("HERO_03");
		public static Card Paladin = Cards.FromId("HERO_04");
		public static Card Hunter = Cards.FromId("HERO_05");
		public static Card Druid = Cards.FromId("HERO_06");
		public static Card Warlock = Cards.FromId("HERO_07");
		public static Card Mage = Cards.FromId("HERO_08");
		public static Card Priest = Cards.FromId("HERO_09");
	}
}
