using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Brimstone
{
	public class QueueActionEventArgs : EventArgs, ICloneable
	{
		public Game Game { get; set; }
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

		public override string ToString() {
			string s = string.Format("Game {0:x8}: {1} ({2}) -> {3}", Game.Entities.FuzzyGameHash, Source.Card.Name, Source.Id, Action);
			if (Args.Count > 0) {
				s += "(";
				foreach (var a in Args)
					s += a + ", ";
				s = s.Substring(0, s.Length - 2) + ")";
			}
			return s;
		}

		public object Clone() {
			// NOTE: Cancel flag is cleared when cloning
			return new QueueActionEventArgs(Game, Source, Action, Args);
		}
	}

	public class ActionQueue : ICloneable
	{
		public Game Game { get; private set; }
		public Deque<QueueActionEventArgs> Queue = new Deque<QueueActionEventArgs>();
		public Stack<ActionResult> ResultStack = new Stack<ActionResult>();
		public List<QueueActionEventArgs> History;
		public bool Paused { get; set; }

		public event EventHandler<QueueActionEventArgs> OnQueueing;
		public event EventHandler<QueueActionEventArgs> OnQueued;
		public event EventHandler<QueueActionEventArgs> OnActionStarting;
		public event EventHandler<QueueActionEventArgs> OnAction;

		private Dictionary<Type, Func<ActionQueue, QueueActionEventArgs, Task>> ReplacedActions;

		public int Count { get { return Queue.Count; } }

		public ActionQueue(Game game) {
			Game = game;
			Paused = false;
			History = new List<QueueActionEventArgs>();
			ReplacedActions = new Dictionary<Type, Func<ActionQueue, QueueActionEventArgs, Task>>();
		}

		public ActionQueue(ActionQueue cloneFrom) {
			foreach (var item in cloneFrom.Queue)
				Queue.AddBack((QueueActionEventArgs)item.Clone());
			var stack = new List<ActionResult>(cloneFrom.ResultStack);
			stack.Reverse();
			// TODO: Doesn't this clone some items twice?
			foreach (var item in stack)
				ResultStack.Push((ActionResult)item.Clone());
			History = new List<QueueActionEventArgs>(cloneFrom.History);
			ReplacedActions = new Dictionary<Type, Func<ActionQueue, QueueActionEventArgs, Task>>(cloneFrom.ReplacedActions);
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

		// Gets a QueueAction that can put into the queue
		private QueueActionEventArgs initializeAction(IEntity source, QueueAction qa) {
			return new QueueActionEventArgs(Game, source, qa);
		}

		public void InsertDeferred(IEntity source, List<QueueAction> qa) {
			if (qa != null) {
				foreach (var a in qa) {
					Queue.AddFront(initializeAction(source, a));
				}
			}
		}

		public void InsertDeferred(IEntity source, ActionGraph g) {
			InsertDeferred(source, g.Unravel());
		}

		public void InsertDeferred(IEntity source, QueueAction a) {
			if (a == null)
				return;
			// No event triggers when inserting at front of queue
			Queue.AddFront(initializeAction(source, a));
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

			var e = initializeAction(source, a);
			if (OnQueueing != null) {
				OnQueueing(this, e);
				// TODO: Count the number of arguments the cancelled action would take and remove those too
				if (!e.Cancel)
					Queue.AddBack(e);
			}
			else
				Queue.AddBack(e);

			if (OnQueued != null)
				OnQueued(this, e);
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
				ResultStack.Push(a);
		}

		public List<ActionResult> ProcessAll() {
			return ProcessAllAsync().Result;
		}

		public async Task<List<ActionResult>> ProcessAllAsync() {
			while (await ProcessOne())
				;
			// Return whatever is left on the stack
			var stack = new List<ActionResult>(ResultStack);
			if (Paused || Queue.Count != 0)
				return stack;
			ResultStack.Clear();
			stack.Reverse();
			return stack;
		}

		public async Task<bool> ProcessOne() {
			if (Paused)
				return false;

			if (Queue.Count == 0)
				return false;

			// Get next action and make sure it's up to date if cloned from another game
			var action = Queue.RemoveFront();
			action.Game = Game;
			if (action.Source.Game.GameId != Game.GameId)
				action.Source = Game.Entities[action.Source.Id];

			// TODO: Fix stack modifying on OnActionStarting

			// Get arguments for action from stack
			action.Args = new List<ActionResult>();
			for (int i = 0; i < action.Action.Args.Count; i++)
				action.Args.Add(ResultStack.Pop());
			action.Args.Reverse();

			if (OnActionStarting != null) {
				OnActionStarting(this, action);
				if (action.Cancel)
					return false;
			}
			var actionType = action.Action.GetType();
			if (ReplacedActions.ContainsKey(actionType)) {
				await ReplacedActions[actionType](this, action);
				// action.Cancel implied when action is replaced
				return false;
			}
			History.Add(action);

			// Run action and push results onto stack
			var result = action.Action.Execute(action.Game, action.Source, action.Args);
			if (result.HasResult)
				ResultStack.Push(result);

			if (OnAction != null) {
				OnAction(this, action);
				if (action.Cancel)
					return false;
			}
			return true;
		}

		public void ReplaceAction<QAT>(Func<ActionQueue, QueueActionEventArgs, Task> evt) {
			if (!ReplacedActions.ContainsKey(typeof(QAT)))
				ReplacedActions.Add(typeof(QAT), evt);
			else
				ReplacedActions[typeof(QAT)] = evt;
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
