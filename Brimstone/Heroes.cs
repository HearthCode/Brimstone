using System.Collections.Generic;

namespace Brimstone
{
	public enum HeroClass {
		Druid,
		Hunter,
		Mage,
		Paladin,
		Priest,
		Rogue,
		Shaman,
		Warlock,
		Warrior
	}

	public static class DefaultHero {
		private static Dictionary<HeroClass, Card> cards = new Dictionary<HeroClass, Card>() {
			{ HeroClass.Druid, Cards.Find["HERO_06"] },
			{ HeroClass.Hunter, Cards.Find["HERO_05"] },
			{ HeroClass.Mage, Cards.Find["HERO_08"] },
			{ HeroClass.Paladin, Cards.Find["HERO_04"] },
			{ HeroClass.Priest, Cards.Find["HERO_09"] },
			{ HeroClass.Rogue, Cards.Find["HERO_03"] },
			{ HeroClass.Shaman, Cards.Find["HERO_02"] },
			{ HeroClass.Warlock, Cards.Find["HERO_07"] },
			{ HeroClass.Warrior, Cards.Find["HERO_01"] }
		};

		public static Card For(HeroClass heroClass) {
			return cards[heroClass];
		}

		public static Card Warrior = Cards.Find["HERO_01"];
		public static Card Shaman = Cards.Find["HERO_02"];
		public static Card Rogue = Cards.Find["HERO_03"];
		public static Card Paladin = Cards.Find["HERO_04"];
		public static Card Hunter = Cards.Find["HERO_05"];
		public static Card Druid = Cards.Find["HERO_06"];
		public static Card Warlock = Cards.Find["HERO_07"];
		public static Card Mage = Cards.Find["HERO_08"];
		public static Card Priest = Cards.Find["HERO_09"];
	}

	public static class HeroCard {
		public static Card Grommash = Cards.Find["HERO_01"];
		public static Card Magni = Cards.Find["HERO_01a"];
		public static Card Thrall = Cards.Find["HERO_02"];
		public static Card Valeera = Cards.Find["HERO_03"];
		public static Card Uther = Cards.Find["HERO_04"];
		public static Card Liadrin = Cards.Find["HERO_04a"];
		public static Card Rexxar = Cards.Find["HERO_05"];
		public static Card Alleria = Cards.Find["HERO_05a"];
		public static Card Malfurion = Cards.Find["HERO_06"];
		public static Card Guldan = Cards.Find["HERO_07"];
		public static Card Jaina = Cards.Find["HERO_08"];
		public static Card Medivh = Cards.Find["HERO_08a"];
		public static Card Khadgar = Cards.Find["HERO_08b"];
		public static Card Anduin = Cards.Find["HERO_09"];
	}
}
