using System;
using System.Collections.Generic;

namespace Brimstone
{
	public struct Tag
	{
		public GameTag Name { get; set; }
		public Variant Value { get; set; }

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
	}
}