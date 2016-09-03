#define _TRIGGER_DEBUG

using System.Collections.Generic;

namespace Brimstone
{
	public class Trigger
	{
		public Condition Condition { get; }
		public List<QueueAction> Action { get; }= new List<QueueAction>();
		public TriggerType Type { get; }

		public Trigger(TriggerType type, ActionGraph action, Condition condition = null) {
#if _TRIGGER_DEBUG
			DebugLog.WriteLine("Creating trigger " + type + " using " + action.Graph[0].GetType().Name);
#endif
			Condition = condition;
			Action = action.Unravel();
			Type = type;
		}

		public Trigger(Trigger t) {
			// Only a shallow copy is necessary because Args and Action don't change and are lazily evaluated
			Condition = t.Condition;
			Action = t.Action;
			Type = t.Type;
		}

		public static Trigger At(TriggerType type, ActionGraph g, Condition condition = null) {
			return new Trigger(type, g, condition);
		}
	}
}
