using System;
using System.Collections.Generic;
using System.Linq;

namespace Brimstone
{
	public abstract class QueueAction
	{
		// TODO: Implement event triggers
		// Each event must have 'On' and 'After' triggers
		// Each event must have a list of associated lazily evaluated entities (a lambda selector) which is compared with
		// any entities affected by the event. In the event of a match, the trigger is executed, eg (psuedo-code):
		// OnDamage(OpponentMinions, Draw * 2)   or:
		// OpponentMinions.OnDamage(Draw * 2)   or:
		// When(Damage(OpponentMinions), Draw * 2)   or:
		// Damage(OpponentMinions).On(Draw * 2)
		// etc. Any of these syntaxes will do. Only one needs to work.

		public int SourceEntityId { get; set; }
		public List<ActionGraph> Args { get; } = new List<ActionGraph>();
		public abstract ActionResult Run(Game game, IEntity source, List<ActionResult> args);

		public ActionResult Execute(Game game, IEntity source, List<ActionResult> args) {
			Type me = GetType();
			// TODO: Track active triggers instead of iterating because this is dog slow
			// (make Game use a dictionary of lists indexed by TriggerActionType)
			foreach (var e in game.Entities)
				if (e.Card.Behaviour != null)
					if (e.Card.Behaviour.Triggers != null)
						foreach (var trigger in e.Card.Behaviour.Triggers)
							if (trigger.TriggerActionType == me && trigger.Epoch == TriggerEpoch.When) {
								// Get arguments that must match for the trigger to fire
								var matchArgs = game.ActionQueue.EnqueueMultiResult(e, trigger.Args);

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
									game.ActionQueue.Enqueue(e, trigger.Action);
							}

			var result = Run(game, source, args);

			// TODO: After triggers

			return result;
		}

		public override string ToString() {
			return "[ACTION: " + GetType().Name + ", SOURCE: " + SourceEntityId + "]";
		}
	}
}