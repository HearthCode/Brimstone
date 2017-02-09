/*
	Copyright 2016, 2017 Katy Coe
	Copyright 2016 Leonard Dahlmann

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

using System;
using System.Collections.Generic;
using System.Linq;

namespace Brimstone
{
	/// <summary>
	/// Encapsulates the definition of a single card
	/// </summary>
	public class Card
	{
		public int AssetId { get; set; }
		public Guid Guid { get; set; }
		public string Id { get; set; }
		public string Name { get; set; }
		public Dictionary<GameTag, int> Tags { get; set; }
		public Dictionary<PlayRequirements, int> Requirements { get; set; }
		public CardBehaviour Behaviour { get; set; }

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

		public bool HasCombo {
			get { return this[GameTag.COMBO] == 1; }
		}

		public Rarity Rarity {
			get { return (Rarity) this[GameTag.RARITY]; }
		}

		public CardType Type {
			get { return (CardType) this[GameTag.CARDTYPE]; }
		}

		public int Cost {
			get { return this[GameTag.COST]; }
		}

		public bool HasOverload {
			get { return this[GameTag.OVERLOAD] == 1; }
		}

		public int Overload {
			get { return this[GameTag.OVERLOAD_OWED]; }
		}

		public bool RequiresTarget {
			get { return Requirements.ContainsKey(PlayRequirements.REQ_TARGET_TO_PLAY); }
		}

		public bool RequiresTargetIfAvailable {
			get { return Requirements.ContainsKey(PlayRequirements.REQ_TARGET_IF_AVAILABLE); }
		}

		public int MaxAllowedInDeck {
			get { return Rarity == Rarity.LEGENDARY ? 1 : 2; }
		}

		public string AbbrieviatedName {
			get { return new string(Name.Split(new[] {' '}).Select(word => word.First()).ToArray()); }
		}

		public static implicit operator Card(string name) {
			return Cards.FromName(name) ?? Cards.FromId(name);
		}

		public override string ToString() {
			return "[CARD: " + Name + "]";
		}
	}
}
