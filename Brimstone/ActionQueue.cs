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
		public IEntity Source;
		public QueueAction Action;

		public QueueActionEventArgs(Game g, IEntity s, QueueAction a) {
			Game = g;
			Source = s;
			Action = a;
		}
	}

	public class ActionQueue
	{
		public Game Game { get; private set; }
		public Queue<QueueAction> Queue = new Queue<QueueAction>();
		public Stack<ActionResult> ResultStack = new Stack<ActionResult>();

		public event EventHandler<QueueActionEventArgs> OnQueueing;
		public event EventHandler<QueueActionEventArgs> OnQueued;
		public event EventHandler<QueueActionEventArgs> OnActionStarting;
		public event EventHandler<QueueActionEventArgs> OnAction;

		public void Attach(Game game) {
			Game = game;
		}

		public void EnqueuePaused(IEntity source, List<QueueAction> qa) {
			if (qa != null)
				foreach (var a in qa)
					EnqueuePaused(source, a);
		}

		public void EnqueuePaused(IEntity source, ActionGraph g) {
			// Don't queue unimplemented cards
			if (g != null)
				// Unravel the graph into a list of actions
				g.Queue(source, this);
		}

		public List<ActionResult> Enqueue(IEntity source, List<QueueAction> qa) {
			EnqueuePaused(source, qa);
			return Process();
		}

		public List<ActionResult> Enqueue(IEntity source, ActionGraph g) {
			EnqueuePaused(source, g);
			return Process();
		}

		public ActionResult EnqueueSingleResult(IEntity source, List<QueueAction> qa) {
			EnqueuePaused(source, qa);
			return Process()[0];
		}

		public ActionResult EnqueueSingleResult(IEntity source, ActionGraph g) {
			EnqueuePaused(source, g);
			return Process()[0];
		}

		public List<ActionResult> Enqueue(IEntity source, QueueAction a) {
			EnqueuePaused(source, a);
			return Process();
		}

		public ActionResult EnqueueSingleResult(IEntity source, QueueAction a) {
			EnqueuePaused(source, a);
			return Process()[0];
		}

		public void EnqueuePaused(IEntity source, QueueAction a) {
			if (a == null)
				return;

			a.SourceEntityId = source.Id;

			if (OnQueueing != null)
				OnQueueing(this, new QueueActionEventArgs(Game, source, a));

			Queue.Enqueue(a);

			if (OnQueued != null)
				OnQueued(this, new QueueActionEventArgs(Game, source, a));
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

		public void ReplaceNextAction(QueueAction a) {
			var previousAction = Queue.Dequeue();
			// TODO: This really needs to be inserted at the start of the queue
			EnqueuePaused(Game.Entities[previousAction.SourceEntityId], (ActionGraph)a);
		}

		public List<ActionResult> Process() {
			while (Queue.Count > 0) {
				// Get next action
				var action = Queue.Dequeue();
				var source = Game.Entities[action.SourceEntityId];

				if (OnActionStarting != null)
					OnActionStarting(this, new QueueActionEventArgs(Game, source, action));

				// Get arguments for action from stack
				var args = new List<ActionResult>();
				for (int i = 0; i < action.Args.Count; i++)
					args.Add(ResultStack.Pop());
				args.Reverse();

				// TODO: Replace with async/await later
				// Run action and push results onto stack
				var result = action.Run(Game, source, args);
				if (result.HasResult)
					ResultStack.Push(result);
				if (OnAction != null)
					OnAction(this, new QueueActionEventArgs(Game, source, action));
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
