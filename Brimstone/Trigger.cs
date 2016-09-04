using System.Collections.Generic;

namespace Brimstone
{
	public class Trigger
	{
		public Condition Condition { get; }
		public List<QueueAction> Action { get; }= new List<QueueAction>();

		public Trigger(ActionGraph action, Condition condition = null) {
#if _TRIGGER_DEBUG
			DebugLog.WriteLine("Creating trigger " + type + " using " + action.Graph[0].GetType().Name);
#endif
			Condition = condition;
			Action = action.Unravel();
		}

		public Trigger(Trigger t) {
			// Only a shallow copy is necessary because Args and Action don't change and are lazily evaluated
			Condition = t.Condition;
			Action = t.Action;
		}

		public static implicit operator Trigger(ActionGraph g) {
			return new Trigger(g);
		}
	}
}
