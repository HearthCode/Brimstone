using System.Collections.Generic;

namespace Brimstone
{
	public struct ActionResult : IEnumerable<IEntity>
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

		public bool HasResult { get { return hasValue; } }

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
				return (bool)(this == (ActionResult)o);
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

		public override string ToString() {
			if (!hasValue)
				return "<none>";
			else if (hasIntValue)
				return "<int: " + intValue + ">";
			else if (hasBoolValue)
				return "<bool: " + boolValue.ToString() + ">";
			else if (hasCardValue)
				return "<card: " + cardValue + ">";
			else if (hasEntityListValue) {
				string s = "<Entities:";
				foreach (var e in entityListValue)
					s += e;
				s += ">";
				return s;
			}
			else
				return "<unknown>";
		}
	}
}