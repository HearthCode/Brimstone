using System;
using System.Collections.Generic;

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
			get { return (CardClass) this[GameTag.CLASS]; }
		}

		public CardType Type {
			get { return (CardType)this[GameTag.CARDTYPE]; }
		}

		public int MaxAllowedInDeck {
			get { return this[GameTag.RARITY] == (int)Rarity.LEGENDARY ? 1 : 2; }
		}

		public static implicit operator Card(string name) {
			return Cards.FromName(name) ?? Cards.FromId(name);
		}

		public override string ToString() {
			return "[CARD: " + Name + "]";
		}
	}
}