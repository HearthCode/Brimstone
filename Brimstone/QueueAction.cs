using System;
using System.Collections.Generic;
using System.Linq;

namespace Brimstone
{
	public abstract class QueueAction
	{
		public int SourceEntityId { get; set; }
		public List<ActionGraph> Args { get; } = new List<ActionGraph>();
		public abstract ActionResult Run(Game game, IEntity source, List<ActionResult> args);

		public ActionResult Execute(Game game, IEntity source, List<ActionResult> args) {

			Type me = GetType();
			if (game.ActiveTriggers.ContainsKey(me)) {
				var triggers = game.ActiveTriggers[me];

				foreach (var trigger in triggers)
					if (trigger.Epoch == TriggerEpoch.When) {
						// Get arguments that must match for the trigger to fire
						var matchArgs = new List<ActionResult>();
						foreach (var a in trigger.Args)
							matchArgs.Add(a.Run(game, game.Entities[trigger.EntityId], null));
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
							game.ActionQueue.Enqueue(game.Entities[trigger.EntityId], trigger.Action);
					}
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