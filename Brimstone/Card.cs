using System;
using System.Collections.Generic;
using System.Linq;

namespace Brimstone
{
	public class Card
	{
		public int AssetId { get; set; }
		public Guid Guid { get; set; }
		public string Id { get; set; }
		public string Name { get; set; }
		public Dictionary<GameTag, int> Tags { get; set; }
		public Dictionary<PlayRequirements, int> Requirements { get; set; }
		public CompiledBehaviour Behaviour { get; set; }

		public int this[GameTag t] {
			get {
				if (Tags.ContainsKey(t))
					return Tags[t];
				else
					return 0;
			}
		}

		public bool Collectible {
			get { return this[GameTag.COLLECTIBLE] == 1; }
		}

		public CardClass Class {
			get { return (CardClass)this[GameTag.CLASS]; }
		}

		public bool HasCombo {
			get { return this[GameTag.COMBO] == 1; }
		}

		public Rarity Rarity {
			get { return (Rarity)this[GameTag.RARITY]; }
		}

		public CardType Type {
			get { return (CardType)this[GameTag.CARDTYPE]; }
		}

		public bool RequiresTarget {
			get {
				return Requirements.ContainsKey(PlayRequirements.REQ_TARGET_TO_PLAY);
			}
		}

		public bool RequiresTargetIfAvailable {
			get {
				return Requirements.ContainsKey(PlayRequirements.REQ_TARGET_IF_AVAILABLE);
			}
		}

		public int MaxAllowedInDeck {
			get { return Rarity == Rarity.LEGENDARY ? 1 : 2; }
		}

		public string AbbrieviatedName {
			get {
				return new string(Name.Split(new[] { ' ' }).Select(word => word.First()).ToArray());
			}
		}

		public static implicit operator Card(string name) {
			return Cards.FromName(name) ?? Cards.FromId(name);
		}

		public override string ToString() {
			return "[CARD: " + Name + "]";
		}
	}
}
