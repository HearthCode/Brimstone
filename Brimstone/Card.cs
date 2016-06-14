using System.Collections.Generic;

namespace Brimstone
{
	public class Card
	{
		public virtual string Id { get; set; }
		public virtual string Name { get; set; }
		public virtual Dictionary<GameTag, int> Tags { get; set; }
		public virtual Behaviour Behaviour { get; set; }

		public int? this[GameTag t] {
			get {
				// Use TryGetValue for safety
				return Tags[t];
			}
		}

		public override string ToString() {
			return "[CARD: " + Name + "]";
		}
	}
}