using System;
using System.Collections.Generic;
using System.Linq;

namespace Brimstone
{
	public abstract class QueueAction : ICloneable
	{
		public List<ActionGraph> Args { get; } = new List<ActionGraph>();
		public ActionResult[] CompiledArgs { get; set; }

		public abstract ActionResult Run(Game game, IEntity source, ActionResult[] args);

		public ActionGraph Then(ActionGraph g) {
			return ((ActionGraph) this).Then(g);
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