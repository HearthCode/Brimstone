using System;

namespace Brimstone
{
	public struct TagValue : IEquatable<TagValue>
	{
		public bool HasValue { get; private set; }
		public bool HasBoolValue { get; private set; }
		public bool HasIntValue { get; private set; }
		public bool HasStringValue { get; private set; }

		private bool boolValue;
		private int intValue;
		private string stringValue;

		public static implicit operator TagValue(int x) {
			return new TagValue {HasValue = true, HasIntValue = true, intValue = x};
		}

		public static implicit operator TagValue(bool x) {
			return new TagValue {HasValue = true, HasBoolValue = true, boolValue = x};
		}

		public static implicit operator TagValue(string x) {
			return new TagValue {HasValue = true, HasStringValue = true, stringValue = x};
		}

		public static implicit operator int(TagValue a) {
			return a.intValue;
		}

		public static implicit operator bool(TagValue a) {
			return a.boolValue;
		}

		public static implicit operator string(TagValue a) {
			return a.stringValue;
		}

		public static bool operator ==(TagValue x, TagValue y) {
			if (ReferenceEquals(x, null))
				return false;
			return x.Equals(y);
		}

		public static bool operator !=(TagValue x, TagValue y) {
			return !(x == y);
		}

		public override bool Equals(object o) {
			if (!(o is TagValue))
				return false;
			return Equals((TagValue) o);
		}

		public bool Equals(TagValue o) {
			if (ReferenceEquals(o, null))
				return false;
			if (ReferenceEquals(this, o))
				return true;
			// Both must have a value or no value
			if (HasValue != o.HasValue)
				return false;
			// If neither have a value, they are equal
			if (!(HasValue || o.HasValue))
				return true;
			// Precedence order: int -> bool -> string
			if (HasIntValue && o.HasIntValue)
				return intValue == o.intValue;
			if (HasBoolValue && o.HasBoolValue)
				return boolValue == o.boolValue;
			if (HasStringValue && o.HasStringValue)
				return stringValue == o.stringValue;
			return false;
		}

		public override string ToString() {
			if (!HasValue)
				return "null";
			if (HasBoolValue)
				return boolValue.ToString();
			if (HasIntValue)
				return intValue.ToString();
			if (HasStringValue)
				return stringValue.ToString();
			return "unknown";
		}

		public override int GetHashCode() {
			return ToString().GetHashCode();
		}
	}
}
