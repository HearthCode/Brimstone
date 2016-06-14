using System.Collections.Generic;

namespace Brimstone
{
	public class PowerAction
	{
		public int EntityId { get; set; }

		public PowerAction(Entity e) {
			EntityId = e.Id;
		}

		public override string ToString() {
			return "Entity: " + EntityId;
		}
	}

	public class TagChange : PowerAction
	{
		public GameTag Key { get; set; }
		public int? Value { get; set; }

		public TagChange(Entity e) : base(e) { }

		public override string ToString() {
			return "[Tag] Entity " + EntityId + ": " + Key.ToString() + " = " + Value;
		}
	}

	public class CreateEntity : PowerAction
	{
		public Dictionary<GameTag, int?> Tags { get; set; }

		public CreateEntity(Entity e) : base(e) {
			// Make sure we copy the tags, not the references!
			Tags = new Dictionary<GameTag, int?>(e.Tags);
		}

		public override string ToString() {
			return "[Create] " + base.ToString();
		}
	}
}