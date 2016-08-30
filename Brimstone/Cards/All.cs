namespace Brimstone
{
	public class BehaviourScripts : Actions
	{
		// Flame Juggler
		public static Behaviour AT_094 = new Behaviour {
			Battlecry = Damage(RandomOpponentCharacter, 1)
		};

		// Boom Bot
		public static Behaviour GVG_110t = new Behaviour {
			Deathrattle = Damage(RandomOpponentCharacter, RandomAmount(1, 4))
		};

		// Whirlwind
		public static Behaviour EX1_400 = new Behaviour {
			Battlecry = Damage(AllMinions, 1)
		};

		// Acolyte of Pain
		public static Behaviour EX1_007 = new Behaviour {
			Triggers = {
				OnDamage(Self, Draw(Controller))
			}
		};

		// Arcane Missiles
		public static Behaviour EX1_277 = new Behaviour {
			Battlecry = Damage(RandomOpponentCharacter, 1) * 3
		};

		// Fireball
		public static Behaviour CS2_029 = new Behaviour {
			Battlecry = Damage(Target, 6)
		};

		// Armorsmith
		// Imp Gang Boss
	}
}