using System.Collections.Generic;

namespace Brimstone
{
	public class Card
	{
		public string Id { get; set; }
		public string Name { get; set; }
		public Dictionary<GameTag, int> Tags { get; set; }
		public Dictionary<PlayRequirements, int> Requirements { get; set; }
		public CompiledBehaviour Behaviour { get; set; }

		public int this[GameTag t] {
			get {
				// TODO: Use TryGetValue for safety
				return Tags[t];
			}
		}

		public override string ToString() {
			return "[CARD: " + Name + "]";
		}
	}
}