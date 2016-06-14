using System.Collections.Generic;

namespace Brimstone
{
	public abstract class QueueAction
	{
		public List<ActionGraph> Args { get; } = new List<ActionGraph>();
		public abstract ActionResult Run(Game game, List<ActionResult> args);

		public override string ToString() {
			return "[ACTION: " + this.GetType().Name + "]";
		}
	}
}