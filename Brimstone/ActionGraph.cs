using System;
using System.Collections.Generic;

namespace Brimstone
{
	public class ActionGraph
	{
		public List<QueueAction> Graph { get; private set; }

		public ActionGraph(QueueAction q) {
			Graph = new List<QueueAction>() { q };
		}

		public ActionGraph(ActionGraph g) {
			Graph = new List<QueueAction>(g.Graph);
		}

		// Convert single QueueAction to ActionGraph
		public static implicit operator ActionGraph(QueueAction q) {
			return new ActionGraph(q);
		}

		public ActionGraph Then(ActionGraph act) {
			Graph.AddRange(act.Graph);
			return this;
		}

		public ActionGraph Repeat(ActionGraph qty) {
			var repeatAction = new Repeat { Actions = new ActionGraph(this), Args = { qty } };
			Graph = new List<QueueAction>() { repeatAction };
			return this;
		}

		// Convert values to actions
		public static implicit operator ActionGraph(int x) {
			return new FixedNumber { Num = x };
		}
		public static implicit operator ActionGraph(Card x) {
			return new FixedCard { Card = x };
		}
		public static implicit operator ActionGraph(string x) {
			return new FixedCard { Card = x };
		}
		public static implicit operator ActionGraph(Entity x) {
			return new LazyEntity { EntityId = x.Id };
		}
		public static implicit operator ActionGraph(Action<IEntity> x) {
			return new Func { F = x };
		}
		public static implicit operator ActionGraph(List<IEntity> e) {
			return new Selector { Lambda = x => e };
		}

		// Unravel graph implicitly
		public static implicit operator List<QueueAction>(ActionGraph g) {
			return g.Unravel();
		}

		// Repeated action
		public static ActionGraph operator *(ActionGraph x, ActionGraph y) {
			return x.Repeat(y);
		}

		// Add the graph to the game's action queue
		public void Queue(IEntity source, ActionQueue queue) {
			foreach (var action in Graph) {
				foreach (var arg in action.Args)
					arg.Queue(source, queue);
				queue.EnqueueDeferred(source, action);
			}
		}

		public List<QueueAction> Unravel(ActionGraph g = null) {
			if (g == null)
				g = this;
			var ql = new List<QueueAction>();
			foreach (var action in g.Graph) {
				foreach (var arg in action.Args)
					if (arg != null)
						ql.AddRange(Unravel(arg));
					else
						ql.Add(new Empty());
				ql.Add(action);
			}
			return ql;
		}
	}

	public static class QueueActionListExtensions
	{
		public static List<QueueAction> Then(this List<QueueAction> ql, ActionGraph act) {
			ql.AddRange(act.Unravel());
			return ql;
		}
	}
}
