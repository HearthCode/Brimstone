using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Brimstone
{
	public static class TagFormatter
	{
		public static string Format(this KeyValuePair<GameTag, int?> tag) {
			string s = tag.Key + " = ";
			try {
				s += GetValue(tag.Key, tag.Value);
			}
			catch (Exception) {
				s += tag.Value;
			}
			return s;
		}

		private static string GetValue(GameTag tag, int? value) {
			string result = value.ToString();
			if (TypedTags.ContainsKey(tag)) {
				result = Enum.GetName(TypedTags[tag], value);
			}
			return result;
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
				// { GameTag.FACTION, typeof(TAG_FACTION) },
				{ GameTag.CARDRACE, typeof(Race) }
				// { GameTag.ENCHANTMENT_BIRTH_VISUAL, typeof(TAG_ENCHANTMENT_VISUAL) },
				// { GameTag.ENCHANTMENT_IDLE_VISUAL, typeof(TAG_ENCHANTMENT_VISUAL) },
				// { GameTag.GOLD_REWARD_STATE, typeof(TAG_GOLD_REWARD_STATE) }
			};
	}
}
