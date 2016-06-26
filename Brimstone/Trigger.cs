using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Brimstone
{
	public enum TriggerEpoch
	{
		When,
		After
	}

	public class Trigger
	{
		public int EntityId { get; set; }
		public Type TriggerActionType { get; }
		public List<QueueAction> Args { get; } = new List<QueueAction>();
		public List<QueueAction> Action { get; }= new List<QueueAction>();
		public TriggerEpoch Epoch { get; }

		public Trigger(ActionGraph triggerSource, ActionGraph action, TriggerEpoch when) {
			var actionList = triggerSource.Unravel();
			TriggerActionType = actionList[actionList.Count - 1].GetType();
			actionList.RemoveAt(actionList.Count - 1);
			Args = actionList;
			Action = action.Unravel();
			Epoch = when;
		}

		public Trigger(Trigger t) {
			// Only a shallow copy is necessary because Args and Action don't change and are lazily evaluated
			TriggerActionType = t.TriggerActionType;
			Args = t.Args;
			Action = t.Action;
			Epoch = t.Epoch;
			EntityId = t.EntityId;
		}

		public Trigger CreateAttachedTrigger(int entityId) {
			// The base Triggers property on Card.Behaviour has no linked entity
			// Link by creating a copy of the trigger and setting EntityId
			var attached = new Trigger(this);
			attached.EntityId = entityId;
			return attached;
		}

		public static Trigger When(ActionGraph trigger, ActionGraph g) {
			return new Trigger(trigger, g, TriggerEpoch.When);
		}

		public static Trigger After(ActionGraph trigger, ActionGraph g) {
			return new Trigger(trigger, g, TriggerEpoch.After);
		}
	}
}
