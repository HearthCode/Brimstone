using System.Collections.Generic;

namespace Brimstone
{
	public class CompiledBehaviour
	{
		public List<QueueAction> Battlecry;
		public List<QueueAction> Deathrattle;

		// Compile all the ActionGraph fields in a Behaviour into lists of QueueActions
		public static CompiledBehaviour Compile(Behaviour b) {
			var compiled = new CompiledBehaviour();
			var behaviourList = new List<string>();

			// Find all the fields we can compile
			var behaviourClass = typeof(Behaviour).GetFields();
			foreach (var field in behaviourClass)
				behaviourList.Add(field.Name);

			// Compile each field that exists
			foreach (var fieldName in behaviourList) {
				var field = b.GetType().GetField(fieldName);
				ActionGraph fieldValue = field.GetValue(b) as ActionGraph;
				if (fieldValue != null) {
					compiled.GetType().GetField(fieldName).SetValue(compiled, Unravel(fieldValue));
				} else
					compiled.GetType().GetField(fieldName).SetValue(compiled, new List<QueueAction>());
			}
			return compiled;
		}

		private static List<QueueAction> Unravel(ActionGraph g) {
			var ql = new List<QueueAction>();
			foreach (var action in g.Graph) {
				foreach (var arg in action.Args)
					ql.AddRange(Unravel(arg));
				ql.Add(action);
			}
			return ql;
		}
	}
}
