using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Brimstone
{
	public class QueueActionEventArgs : EventArgs
	{
		public Game Game;
		public QueueAction Action;

		public QueueActionEventArgs(Game g, QueueAction a) {
			Game = g;
			Action = a;
		}
	}

	public class ActionQueue
	{
		public Game Game { get; private set; }
		public Queue<QueueAction> Queue = new Queue<QueueAction>();
		public Stack<ActionResult> ResultStack = new Stack<ActionResult>();
		public bool Paused { get; set; }

		public event EventHandler<QueueActionEventArgs> OnQueueing;
		public event EventHandler<QueueActionEventArgs> OnQueued;
		public event EventHandler<QueueActionEventArgs> OnActionStarting;
		public event EventHandler<QueueActionEventArgs> OnAction;

		public void Attach(Game game) {
			Game = game;
		}

		public void Enqueue(ActionGraph g) {
			// Don't queue unimplemented cards
			if (g != null)
				// Unravel the graph into a list of actions
				g.Queue(this);
		}

		public void EnqueueSingleAction(QueueAction a) {
			if (OnQueueing != null)
				OnQueueing(this, new QueueActionEventArgs(Game, a));

			Queue.Enqueue(a);

			if (OnQueued != null)
				OnQueued(this, new QueueActionEventArgs(Game, a));
		}

		public void ReplaceArg(ActionResult newArg) {
			ReplaceArgs(new List<ActionResult> { newArg });
		}

		public void ReplaceArgs(List<ActionResult> newArgs) {
			for (int i = 0; i < newArgs.Count; i++)
				ResultStack.Pop();
			foreach (var a in newArgs)
				ResultStack.Push(a); ;
		}

		public void ReplaceAction(QueueAction a) {
			Queue.Dequeue();
			Enqueue(a);
		}

		public List<ActionResult> Process() {
			if (Paused)
				return null;

			while (Queue.Count > 0) {
				var action = Queue.Dequeue();
				Console.WriteLine(action);
				if (OnActionStarting != null)
					OnActionStarting(this, new QueueActionEventArgs(Game, action));
				// TODO: Replace with async/await later
				if (Paused)
					return null;
				var args = new List<ActionResult>();
				for (int i = 0; i < action.Args.Count; i++)
					args.Add(ResultStack.Pop());
				args.Reverse();
				var result = action.Run(Game, args);
				if (result.HasResult)
					ResultStack.Push(result);
				if (OnAction != null)
					OnAction(this, new QueueActionEventArgs(Game, action));
			}
			// Return whatever is left on the stack
			var stack = new List<ActionResult>(ResultStack);
			ResultStack.Clear();
			return stack;
		}

		public string StackToString() {
			string s = string.Empty;
			foreach (var r in ResultStack)
				s += r + "\n";
			return s;
		}

		public override string ToString() {
			string s = string.Empty;
			foreach (var a in Queue)
				s += a + "\n";
			return s;
		}
	}
}
