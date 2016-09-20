using static Brimstone.Actions;
using Behaviour = Brimstone.CardBehaviourGraph;

namespace Brimstone.Games.Hearthstone
{
	internal partial class Cards
	{
		// Hunter (Steady Shot)
		internal static Behaviour DS1h_292 = new Behaviour {
			Battlecry = Damage(OpponentHero, 2)
		};

		// Mage (Fireblast)
		internal static Behaviour CS2_034 = new Behaviour {
			Battlecry = Damage(Target, 1)
		};

		// Warlock (Life Tap)
		internal static Behaviour CS2_056 = new Behaviour {
			Battlecry = Damage(FriendlyHero, 2).Then(Draw(Controller))
		};
	}
}
