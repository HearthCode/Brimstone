using System.Collections.Generic;

public struct Variant
{
	public bool HasValue { get; private set; }
	public bool HasBoolValue { get; private set; }
	public bool HasIntValue { get; private set; }
	public bool HasStringValue { get; private set; }

	private bool boolValue;
	private int intValue;
	private string stringValue;

	public static implicit operator Variant(int? x) {
		return new Variant { HasValue = x.HasValue, HasIntValue = x.HasValue, intValue = (x.HasValue? x.Value : 0) };
	}
	public static implicit operator Variant(bool x) {
		return new Variant { HasValue = true, HasBoolValue = true, boolValue = x };
	}
	public static implicit operator Variant(string x) {
		return new Variant { HasValue = true, HasStringValue = true, stringValue = x };
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
		if (ReferenceEquals(x, y))
			return true;
		// Also deals with one-sided null comparisons since it will use struct value type defaults
		// Do we need to compare the lists properly?
		return (x.boolValue == y.boolValue && x.intValue == y.intValue && x.stringValue == y.stringValue);
	}

	public static bool operator !=(Variant x, Variant y) {
		return !(x == y);
	}

	public override bool Equals(object o) {
		try {
			return (bool)(this == (Variant)o);
		}
		catch {
			return false;
		}
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
