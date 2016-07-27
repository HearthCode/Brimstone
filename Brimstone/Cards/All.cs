namespace Brimstone
{
	public partial class BehaviourScripts : Actions
	{
		// Flame Juggler
		public static Behaviour AT_094 = new Behaviour {
			Battlecry = Damage(RandomOpponentMinion, 1)
		};

		// Boom Bot
		public static Behaviour GVG_110t = new Behaviour {
			Deathrattle = Damage(RandomOpponentMinion, RandomAmount(1, 4))
		};

		// Whirlwind
		public static Behaviour EX1_400 = new Behaviour {
			Battlecry = Damage(AllMinions, 1)
		};

		// Acolyte of Pain
		public static Behaviour EX1_007 = new Behaviour {
			Triggers = {
				When(Damage(Self), Draw(Controller))
			}
		};

		// Arcane Missiles
		public static Behaviour EX1_277 = new Behaviour {
			Battlecry = Damage(Random(AllCharacters), 1) * 3
		};

		// Armorsmith
		// Imp Gang Boss
	}
}