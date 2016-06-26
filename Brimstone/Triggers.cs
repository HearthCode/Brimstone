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

		public Trigger(Trigger t) {
			// Only a shallow copy is necessary because Args and Action don't change and are lazily evaluated
			TriggerActionType = t.TriggerActionType;
			Args = t.Args;
			Action = t.Action;
			Epoch = t.Epoch;
		}

		public AttachedTrigger CreateAttachedTrigger(IEntity entity) {
			// The base Triggers property on Card.Behaviour has no linked entity
			// Link by creating a copy of the trigger and setting EntityId
			return new AttachedTrigger(this, entity);
		}

		public static Trigger When(ActionGraph trigger, ActionGraph g) {
			return new Trigger(trigger, g, TriggerEpoch.When);
		}

		public static Trigger After(ActionGraph trigger, ActionGraph g) {
			return new Trigger(trigger, g, TriggerEpoch.After);
		}
	}

	public class AttachedTrigger : Trigger
	{
		public int EntityId { get; }

		public AttachedTrigger(Trigger t, IEntity e) : base(t) {
			EntityId = e.Id;
		}

		public AttachedTrigger(AttachedTrigger t) : base(t) {
			EntityId = t.EntityId;
		}
	}

	public class TriggerManager : ICloneable
	{
		public Game Game { get; set; }
		public Dictionary<Type, List<AttachedTrigger>> Triggers { get; }

		public TriggerManager(Game game) {
			Game = game;
			Triggers = new Dictionary<Type, List<AttachedTrigger>>();
		}

		public TriggerManager(TriggerManager tm) {
			Game = tm.Game;
			Triggers = new Dictionary<Type, List<AttachedTrigger>>(tm.Triggers);
		}

		public void Add(IEntity entity) {
			if (entity.Card.Behaviour != null)
				if (entity.Card.Behaviour.Triggers != null)
					foreach (var t in entity.Card.Behaviour.Triggers)
						Add(t.CreateAttachedTrigger(entity));
		}

		public void Add(AttachedTrigger t) {
			if (Triggers.ContainsKey(t.TriggerActionType))
				Triggers[t.TriggerActionType].Add(t);
			else
				Triggers.Add(t.TriggerActionType, new List<AttachedTrigger> { t });
		}

		public void Fire(TriggerEpoch epoch, QueueAction action, IEntity source, List<ActionResult> args) {
			Type me = action.GetType();
			if (!Triggers.ContainsKey(me))
				return;

			foreach (var trigger in Triggers[me])
				if (trigger.Epoch == epoch) {
					// Get arguments that must match for the trigger to fire
					var matchArgs = new List<ActionResult>();
					foreach (var a in trigger.Args)
						matchArgs.Add(a.Run(Game, Game.Entities[trigger.EntityId], null));
					//var matchArgs = game.ActionQueue.EnqueueMultiResult(game.Entities[trigger.EntityId], trigger.Args);

					bool match = true;
					for (int i = 0; i < matchArgs.Count; i++) {
						// Always match unspecified arguments
						if (matchArgs[i].IsBlank)
							continue;
						// Match if value type equality is met
						else if (matchArgs[i] == args[i])
							continue;
						// Check if it's a list of entities
						List<IEntity> matchEntities = matchArgs[i];
						if (matchEntities == null) {
							// If it's not then equality failed and there is no match
							match = false;
							break;
						}
						// One if the items in the trigger list must be in the actual entity list to match
						match = matchArgs[i].Intersect((List<IEntity>)args[i]).Any();
						if (!match)
							break;
					}
					if (match)
						Game.ActionQueue.EnqueuePaused(Game.Entities[trigger.EntityId], trigger.Action);
				}
		}

		public void When(ActionGraph trigger, ActionGraph g) {
			Add(new Trigger(trigger, g, TriggerEpoch.When).CreateAttachedTrigger(Game));
		}

		public void After(ActionGraph trigger, ActionGraph g) {
			Add(new Trigger(trigger, g, TriggerEpoch.After).CreateAttachedTrigger(Game));
		}

		public object Clone() {
			return new TriggerManager(this);
		}
	}
}
