using static Brimstone.TriggerType;
using static Brimstone.Behaviours;

namespace Brimstone
{
	public partial class BehaviourScripts
	{
		// Basic, Neutral

		// Raid Leader
		// public static Behaviour CS2_122 = new Behaviour {
		// TODO: Combining selectors, Enchantment-refreshing Auras (CS2_122e)

		// Stormwind Champion
		// public static Behaviour CS2_222 = new Behaviour {
		// TODO: Combining selectors, Enchantment-refreshing Auras (CS2_222o)

		// Frostwolf Warlord
		// Battlecry =
		// TODO: Counting selectors, Enchantments (CS2_266e)

		// Voodoo Doctor
		public static Behaviour EX1_011 = new Behaviour {
			Battlecry = Heal(Target, 2)
		};

		// Novice Engineer
		public static Behaviour EX1_015 = new Behaviour {
			Battlecry = Draw(Controller)
		};

		// Mad Bomber
		public static Behaviour EX1_082 = new Behaviour {
			Battlecry = Damage(Random(AllHealthyCharacters - Self), 1) * 3
		};

		// Demolisher
		public static Behaviour EX1_102 = new Behaviour {
			Triggers = {
				[OnBeginTurn] = IsFriendly > Damage(RandomOpponentHealthyCharacter, 2)
			}
		};

		// Dire Wolf Alpha
		// public static Behaviour EX1_163 = new Behaviour {
		// TODO: Enchantment-refreshing Auras (EX1_162o)

		// Gurubashi Berserker
		// public static Behaviour EX1_399 = new Behaviour {
		// TODO: Enchantments (EX1_508o)

		// Nightblade
		public static Behaviour EX1_593 = new Behaviour {
			Battlecry = Damage(OpponentHero, 3)
		};

		// Cult Master
		public static Behaviour EX1_595 = new Behaviour {
			Triggers = {
				[OnDeath] = FriendlyMinions - Self > Draw(Controller)
			}
		};

		// Classic, Neutral, Common

		// Earthen Ring Farseer
		public static Behaviour CS2_117 = new Behaviour {
			Battlecry = Heal(Target, 3)
		};

		// Ironforge Rifleman
		public static Behaviour CS2_141 = new Behaviour {
			Battlecry = Damage(Target, 2)
		};

		// Silver Hand Knight
		public static Behaviour CS2_151 = new Behaviour {
			Battlecry = Summon(Controller, "CS2_152")
		};

		// Elven Archer
		public static Behaviour CS2_189 = new Behaviour {
			Battlecry = Damage(Target, 1)
		};

		// Abusive Sergeant
		// public static Behaviour CS2_188 = new Behaviour {
		// TODO: Enchantments (CS2_188o)

		// Razorfen Hunter
		public static Behaviour CS2_196 = new Behaviour {
			Battlecry = Summon(Controller, "CS2_boar")
		};

		// Ironbeak Owl
		public static Behaviour CS2_203 = new Behaviour {
			Battlecry = Silence(Target)
		};

		// Spiteful Smith
		// public static Behaviour CS2_221 = new Behaviour {
		// TODO: Enrage, Enchantment-refreshing Auras (CS2_221e)

		// Venture Co. Mercenary
		// public static Behaviour CS2_227 = new Behaviour {
		// TODO: Tag-refreshing Auras

		// Darkscale Healer
		public static Behaviour DS1_055 = new Behaviour {
			Battlecry = Heal(FriendlyCharacters, 2)
		};

		// Acolyte of Pain
		public static Behaviour EX1_007 = new Behaviour {
			Triggers = {
				[OnDamage] = IsSelf > Draw(Controller)
			}
		};

		// Shattered Sun Cleric
		// public static Behaviour EX1_019 = new Behaviour {
		// TODO: Enchantments (EX1_019e)

		// Dragonling Mechanic
		public static Behaviour EX1_025 = new Behaviour {
			Battlecry = Summon(Controller, "EX1_025t")
		};

		// Leper Gnome
		public static Behaviour EX1_029 = new Behaviour {
			Deathrattle = Damage(OpponentHero, 2)
		};

		// Dark Iron Dwarf
		/*public static Behaviour EX1_046 = new Behaviour {
			Battlecry =
		};*/
		// TODO: Enchantments (EX1_046e)

		// Spellbreaker
		public static Behaviour EX1_048 = new Behaviour {
			Battlecry = Silence(Target)
		};

		// Youthful Brewmaster
		public static Behaviour EX1_049 = new Behaviour {
			Battlecry = Bounce(Target)
		};

		// Ancient Brewmaster
		public static Behaviour EX1_057 = new Behaviour {
			Battlecry = Bounce(Target)
		};

		// Acidic Swamp Ooze
		/*public static Behaviour EX1_066 = new Behaviour {
			Battlecry = Destroy(OpponentWeapon)
		};*/
		// TODO: Weapon selectors

