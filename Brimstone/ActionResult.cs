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

using System;
using System.Collections.Generic;
using Brimstone.Entities;

namespace Brimstone
{
	public struct ActionResult : IEnumerable<IEntity>, ICloneable
	{
		private bool hasValue;
		private bool hasBoolValue;
		private bool hasIntValue;
		private bool hasCardValue;
		private bool hasEntityListValue;

		private bool boolValue;
		private int intValue;
		private Card cardValue;
		private List<IEntity> entityListValue;

		public bool HasResult { get { return hasValue || IsBlank; } }

		public bool IsBlank { get; set; }

		public static implicit operator ActionResult(int x) {
			return new ActionResult { hasValue = true, hasIntValue = true, intValue = x };
		}
		public static implicit operator ActionResult(bool x) {
			return new ActionResult { hasValue = true, hasBoolValue = true, boolValue = x };
		}
		public static implicit operator ActionResult(Entity x) {
			return new ActionResult { hasValue = true, hasEntityListValue = true, entityListValue = new List<IEntity> { x } };
		}
		public static implicit operator ActionResult(List<IEntity> x) {
			return new ActionResult { hasValue = true, hasEntityListValue = true, entityListValue = x };
		}
		public static implicit operator ActionResult(Card x) {
			return new ActionResult { hasValue = true, hasCardValue = true, cardValue = x };
		}
		public static implicit operator int(ActionResult a) {
			return a.intValue;
		}
		public static implicit operator bool(ActionResult a) {
			return a.boolValue;
		}
		public static implicit operator Entity(ActionResult a) {
			return a.entityListValue[0] as Entity;
		}
		public static implicit operator List<IEntity>(ActionResult a) {
			return a.entityListValue;
		}
		public static implicit operator Card(ActionResult a) {
			return a.cardValue;
		}
		public static bool operator ==(ActionResult x, ActionResult y) {
			if (ReferenceEquals(x, y))
				return true;
			// Also deals with one-sided null comparisons since it will use struct value type defaults
			// Do we need to compare the lists properly?
			return (x.boolValue == y.boolValue && x.intValue == y.intValue && x.entityListValue == y.entityListValue
				&& x.cardValue == y.cardValue);
		}
		public static bool operator !=(ActionResult x, ActionResult y) {
			return !(x == y);
		}

		public override bool Equals(object o) {
			try {
				return (this == (ActionResult)o);
			}
			catch {
				return false;
			}
		}

		public override int GetHashCode() {
			return ToString().GetHashCode();
		}

		public IEnumerator<IEntity> GetEnumerator() {
			return entityListValue.GetEnumerator();
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}

		public static ActionResult None = new ActionResult();
		public static ActionResult Empty = new ActionResult { IsBlank = true };

		public override string ToString() {
			if (!hasValue)
				return "<not evaluated>";
			else if (hasIntValue)
				return intValue.ToString();
			else if (hasBoolValue)
				return boolValue.ToString();
			else if (hasCardValue)
				return "<" + cardValue + ">";
			else if (hasEntityListValue) {
				string s = "<Entities:";
				foreach (var e in entityListValue)
					s += " (" + e + ")";
				s += ">";
				return s;
			}
			else
				return "<unknown>";
		}

		public object Clone() {
			return MemberwiseClone();
		}
	}
}