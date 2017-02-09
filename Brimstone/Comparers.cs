/*
	Copyright 2016, 2017 Katy Coe

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

using System.Collections.Generic;
using Brimstone.Entities;
using Brimstone.PowerActions;

namespace Brimstone
{
	public class FuzzyEntityComparer : IEqualityComparer<IEntity>
	{
		// Used when adding to and fetching from HashSet, and testing for equality
		public bool Equals(IEntity x, IEntity y) {
			return x.FuzzyHash == y.FuzzyHash;
		}

		public int GetHashCode(IEntity obj) {
			return obj.FuzzyHash;
		}
	}

	public class FuzzyGameComparer : IEqualityComparer<Game>
	{
		// Used when adding to and fetching from HashSet, and testing for equality
		public bool Equals(Game x, Game y) {
			if (ReferenceEquals(x, y))
				return true;
			if (Settings.UseGameHashForEquality)
				return x.FuzzyGameHash == y.FuzzyGameHash;
			return x.PowerHistory.EquivalentTo(y.PowerHistory);
		}

		public int GetHashCode(Game obj) {
			return obj.FuzzyGameHash;
		}
	}

	public class EntityAndTagNameComparer : IEqualityComparer<TagChange>
	{
		// Used when adding to and fetching from HashSet, and testing for equality
		public bool Equals(TagChange x, TagChange y) {
			return x.EntityId == y.EntityId && x.Tag.Name == y.Tag.Name;
		}

		public int GetHashCode(TagChange obj) {
			int hash = 17 * 31 + obj.EntityId;
			return hash * 31 + (int)obj.Tag.Name;
		}
	}
}
