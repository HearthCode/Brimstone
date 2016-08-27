using System;
using System.Collections.Generic;
using System.Linq;

namespace Brimstone
{
	public partial class Card
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

		public string AbbrieviatedName {
			get { return new string(Name.Split(new[] { ' ' }).Select(word => word.First()).ToArray()); }
		}

		public static implicit operator Card(string name) {
			return Cards.FromName(name) ?? Cards.FromId(name);
		}

		public override string ToString() {
			return "[CARD: " + Name + "]";
		}
	}
}
