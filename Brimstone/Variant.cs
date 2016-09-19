using System;

namespace Brimstone
{
	public struct Variant : IEquatable<Variant>
	{
		public bool HasValue { get; private set; }
		public bool HasBoolValue { get; private set; }
		public bool HasIntValue { get; private set; }
		public bool HasStringValue { get; private set; }

		private bool boolValue;
		private int intValue;
		private string stringValue;

		public static implicit operator Variant(int x) {
			return new Variant {HasValue = true, HasIntValue = true, intValue = x};
		}

		public static implicit operator Variant(bool x) {
			return new Variant {HasValue = true, HasBoolValue = true, boolValue = x};
		}

		public static implicit operator Variant(string x) {
			return new Variant {HasValue = true, HasStringValue = true, stringValue = x};
		}

		public static implicit operator int(Variant a) {
			return a.intValue;
		}

		public static implicit operator bool(Variant a) {
			return a.boolValue;
		}

		public static implicit operator string(Variant a) {
			return a.stringValue;
		}

		public static bool operator ==(Variant x, Variant y) {
			if (ReferenceEquals(x, null))
				return false;
			return x.Equals(y);
		}

		public static bool operator !=(Variant x, Variant y) {
			return !(x == y);
		}

		public override bool Equals(object o) {
			if (!(o is Variant))
				return false;
			return Equals((Variant) o);
		}

		public bool Equals(Variant o) {
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
