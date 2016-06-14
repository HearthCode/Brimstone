using System.Collections.Generic;

namespace Brimstone
{
	public class ActionGraph
	{
		private List<QueueAction> graph = new List<QueueAction>();

		public ActionGraph(QueueAction q) {
			graph.Add(q);
		}

		// Convert single QueueAction to ActionGraph
		public static implicit operator ActionGraph(QueueAction q) {
			return new ActionGraph(q);
		}

		public ActionGraph Then(ActionGraph act) {
			graph.AddRange(act.graph);
			return this;
		}

		// Convert values to actions
		public static implicit operator ActionGraph(int x) {
			return new LazyNumber { Num = x };
		}
		public static implicit operator ActionGraph(Card x) {
			return new LazyCard { Card = x };
		}
		public static implicit operator ActionGraph(Entity x) {
			return new LazyEntity { Entity = x };
		}

		// Add the graph to the game's action queue
		public void Queue(ActionQueue queue) {
			foreach (var action in graph) {
				foreach (var arg in action.Args)
					arg.Queue(queue);
				queue.Queue.Enqueue(action);
			}
		}
	}
}