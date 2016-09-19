using static Brimstone.Behaviours;

namespace Brimstone.Games.Hearthstone
{
	public partial class CardDefinitions
	{
		// Basic Hero Powers

		// Hunter (Steady Shot)
		public static Behaviour DS1h_292 = new Behaviour {
			Battlecry = Damage(OpponentHero, 2)
		};

		// Mage (Fireblast)
		public static Behaviour CS2_034 = new Behaviour {
			Battlecry = Damage(Target, 1)
		};

		// Warlock (Life Tap)
		public static Behaviour CS2_056 = new Behaviour {
			Battlecry = Damage(FriendlyHero, 2).Then(Draw(Controller))
		};
	}
}
