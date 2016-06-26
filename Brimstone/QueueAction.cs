using System;
using System.Collections.Generic;
using System.Linq;

namespace Brimstone
{
	public abstract class QueueAction
	{
		public int SourceEntityId { get; set; }
		public List<ActionGraph> Args { get; } = new List<ActionGraph>();
		public abstract ActionResult Run(Game game, IEntity source, List<ActionResult> args);

		public ActionResult Execute(Game game, IEntity source, List<ActionResult> args) {
			game.ActiveTriggers.Fire(TriggerEpoch.When, this, source, args);
			var result = Run(game, source, args);
			game.ActiveTriggers.Fire(TriggerEpoch.After, this, source, args);
			return result;
		}

		public override string ToString() {
			return "[ACTION: " + GetType().Name + ", SOURCE: " + SourceEntityId + "]";
		}
	}
}