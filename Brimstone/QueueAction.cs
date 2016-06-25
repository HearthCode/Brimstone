using System.Collections.Generic;

namespace Brimstone
{
	public abstract class QueueAction
	{
		public int SourceEntityId { get; set; }
		public List<ActionGraph> Args { get; } = new List<ActionGraph>();
		public abstract ActionResult Run(Game game, IEntity source, List<ActionResult> args);

		public override string ToString() {
			return "[ACTION: " + GetType().Name + ", SOURCE: " + SourceEntityId + "]";
		}
	}
}