		// Loot Hoarder
		public static Behaviour EX1_096 = new Behaviour {
			Deathrattle = Draw(Controller)
		};

		// Tauren Warrior
		// public static Behaviour EX1_390 = new Behaviour {
		// TODO: Enrage, Enchantments (EX1_390e)

		// Amani Berserker
		// public static Behaviour EX1_393 = new Behaviour {
		// TODO: Enrage, Enchantments (EX1_393e)

		// Raging Worgen
		// public static Behaviour EX1_412 = new Behaviour {
		// TODO: Enrage, Enchantments, gaining Windfury (EX1_412e)

		// Murloc Tidehunter
		public static Behaviour EX1_506 = new Behaviour {
			Battlecry = Summon(Controller, "EX1_506a")
		};

		// Harvest Golem
		public static Behaviour EX1_556 = new Behaviour {
			Deathrattle = Summon(Controller, "skele21")
		};

		// Priestess of Elune
		public static Behaviour EX1_583 = new Behaviour {
			Battlecry = Heal(FriendlyHero, 4)
		};

		// Bloodsail Raider
		// public static Behaviour NEW1_018 = new Behaviour {
		// TODO: Enchantments (NEW1_018e)

		// Dread Corsair
		// public static Behaviour NEW1_022 = new Behaviour {
		// TODO: Tag-refreshing Auras

		// Flesheating Ghoul
		// public static Behaviour tt_004 = new Behaviour {
		// TODO: Enchantments (tt_004o)

		// unused buffs: CS2_181e = buff(atk=2)

		// Classic, Neutral, Rare

		// Injured Blademaster
		public static Behaviour CS2_181 = new Behaviour {
			Battlecry = Damage(Self, 4)
		};

		// Lightwarden
		// public static Behaviour EX1_001 = new Behaviour {
		// TODO: Enchantments (EX1_001e)

		// Young Priestess
		// public static Behaviour EX1_004 = new Behaviour {
		// TODO: Enchantments (EX1_004e)

		// Alarm-o-Bot
		/*public static Behaviour EX1_006 = new Behaviour {
			Triggers = {
				OnBeginTurn(Controller, SwapHandAndPlay(RandomFriendlyMinionInHand, Self))
			}
		};*/
		//TODO: Queuing/trigger conditions 'minion exists in friendly hand' http://i.imgur.com/ov58pCN.png

		// Angry Chicken
		// public static Behaviour EX1_009 = new Behaviour {
		// TODO: Enrage, Enchantments (EX1_009e)

		// Twilight Drake
		// public static Behaviour EX1_043e = new Behaviour {
		// TODO: Counting selectors, Enchantments (EX1_043e)

		// Questing Adventurer
		// public static Behaviour EX1_044e = new Behaviour {
		// TODO: Enchantments (EX1_044e)

		// Coldlight Oracle
		public static Behaviour EX1_050 = new Behaviour {
			Battlecry = (Draw(Controller) * 2).Then(Draw(Opponent) * 2)
		};

		// Mana Addict
		// public static Behaviour EX1_055 = new Behaviour {
		// TODO: Conditional selectors, Enchantments (EX1_055o)

		// Sunfury Protector
		/*public static Behaviour EX1_058 = new Behaviour {
			Battlecry = SetTag(AdjacentMinions, GameTag.TAUNT, 1)
		};*/
		// TODO: Tag setting action

		// Crazed Alchemist
		// public static Behaviour EX1_059 = new Behaviour {
		// TODO: Enchantments (EX1_059e)

		// Pint-Sized Summoner
		// public static Behaviour EX1_076 = new Behaviour {
		// TODO: Tag-refreshing Auras

		// Secretkeeper
		// public static Behaviour EX1_080 = new Behaviour {
		// TODO: Conditional selectors, Enchantments (EX1_080o)

		// Mind Control Tech
		// public static Behaviour EX1_085 = new Behaviour {
		// TODO: Counting selectors

		// Arcane Golem
		public static Behaviour EX1_089 = new Behaviour {
			Battlecry = GainMana(Opponent, 1)
		};

		// Defender of Argus
		// public static Behaviour EX1_093 = new Behaviour {
		// TODO: Enchantments providing Taunt, Enchantments (EX1_093e)

		// Gadgetzan Auctioneer
		public static Behaviour EX1_095 = new Behaviour {
			Triggers = {
				[OnPlay] = IsFriendlySpell > Draw(Controller)
			}
		};

		// Abomination
		public static Behaviour EX1_097 = new Behaviour {
			Deathrattle = Damage(AllCharacters, 2)
		};

		// Coldlight Seer
		// public static Behaviour EX1_103 = new Behaviour {
		// TODO: Combining selectors, Enchantments (EX1_103e)

		// Azure Drake
		public static Behaviour EX1_284 = new Behaviour {
			Battlecry = Draw(Controller)
		};

		// Murloc Tidecaller
		// public static Behaviour EX1_509 = new Behaviour {
		// TODO: Combining selectors, Enchantments (EX1_509e)

