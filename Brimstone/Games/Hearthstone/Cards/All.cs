/*
	Copyright 2016, 2017 Katy Coe
	Copyright 2016 Timothy Stiles

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

using static Brimstone.Actions;
using static Brimstone.TriggerType;
using Behaviour = Brimstone.CardBehaviourGraph;

namespace Brimstone.Games.Hearthstone
{
	internal partial class Cards
	{
		// Basic, Neutral

		// Raid Leader
		// internal static Behaviour CS2_122 = new Behaviour {
		// TODO: Combining selectors, Enchantment-refreshing Auras (CS2_122e)

		// Stormwind Champion
		// internal static Behaviour CS2_222 = new Behaviour {
		// TODO: Combining selectors, Enchantment-refreshing Auras (CS2_222o)

		// Frostwolf Warlord
		// Battlecry =
		// TODO: Counting selectors, Enchantments (CS2_266e)

		// Voodoo Doctor
		internal static Behaviour EX1_011 = new Behaviour {
			Battlecry = Heal(Target, 2)
		};

		// Novice Engineer
		internal static Behaviour EX1_015 = new Behaviour {
			Battlecry = Draw(Controller)
		};

		// Mad Bomber
		internal static Behaviour EX1_082 = new Behaviour {
			Battlecry = Damage(Random(AllHealthyCharacters - Self), 1) * 3
		};

		// Demolisher
		internal static Behaviour EX1_102 = new Behaviour {
			Triggers = {
				[OnBeginTurn] = IsFriendly > Damage(RandomOpponentHealthyCharacter, 2)
			}
		};

		// Dire Wolf Alpha
		// internal static Behaviour EX1_163 = new Behaviour {
		// TODO: Enchantment-refreshing Auras (EX1_162o)

		// Gurubashi Berserker
		// internal static Behaviour EX1_399 = new Behaviour {
		// TODO: Enchantments (EX1_508o)

		// Nightblade
		internal static Behaviour EX1_593 = new Behaviour {
			Battlecry = Damage(OpponentHero, 3)
		};

		// Cult Master
		internal static Behaviour EX1_595 = new Behaviour {
			Triggers = {
				[OnDeath] = FriendlyMinions - Self > Draw(Controller)
			}
		};

		// Classic, Neutral, Common

		// Earthen Ring Farseer
		internal static Behaviour CS2_117 = new Behaviour {
			Battlecry = Heal(Target, 3)
		};

		// Ironforge Rifleman
		internal static Behaviour CS2_141 = new Behaviour {
			Battlecry = Damage(Target, 2)
		};

		// Silver Hand Knight
		internal static Behaviour CS2_151 = new Behaviour {
			Battlecry = Summon(Controller, "CS2_152")
		};

		// Elven Archer
		internal static Behaviour CS2_189 = new Behaviour {
			Battlecry = Damage(Target, 1)
		};

		// Abusive Sergeant
		// internal static Behaviour CS2_188 = new Behaviour {
		// TODO: Enchantments (CS2_188o)

		// Razorfen Hunter
		internal static Behaviour CS2_196 = new Behaviour {
			Battlecry = Summon(Controller, "CS2_boar")
		};

		// Ironbeak Owl
		internal static Behaviour CS2_203 = new Behaviour {
			Battlecry = Silence(Target)
		};

		// Spiteful Smith
		// internal static Behaviour CS2_221 = new Behaviour {
		// TODO: Enrage, Enchantment-refreshing Auras (CS2_221e)

		// Venture Co. Mercenary
		// internal static Behaviour CS2_227 = new Behaviour {
		// TODO: Tag-refreshing Auras

		// Darkscale Healer
		internal static Behaviour DS1_055 = new Behaviour {
			Battlecry = Heal(FriendlyCharacters, 2)
		};

		// Acolyte of Pain
		internal static Behaviour EX1_007 = new Behaviour {
			Triggers = {
				[OnDamage] = IsSelf > Draw(Controller)
			}
		};

		// Shattered Sun Cleric
		// internal static Behaviour EX1_019 = new Behaviour {
		// TODO: Enchantments (EX1_019e)

		// Dragonling Mechanic
		internal static Behaviour EX1_025 = new Behaviour {
			Battlecry = Summon(Controller, "EX1_025t")
		};

		// Leper Gnome
		internal static Behaviour EX1_029 = new Behaviour {
			Deathrattle = Damage(OpponentHero, 2)
		};

		// Dark Iron Dwarf
		/*internal static Behaviour EX1_046 = new Behaviour {
			Battlecry =
		};*/
		// TODO: Enchantments (EX1_046e)

		// Spellbreaker
		internal static Behaviour EX1_048 = new Behaviour {
			Battlecry = Silence(Target)
		};

		// Youthful Brewmaster
		internal static Behaviour EX1_049 = new Behaviour {
			Battlecry = Bounce(Target)
		};

		// Ancient Brewmaster
		internal static Behaviour EX1_057 = new Behaviour {
			Battlecry = Bounce(Target)
		};

		// Acidic Swamp Ooze
		/*internal static Behaviour EX1_066 = new Behaviour {
			Battlecry = Destroy(OpponentWeapon)
		};*/
		// TODO: Weapon selectors

		// Loot Hoarder
		internal static Behaviour EX1_096 = new Behaviour {
			Deathrattle = Draw(Controller)
		};

		// Tauren Warrior
		// internal static Behaviour EX1_390 = new Behaviour {
		// TODO: Enrage, Enchantments (EX1_390e)

		// Amani Berserker
		// internal static Behaviour EX1_393 = new Behaviour {
		// TODO: Enrage, Enchantments (EX1_393e)

		// Raging Worgen
		// internal static Behaviour EX1_412 = new Behaviour {
		// TODO: Enrage, Enchantments, gaining Windfury (EX1_412e)

		// Murloc Tidehunter
		internal static Behaviour EX1_506 = new Behaviour {
			Battlecry = Summon(Controller, "EX1_506a")
		};

		// Harvest Golem
		internal static Behaviour EX1_556 = new Behaviour {
			Deathrattle = Summon(Controller, "skele21")
		};

		// Priestess of Elune
		internal static Behaviour EX1_583 = new Behaviour {
			Battlecry = Heal(FriendlyHero, 4)
		};

		// Bloodsail Raider
		// internal static Behaviour NEW1_018 = new Behaviour {
		// TODO: Enchantments (NEW1_018e)

		// Dread Corsair
		// internal static Behaviour NEW1_022 = new Behaviour {
		// TODO: Tag-refreshing Auras

		// Flesheating Ghoul
		// internal static Behaviour tt_004 = new Behaviour {
		// TODO: Enchantments (tt_004o)

		// unused buffs: CS2_181e = buff(atk=2)

		// Classic, Neutral, Rare

		// Injured Blademaster
		internal static Behaviour CS2_181 = new Behaviour {
			Battlecry = Damage(Self, 4)
		};

		// Lightwarden
		// internal static Behaviour EX1_001 = new Behaviour {
		// TODO: Enchantments (EX1_001e)

		// Young Priestess
		// internal static Behaviour EX1_004 = new Behaviour {
		// TODO: Enchantments (EX1_004e)

		// Alarm-o-Bot
		/*internal static Behaviour EX1_006 = new Behaviour {
			Triggers = {
				OnBeginTurn(Controller, SwapHandAndPlay(RandomFriendlyMinionInHand, Self))
			}
		};*/
		//TODO: Queuing/trigger conditions 'minion exists in friendly hand' http://i.imgur.com/ov58pCN.png

		// Angry Chicken
		// internal static Behaviour EX1_009 = new Behaviour {
		// TODO: Enrage, Enchantments (EX1_009e)

		// Twilight Drake
		// internal static Behaviour EX1_043e = new Behaviour {
		// TODO: Counting selectors, Enchantments (EX1_043e)

		// Questing Adventurer
		// internal static Behaviour EX1_044e = new Behaviour {
		// TODO: Enchantments (EX1_044e)

		// Coldlight Oracle
		internal static Behaviour EX1_050 = new Behaviour {
			Battlecry = (Draw(Controller) * 2).Then(Draw(Opponent) * 2)
		};

		// Mana Addict
		// internal static Behaviour EX1_055 = new Behaviour {
		// TODO: Conditional selectors, Enchantments (EX1_055o)

		// Sunfury Protector
		/*internal static Behaviour EX1_058 = new Behaviour {
			Battlecry = SetTag(AdjacentMinions, GameTag.TAUNT, 1)
		};*/
		// TODO: Tag setting action

		// Crazed Alchemist
		// internal static Behaviour EX1_059 = new Behaviour {
		// TODO: Enchantments (EX1_059e)

		// Pint-Sized Summoner
		// internal static Behaviour EX1_076 = new Behaviour {
		// TODO: Tag-refreshing Auras

		// Secretkeeper
		// internal static Behaviour EX1_080 = new Behaviour {
		// TODO: Conditional selectors, Enchantments (EX1_080o)

		// Mind Control Tech
		// internal static Behaviour EX1_085 = new Behaviour {
		// TODO: Counting selectors

		// Arcane Golem
		internal static Behaviour EX1_089 = new Behaviour {
			Battlecry = GainMana(Opponent, 1)
		};

		// Defender of Argus
		// internal static Behaviour EX1_093 = new Behaviour {
		// TODO: Enchantments providing Taunt, Enchantments (EX1_093e)

		// Gadgetzan Auctioneer
		internal static Behaviour EX1_095 = new Behaviour {
			Triggers = {
				[OnPlay] = IsFriendlySpell > Draw(Controller)
			}
		};

		// Abomination
		internal static Behaviour EX1_097 = new Behaviour {
			Deathrattle = Damage(AllCharacters, 2)
		};

		// Coldlight Seer
		// internal static Behaviour EX1_103 = new Behaviour {
		// TODO: Combining selectors, Enchantments (EX1_103e)

		// Azure Drake
		internal static Behaviour EX1_284 = new Behaviour {
			Battlecry = Draw(Controller)
		};

		// Murloc Tidecaller
		// internal static Behaviour EX1_509 = new Behaviour {
		// TODO: Combining selectors, Enchantments (EX1_509e)

		// Ancient Mage
		// internal static Behaviour EX1_584 = new Behaviour {
		// TODO: Enchantments (EX1_584e)

		// Imp Master
		internal static Behaviour EX1_597 = new Behaviour {
			Triggers = {
				[OnEndTurn] = IsFriendly > Damage(Self, 1).Then(Summon(Controller, "EX1_598"))
			}
		};

		// Mana Wraith
		// internal static Behaviour EX1_616 = new Behaviour {
		// TODO: Tag-refreshing Auras

		// Knife Juggler
		internal static Behaviour NEW1_019 = new Behaviour {
			Triggers = {
				[AfterSummon] = IsFriendly > Damage(RandomOpponentHealthyMinion, 1)
			}
		};

		// Wild Pyromancer
		internal static Behaviour NEW1_020 = new Behaviour {
			Triggers = {
				[AfterPlay] = IsFriendlySpell > Damage(AllMinions, 1)
			}
		};

		// Bloodsail Corsair
		/*internal static Behaviour New1_025 = new Behaviour {
			Battlecry = Damage(OpponentWeapon, 1)
		};*/
		// TODO: Weapon selectors

		// Violet Teacher
		internal static Behaviour NEW1_026 = new Behaviour {
			Triggers = {
				[OnPlay] = IsFriendlySpell > Summon(Controller, "NEW1_026t")
			}
		};

		// Master Swordsmith
		// internal static Behaviour NEW1_037 = new Behaviour {
		// TODO: Combining selectors, Enchantments (NEW1_037e)

		// Stampeding Kodo
		// internal static Behaviour NEW1_041 = new Behaviour {
		// TODO: Combining selectors

		// Unsorted

		// Flame Juggler
		internal static Behaviour AT_094 = new Behaviour {
			Battlecry = Damage(RandomOpponentHealthyCharacter, 1)
		};

		// Boom Bot
		internal static Behaviour GVG_110t = new Behaviour {
			Deathrattle = Damage(RandomOpponentHealthyCharacter, RandomAmount(1, 4))
		};

		// Whirlwind
		internal static Behaviour EX1_400 = new Behaviour {
			Battlecry = Damage(AllMinions, 1)
		};

		// Arcane Missiles
		internal static Behaviour EX1_277 = new Behaviour {
			Battlecry = Damage(RandomOpponentHealthyCharacter, 1) * 3
		};

		// Fireball
		internal static Behaviour CS2_029 = new Behaviour {
			Battlecry = Damage(Target, 6)
		};

		// Pyroblast
		internal static Behaviour EX1_279 = new Behaviour {
			Battlecry = Damage(Target, 10)
		};

		// Arcane Explosion
		internal static Behaviour CS2_025 = new Behaviour {
			Battlecry = Damage(OpponentMinions, 1)
		};

		// Flamestrike
		internal static Behaviour CS2_032 = new Behaviour {
			Battlecry = Damage(OpponentMinions, 4)
		};

		// Moonfire
		internal static Behaviour CS2_008 = new Behaviour {
			Battlecry = Damage(Target, 1)
		};

		// Holy Smite
		internal static Behaviour CS1_130 = new Behaviour {
			Battlecry = Damage(Target, 2)
		};

		// Darkbomb
		internal static Behaviour GVG_015 = new Behaviour {
			Battlecry = Damage(Target, 3)
		};

		// Armorsmith
		/*internal static Behaviour EX1_402 = new Behaviour {
			Triggers = {
				OnDamage(FriendlyMinions, GainArmour(FriendlyHero, 1))
			}
		};*/
		// TODO: Tag-setting action

		// Imp Gang Boss
		internal static Behaviour BRM_006 = new Behaviour {
			Triggers = {
				[OnDamage] = IsSelf > Summon(Controller, "BRM_006t")
			}
		};

		// Arathi Weaponsmith
		internal static Behaviour EX1_398 = new Behaviour {
			Battlecry = Summon(Controller, "EX1_398t")
		};

		// Explosive Sheep
		internal static Behaviour GVG_076 = new Behaviour {
			Deathrattle = Damage(AllMinions, 2)
		};

		// Deathlord
		internal static Behaviour FP1_009 = new Behaviour {
			Deathrattle = Summon(Opponent, RandomOpponentMinionInDeck)
		};

		// Illidan Stormrage
		// TODO: This is triggering in hand!
		internal static Behaviour EX1_614 = new Behaviour {
			Triggers = {
				[OnPlay] = IsFriendly > Summon(Controller, "EX1_614t")
			}
		};

		// Baron Geddon
		internal static Behaviour EX1_249 = new Behaviour {
			Triggers = {
				[OnEndTurn] = IsFriendly > Damage(AllCharacters - Self, 2)
			}
		};

		// Damage Reflector
		internal static Behaviour XXX_024 = new Behaviour {
			Triggers = {
				[OnDamage] = IsSelf > Damage(AllCharacters - Self, 1)
			}
		};

		// Fiery Bat
		internal static Behaviour OG_179 = new Behaviour {
			Deathrattle = Damage(RandomOpponentHealthyCharacter, 1)
		};

		// Cursed!
		internal static Behaviour LOE_007t = new Behaviour {
			TriggersByZone = {
				[Zone.HAND] = {
					[OnBeginTurn] = IsFriendly > Damage(FriendlyHero, 2)
				}
			}
		};

		// Gnomish Inventor
		internal static Behaviour CS2_147 = new Behaviour {
			Battlecry = Draw(Controller)
		};

		// Arcane Intellect
		internal static Behaviour CS2_023 = new Behaviour {
			Battlecry = Draw(Controller)*2
		};

		// Twilight Flamecaller
		internal static Behaviour OG_083 = new Behaviour {
			Battlecry = Damage(OpponentMinions, 1)
		};

		// Anomalus
		internal static Behaviour OG_120 = new Behaviour {
			Deathrattle = Damage(AllMinions, 8)
		};

		// Runic Egg
		internal static Behaviour KAR_029 = new Behaviour {
			Deathrattle = Draw(Controller)
		};

		// North Sea Kraken
		internal static Behaviour AT_103 = new Behaviour {
			Battlecry = Damage(Target, 4)
		};
	}
}
