#define _TRIGGER_DEBUG

using System.Collections.Generic;

namespace Brimstone
{
	public interface ITrigger {
		ICondition Condition { get; }
		List<QueueAction> Action { get; }
		TriggerType Type { get; }
	}

	public class Trigger<T, U> : ITrigger where T : IEntity where U : IEntity
	{
		ICondition ITrigger.Condition => Condition;
		public Condition<T, U> Condition { get; }
		public List<QueueAction> Action { get; }= new List<QueueAction>();
		public TriggerType Type { get; }

		public Trigger(TriggerType type, ActionGraph action, Condition<T, U> condition = null) {
#if _TRIGGER_DEBUG
			DebugLog.WriteLine("Creating trigger " + type + " using " + action.Graph[0].GetType().Name);
#endif
			Condition = condition;
			Action = action.Unravel();
			Type = type;
		}

		public Trigger(Trigger<T, U> t) {
			// Only a shallow copy is necessary because Args and Action don't change and are lazily evaluated
			Condition = t.Condition;
			Action = t.Action;
			Type = t.Type;
		}

		public static Trigger<T, U> At(TriggerType type, ActionGraph g, Condition<T, U> condition = null) {
			return new Trigger<T, U>(type, g, condition);
		}
	}
}
