using System.Collections.Generic;

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

		public override string ToString() {
			return "[ACTION: " + GetType().Name + ", SOURCE: " + SourceEntityId + "]";
		}
	}
}