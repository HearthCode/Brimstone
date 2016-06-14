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

		// Convert ints to actions
		public static implicit operator ActionGraph(int x) {
			return new FixedNumber { Num = x };
		}

		// Add the graph to the game's action queue
		public void Queue(Game game) {
			foreach (var action in graph) {
				foreach (var arg in action.Args)
					arg.Queue(game);
				game.ActionQueue.Enqueue(action);
				action.Game = game;
			}
		}
	}
}