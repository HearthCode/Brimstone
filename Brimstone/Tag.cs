using System;
using System.Collections.Generic;
using Brimstone.Entities;

namespace Brimstone
{
	public struct Tag : IEquatable<Tag>
	{
		public GameTag Name { get;}
		public Variant Value { get; }

		public Tag(GameTag Name, Variant Value) {
			this.Name = Name;
			this.Value = Value;
		}

		public override string ToString() {
			string s = Name.ToString() + " = ";
			string v = Value.ToString();
			try {
				if (TypedTags.ContainsKey(Name))
					v = Enum.GetName(TypedTags[Name], (int)Value);
			}
			catch (Exception) { }
			return s + v;
		}

		public Tag? Filtered(IEntity e) {
			if (AlwaysIncludeFilters.Contains(Name))
				return this;
			if (AlwaysExcludeFilters.Contains(Name))
				return null;

			if (e is Game)
				return (GameIncludeFilters.Contains(Name) ? (Tag?)this : null);
			if (e is Player)
				return (PlayerIncludeFilters.Contains(Name) ? (Tag?)this : null);

			// Never include zone positions in decks
			if (Name == GameTag.ZONE_POSITION && e[GameTag.ZONE] == (int) Zone.DECK)
				return null;

			return this;
		}

		public static bool operator ==(Tag x, Tag y) {
			if (ReferenceEquals(x, null))
				return false;
			return x.Equals(y);
		}

		public static bool operator !=(Tag x, Tag y) {
			return !(x == y);
		}

		public override bool Equals(object o) {
			if (!(o is Tag))
				return false;
			return Equals((Tag)o);
		}

		public bool Equals(Tag o) {
			if (ReferenceEquals(o, null))
				return false;
			if (ReferenceEquals(this, o))
				return true;
			return Name == o.Name && Value == o.Value;
		}

		public override int GetHashCode() {
			return (17 * 31 + (int)Name) * 31 + Value.ToString().GetHashCode();
		}

		private static Dictionary<GameTag, Type> TypedTags = new Dictionary<GameTag, Type> {
				{ GameTag.STATE, typeof(GameState) },
				{ GameTag.ZONE, typeof(Zone) },
				{ GameTag.STEP, typeof(Step) },
				{ GameTag.NEXT_STEP, typeof(Step) },
				{ GameTag.PLAYSTATE, typeof(PlayState) },
				{ GameTag.CARDTYPE, typeof(CardType) },
				{ GameTag.MULLIGAN_STATE, typeof(MulliganState) },
				{ GameTag.CARD_SET, typeof(CardSet) },
				{ GameTag.CLASS, typeof(CardClass) },
				{ GameTag.RARITY, typeof(Rarity) },
				{ GameTag.FACTION, typeof(Faction) },
				{ GameTag.CARDRACE, typeof(Race) },
				{ GameTag.ENCHANTMENT_BIRTH_VISUAL, typeof(EnchantmentVisual) },
				{ GameTag.ENCHANTMENT_IDLE_VISUAL, typeof(EnchantmentVisual) },
				{ GameTag.GOLD_REWARD_STATE, typeof(GoldRewardState) }
			};

		public static List<GameTag> AlwaysIncludeFilters = new List<GameTag> {
			GameTag.CARDTYPE,
			GameTag.ENTITY_ID,
			GameTag.ZONE,
		};

		public static List<GameTag> AlwaysExcludeFilters = new List<GameTag> {
			GameTag.ATTACK_VISUAL_TYPE,
			GameTag.CARD_SET,
			GameTag.CARDRACE,
			GameTag.COLLECTIBLE,
			GameTag.DEV_STATE,
			GameTag.ENCHANTMENT_BIRTH_VISUAL,
			GameTag.ENCHANTMENT_IDLE_VISUAL,
		};

		public static List<GameTag> GameIncludeFilters = new List<GameTag> {
			GameTag.NEXT_STEP,
			GameTag.NUM_MINIONS_KILLED_THIS_TURN,
			GameTag.PROPOSED_ATTACKER,
			GameTag.PROPOSED_DEFENDER,
			GameTag.STATE,
			GameTag.STEP,
			GameTag.TURN,
		};

		public static List<GameTag> PlayerIncludeFilters = new List<GameTag> {
			GameTag.CANT_DRAW,
			GameTag.COMBO_ACTIVE,
			GameTag.CONTROLLER,
			GameTag.CURRENT_PLAYER,
			GameTag.CURRENT_SPELLPOWER,
			GameTag.EMBRACE_THE_SHADOW,
			GameTag.FATIGUE,
			GameTag.FIRST_PLAYER,
			GameTag.GOLD_REWARD_STATE,
			GameTag.HEALING_DOUBLE,
			GameTag.HERO_ENTITY,
			GameTag.HEROPOWER_ACTIVATIONS_THIS_TURN,
			GameTag.LAST_CARD_PLAYED,
			GameTag.MAXHANDSIZE,
			GameTag.MAXRESOURCES,
			GameTag.MULLIGAN_STATE,
			GameTag.NUM_CARDS_DRAWN_THIS_TURN,
			GameTag.NUM_CARDS_PLAYED_THIS_TURN,
			GameTag.NUM_FRIENDLY_MINIONS_THAT_ATTACKED_THIS_TURN,
			GameTag.NUM_FRIENDLY_MINIONS_THAT_DIED_THIS_TURN,
			GameTag.NUM_FRIENDLY_MINIONS_THAT_DIED_THIS_GAME,
			GameTag.NUM_MINIONS_PLAYED_THIS_TURN,
			GameTag.NUM_MINIONS_PLAYER_KILLED_THIS_TURN,
			GameTag.NUM_OPTIONS_PLAYED_THIS_TURN,
			GameTag.NUM_RESOURCES_SPENT_THIS_GAME,
			GameTag.NUM_TIMES_HERO_POWER_USED_THIS_GAME,
			GameTag.NUM_TURNS_LEFT,
			GameTag.OVERLOAD_LOCKED,
			GameTag.OVERLOAD_OWED,
			GameTag.OVERLOAD_THIS_GAME,
			GameTag.PLAYER_ID,
			GameTag.PLAYSTATE,
			GameTag.PROXY_CTHUN,
			GameTag.RESOURCES,
			GameTag.RESOURCES_USED,
			GameTag.SEEN_CTHUN,
			GameTag.SPELLPOWER_DOUBLE,
			GameTag.STARTHANDSIZE,
			GameTag.HERO_POWER_DOUBLE,
			GameTag.TEAM_ID,
			GameTag.TEMP_RESOURCES,
			GameTag.TIMEOUT,
			GameTag.TURN_START,
		};
	}
}