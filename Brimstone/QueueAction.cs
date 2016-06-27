using System;
using System.Collections.Generic;
using System.Linq;

namespace Brimstone
{
	public abstract class QueueAction : ICloneable
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

		public object Clone() {
			// A shallow copy is good enough: all properties and fields are value types
			// except for Args which is immutable
			return MemberwiseClone();
		}
	}
}