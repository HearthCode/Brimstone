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

		public static Trigger When(ActionGraph trigger, ActionGraph g) {
			return new Trigger(trigger, g, TriggerEpoch.When);
		}

		public static Trigger After(ActionGraph trigger, ActionGraph g) {
			return new Trigger(trigger, g, TriggerEpoch.After);
		}
	}
}