		// Ancient Mage
		// public static Behaviour EX1_584 = new Behaviour {
		// TODO: Enchantments (EX1_584e)

		// Imp Master
		public static Behaviour EX1_597 = new Behaviour {
			Triggers = {
				[OnEndTurn] = IsFriendly > Damage(Self, 1).Then(Summon(Controller, "EX1_598"))
			}
		};

		// Mana Wraith
		// public static Behaviour EX1_616 = new Behaviour {
		// TODO: Tag-refreshing Auras

		// Knife Juggler
		public static Behaviour NEW1_019 = new Behaviour {
			Triggers = {
				[AfterSummon] = IsFriendly > Damage(RandomOpponentHealthyMinion, 1)
			}
		};

		// Wild Pyromancer
		public static Behaviour NEW1_020 = new Behaviour {
			Triggers = {
				[AfterPlay] = IsFriendlySpell > Damage(AllMinions, 1)
			}
		};

		// Bloodsail Corsair
		/*public static Behaviour New1_025 = new Behaviour {
			Battlecry = Damage(OpponentWeapon, 1)
		};*/
		// TODO: Weapon selectors

		// Violet Teacher
		public static Behaviour NEW1_026 = new Behaviour {
			Triggers = {
				[OnPlay] = IsFriendlySpell > Summon(Controller, "NEW1_026t")
			}
		};

		// Master Swordsmith
		// public static Behaviour NEW1_037 = new Behaviour {
		// TODO: Combining selectors, Enchantments (NEW1_037e)

		// Stampeding Kodo
		// public static Behaviour NEW1_041 = new Behaviour {
		// TODO: Combining selectors

		// Unsorted

		// Flame Juggler
		public static Behaviour AT_094 = new Behaviour {
			Battlecry = Damage(RandomOpponentHealthyCharacter, 1)
		};

		// Boom Bot
		public static Behaviour GVG_110t = new Behaviour {
			Deathrattle = Damage(RandomOpponentHealthyCharacter, RandomAmount(1, 4))
		};

		// Whirlwind
		public static Behaviour EX1_400 = new Behaviour {
			Battlecry = Damage(AllMinions, 1)
		};

		// Arcane Missiles
		public static Behaviour EX1_277 = new Behaviour {
			Battlecry = Damage(RandomOpponentHealthyCharacter, 1) * 3
		};

		// Fireball
		public static Behaviour CS2_029 = new Behaviour {
			Battlecry = Damage(Target, 6)
		};

		// Pyroblast
		public static Behaviour EX1_279 = new Behaviour {
			Battlecry = Damage(Target, 10)
		};

		// Arcane Explosion
		public static Behaviour CS2_025 = new Behaviour {
			Battlecry = Damage(OpponentMinions, 1)
		};

		// Flamestrike
		public static Behaviour CS2_032 = new Behaviour {
			Battlecry = Damage(OpponentMinions, 4)
		};

		// Moonfire
		public static Behaviour CS2_008 = new Behaviour {
			Battlecry = Damage(Target, 1)
		};

		// Holy Smite
		public static Behaviour CS1_130 = new Behaviour {
			Battlecry = Damage(Target, 2)
		};

		// Darkbomb
		public static Behaviour GVG_015 = new Behaviour {
			Battlecry = Damage(Target, 3)
		};

		// Armorsmith
		/*public static Behaviour EX1_402 = new Behaviour {
			Triggers = {
				OnDamage(FriendlyMinions, GainArmour(FriendlyHero, 1))
			}
		};*/
		// TODO: Tag-setting action

		// Imp Gang Boss
		public static Behaviour BRM_006 = new Behaviour {
			Triggers = {
				[OnDamage] = IsSelf > Summon(Controller, "BRM_006t")
			}
		};

		// Arathi Weaponsmith
		public static Behaviour EX1_398 = new Behaviour {
			Battlecry = Summon(Controller, "EX1_398t")
		};

		// Explosive Sheep
		public static Behaviour GVG_076 = new Behaviour {
			Deathrattle = Damage(AllMinions, 2)
		};

		// Deathlord
		public static Behaviour FP1_009 = new Behaviour {
			Deathrattle = Summon(Opponent, RandomOpponentMinionInDeck)
		};

		// Illidan Stormrage
		public static Behaviour EX1_614 = new Behaviour {
			Triggers = {
				[OnPlay] = IsFriendly > Summon(Controller, "EX1_614t")
			}
		};

		// Baron Geddon
		public static Behaviour EX1_249 = new Behaviour {
			Triggers = {
				[OnEndTurn] = IsFriendly > Damage(AllCharacters - Self, 2)
			}
		};

		// Damage Reflector
		public static Behaviour XXX_024 = new Behaviour {
			Triggers = {
				[OnDamage] = IsSelf > Damage(AllCharacters - Self, 1)
			}
		};
	}
}
