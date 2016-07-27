using System;
using System.Collections.Generic;
using System.Linq;

namespace Brimstone
{
	public class QueueActionEventArgs : EventArgs
	{
		public Game Game { get; }
		public IEntity Source { get; set; }
		public QueueAction Action { get; set; }
		public List<ActionResult> Args { get; set; }
		public bool Cancel { get; set; }

		public QueueActionEventArgs(Game g, IEntity s, QueueAction a, List<ActionResult> p = null) {
			Game = g;
			Source = s;
			Action = a;
			Args = p;
			Cancel = false;
		}
	}

	public class ActionQueue : ICloneable
	{
		public Game Game { get; private set; }
		public Deque<QueueAction> Queue = new Deque<QueueAction>();
		public Stack<ActionResult> ResultStack = new Stack<ActionResult>();
		public bool Paused { get; set; }

		public event EventHandler<QueueActionEventArgs> OnQueueing;
		public event EventHandler<QueueActionEventArgs> OnQueued;
		public event EventHandler<QueueActionEventArgs> OnActionStarting;
		public event EventHandler<QueueActionEventArgs> OnAction;

		public int Count { get { return Queue.Count; } }

		public ActionQueue(Game game) {
			Game = game;
			Paused = false;
		}

		public ActionQueue(ActionQueue cloneFrom) {
			foreach (var item in cloneFrom.Queue)
				Queue.AddBack((QueueAction)item.Clone());
			var stack = new List<ActionResult>(cloneFrom.ResultStack);
			stack.Reverse();
			foreach (var item in stack)
				ResultStack.Push((ActionResult)item.Clone());
			Paused = cloneFrom.Paused;
			// Events are immutable so this creates copies
			OnQueueing = cloneFrom.OnQueueing;
			OnQueued = cloneFrom.OnQueued;
			OnActionStarting = cloneFrom.OnActionStarting;
			OnAction = cloneFrom.OnAction;
		}

		public void Attach(Game game) {
			Game = game;

			// Make action stack entities point to new game
			var stack = new List<ActionResult>(ResultStack);
			stack.Reverse();
			ResultStack.Clear();
			foreach (var ar in stack) {
				List<IEntity> el = ar;
				if (el == null) {
					ResultStack.Push(ar);
					continue;
				}
				List<IEntity> nel = new List<IEntity>();
				foreach (var item in el)
					nel.Add(game.Entities[item.Id]);
				ResultStack.Push(nel);
			}
		}


		public void InsertDeferred(IEntity source, List<QueueAction> qa) {
			if (qa != null) {
				foreach (var a in qa)
					a.SourceEntityId = source.Id;
				Queue.AddFrontRange(qa);
			}
		}

		public void InsertDeferred(IEntity source, ActionGraph g) {
			InsertDeferred(source, g.Unravel());
		}

		public void InsertDeferred(IEntity source, QueueAction a) {
			if (a == null)
				return;
			// No event triggers when inserting at front of queue
			a.SourceEntityId = source.Id;
			Queue.AddFront(a);
		}

		public List<ActionResult> EnqueueMultiResult(IEntity source, List<QueueAction> qa) {
			EnqueueDeferred(source, qa);
			return ProcessAll();
		}

		public List<ActionResult> EnqueueMultiResult(IEntity source, ActionGraph g) {
			EnqueueDeferred(source, g);
			return ProcessAll();
		}

		public List<ActionResult> EnqueueMultiResult(IEntity source, QueueAction a) {
			EnqueueDeferred(source, a);
			return ProcessAll();
		}

		public ActionResult Enqueue(IEntity source, List<QueueAction> qa) {
			EnqueueDeferred(source, qa);
			var result = ProcessAll();
			if (result.Count > 0)
				return result[0];
			return ActionResult.None;
		}

		public ActionResult Enqueue(IEntity source, ActionGraph g) {
			EnqueueDeferred(source, g);
			var result = ProcessAll();
			if (result.Count > 0)
				return result[0];
			return ActionResult.None;
		}

		public ActionResult Enqueue(IEntity source, QueueAction a) {
			EnqueueDeferred(source, a);
			var result = ProcessAll();
			if (result.Count > 0)
				return result[0];
			return ActionResult.None;
		}

		public void EnqueueDeferred(IEntity source, List<QueueAction> qa) {
			if (qa != null)
				foreach (var a in qa)
					EnqueueDeferred(source, a);
		}

		public void EnqueueDeferred(IEntity source, ActionGraph g) {
			// Don't queue unimplemented cards
			if (g != null)
				// Unravel the graph into a list of actions
				g.Queue(source, this);
		}

		public void EnqueueDeferred(IEntity source, QueueAction a) {
			if (a == null)
				return;

			a.SourceEntityId = source.Id;

			if (OnQueueing != null) {
				var e = new QueueActionEventArgs(Game, source, a);
				OnQueueing(this, e);
				// TODO: Count the number of arguments the cancelled action would take and remove those too
				if (!e.Cancel)
					Queue.AddBack(a);
			}
			else
				Queue.AddBack(a);

			if (OnQueued != null)
				OnQueued(this, new QueueActionEventArgs(Game, source, a));
		}

		public void EnqueueDeferred(Action a) {
			Paused = true;
			a();
			Paused = false;
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

		public List<ActionResult> ProcessAll() {
			while (ProcessOne())
				;
			// Return whatever is left on the stack
			var stack = new List<ActionResult>(ResultStack);
			if (Paused || Queue.Count != 0)
				return stack;
			ResultStack.Clear();
			stack.Reverse();
			return stack;
		}

		public bool ProcessOne() {
			if (Paused)
				return false;

			if (Queue.Count == 0)
				return false;

			// Get next action
			var action = Queue.RemoveFront();
			var source = Game.Entities[action.SourceEntityId];

			// TODO: Fix stack modifying on OnActionStarting

			// Get arguments for action from stack
			var args = new List<ActionResult>();
			for (int i = 0; i < action.Args.Count; i++)
				args.Add(ResultStack.Pop());
			args.Reverse();

			if (OnActionStarting != null) {
				var e = new QueueActionEventArgs(Game, source, action, args);
				OnActionStarting(this, e);
				source = e.Source;
				action = e.Action;
				args = e.Args;
				if (e.Cancel)
					return false;
			}

			// TODO: Replace with async/await later
			// Run action and push results onto stack
			var result = action.Execute(Game, source, args);
			if (result.HasResult)
				ResultStack.Push(result);

			if (OnAction != null) {
				var e = new QueueActionEventArgs(Game, source, action, args);
				OnAction(this, e);
				if (e.Cancel)
					return false;
			}
			return true;
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

		public object Clone() {
			return new ActionQueue(this);
		}
	}
}
