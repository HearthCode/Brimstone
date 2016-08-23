using System;
using System.Collections.Generic;
using System.Linq;

namespace Brimstone
{
	public abstract class QueueAction : ICloneable
	{
		public List<ActionGraph> Args { get; } = new List<ActionGraph>();
		public abstract ActionResult Run(Game game, IEntity source, List<ActionResult> args);

		// TODO: Find a way to get rid of source altogether (we only need it for Play and Select at the moment)
		public ActionResult Execute(Game game, IEntity source, List<ActionResult> args) {
			// TODO: The triggers are probably in the wrong place
			game.ActiveTriggers.Fire(TriggerEpoch.When, this, source, args);
			var result = Run(game, source, args);
			game.ActiveTriggers.Fire(TriggerEpoch.After, this, source, args);
			return result;
		}

		public override string ToString() {
			return GetType().Name;
		}

		public object Clone() {
			// A shallow copy is good enough: all properties and fields are value types
			// except for Args which is immutable
			return MemberwiseClone();
		}
	}
}