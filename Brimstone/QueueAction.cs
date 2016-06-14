using System.Collections.Generic;

namespace Brimstone
{
	public abstract class QueueAction
	{
		public Game Game { get; set; }
		public List<ActionGraph> Args { get; } = new List<ActionGraph>();
		public abstract ActionResult Run(List<ActionResult> args);

		public override string ToString() {
			return "[ACTION: " + this.GetType().Name + "]";
		}
	}
}