// TODO: Separate Hearthstone and common

using System.Collections.Generic;

namespace Brimstone
{
	public static class DefaultHero {
		private static Dictionary<HeroClass, Card> cards = new Dictionary<HeroClass, Card>() {
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
