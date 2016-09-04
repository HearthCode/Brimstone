using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Brimstone
{
	public class QueueActionEventArgs : EventArgs, ICloneable
	{
		public Game Game { get; set; }
		private int _sourceId;
		public IEntity Source {
			get { return Game.Entities[_sourceId]; }
			set { _sourceId = value.Id; }
		}
		public QueueAction Action { get; set; }
		public List<ActionResult> Args { get; set; }
		public bool Cancel { get; set; }
		public object UserData { get; set; }

		public QueueActionEventArgs(Game g, IEntity s, QueueAction a, List<ActionResult> p = null, object u = null) {
			Game = g;
			Source = s;
			Action = a;
			Args = p;
			UserData = u;
			Cancel = false;
		}

		public override string ToString() {
			string s = string.Format("Game {0:x8}: {1} ({2}) -> {3}", Game.FuzzyGameHash, Source.Card.Name, Source.Id, Action);
			if (Args != null && Args.Count > 0) {
				s += "(";
				foreach (var a in Args)
					s += a + ", ";
				s = s.Substring(0, s.Length - 2) + ")";
			}
			return s;
		}

		public object Clone() {
			// NOTE: Cancel flag is cleared when cloning
			var clone = (QueueActionEventArgs)MemberwiseClone();
			clone.Cancel = false;
			return clone;
		}
	}

	public class ActionQueue : ListTree<QueueActionEventArgs>, ICloneable
	{
		public Game Game { get; private set; }
		// TODO: Use depth queueing instead of a stack
		public Stack<Deque<QueueActionEventArgs>> QueueStack = new Stack<Deque<QueueActionEventArgs>>();
		public Deque<QueueActionEventArgs> Queue;
		public Stack<ActionResult> ResultStack = new Stack<ActionResult>();
		public IEnumerable<QueueActionEventArgs> History => this;

		public bool Paused { get; set; }

		public object UserData { get; set; }

		public event EventHandler<QueueActionEventArgs> OnQueueing;
		public event EventHandler<QueueActionEventArgs> OnQueued;
		public event EventHandler<QueueActionEventArgs> OnActionStarting;
		public event EventHandler<QueueActionEventArgs> OnAction;

		private readonly Dictionary<Type, Func<ActionQueue, QueueActionEventArgs, Task>> ReplacedActions;
		private readonly Stack<BlockStart> BlockStack;

		public int Count => Queue.Count;
		public bool IsBlockEmpty => Queue.Count == 0;
		public bool IsEmpty => Queue.Count == 0 && QueueStack.Count == 0;
		public int Depth => QueueStack.Count;

		public ActionQueue(Game game, object userData = null) : base(null) {
			Game = game;
			Paused = false;
			UserData = userData;
			Queue = new Deque<QueueActionEventArgs>();
			ReplacedActions = new Dictionary<Type, Func<ActionQueue, QueueActionEventArgs, Task>>();
			BlockStack = new Stack<BlockStart>();
		}

		public ActionQueue(ActionQueue cloneFrom) : base(cloneFrom) {
			// BlockStart is immutable and uses only value types so we can just shallow clone
			BlockStack = new Stack<BlockStart>(cloneFrom.BlockStack.Reverse());
			foreach (var queue in cloneFrom.QueueStack.Reverse())
				QueueStack.Push(new Deque<QueueActionEventArgs>(queue.Select(q => (QueueActionEventArgs) q.Clone())));
			Queue = new Deque<QueueActionEventArgs>(cloneFrom.Queue.Select(q => (QueueActionEventArgs) q.Clone()));
			var stack = new List<ActionResult>(cloneFrom.ResultStack);
			stack.Reverse();
			// TODO: Doesn't this clone some items twice?
			foreach (var item in stack)
				ResultStack.Push((ActionResult)item.Clone());
			// TODO: Option to disable History
			ReplacedActions = new Dictionary<Type, Func<ActionQueue, QueueActionEventArgs, Task>>(cloneFrom.ReplacedActions);
			Paused = cloneFrom.Paused;
			// Events are immutable so this creates copies
			OnQueueing = cloneFrom.OnQueueing;
			OnQueued = cloneFrom.OnQueued;
			OnActionStarting = cloneFrom.OnActionStarting;
			OnAction = cloneFrom.OnAction;
			// Copy user data
			UserData = cloneFrom.UserData;
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

		public void StartBlock(IEntity source, List<QueueAction> qa, BlockStart gameBlock = null) {
			if (qa == null)
				return;
#if _QUEUE_DEBUG
			DebugLog.WriteLine("Queue (Game " + Game.GameId + "): Spawning new queue at depth " + (Depth + 1) + " for " + source.ShortDescription + " with actions: " +
			                   string.Join(" ", qa.Select(a => a.ToString())) + " for action block: " + (gameBlock?.ToString() ?? "none"));
#endif
			QueueStack.Push(Queue);
			BlockStack.Push(gameBlock);
			Queue = new Deque<QueueActionEventArgs>();
			EnqueueDeferred(source, qa);
		}

		public void StartBlock(IEntity source, QueueAction a, BlockStart gameBlock = null) {
			if (a != null)
				StartBlock(source, new List<QueueAction> { a }, gameBlock);
		}

		public void StartBlock(IEntity source, ActionGraph g, BlockStart gameBlock = null) {
			if (g != null)
				StartBlock(source, g.Unravel(), gameBlock);
		}

		private bool EndBlock() {
			if (Depth > 0) {
#if _QUEUE_DEBUG
				DebugLog.WriteLine("Queue (Game " + Game.GameId + "): Destroying queue at depth " + Depth);
#endif
				var gameBlock = BlockStack.Pop();
				if (gameBlock != null)
					Game.OnBlockEmpty(gameBlock);
				Queue = QueueStack.Pop();
			}
			// When the queue is empty, notify the game - it may refill it with new actions
			if (IsEmpty)
				Game.OnQueueEmpty();
			return !IsEmpty;
		}

		// Gets a QueueAction that can put into the queue
		private QueueActionEventArgs initializeAction(IEntity source, QueueAction qa) {
			return new QueueActionEventArgs(Game, source, qa);
		}

		public List<ActionResult> RunMultiResult(IEntity source, List<QueueAction> qa) {
			if (qa == null)
				return new List<ActionResult>();
			StartBlock(source, qa);
			return ProcessBlock();
		}

		public List<ActionResult> RunMultiResult(IEntity source, ActionGraph g) {
			return g != null ? RunMultiResult(source, g.Unravel()) : new List<ActionResult>();
		}

		public List<ActionResult> RunMultiResult(IEntity source, QueueAction a) {
			return a != null ? RunMultiResult(source, new List<QueueAction> {a}) : new List<ActionResult>();
		}

		public ActionResult Run(IEntity source, List<QueueAction> qa) {
			if (qa == null)
				return ActionResult.None;
			StartBlock(source, qa);
			var result = ProcessBlock();
			return result.Count > 0 ? result[0] : ActionResult.None;
		}

		public ActionResult Run(IEntity source, ActionGraph g) {
			return g != null ? Run(source, g.Unravel()) : ActionResult.None;
		}

		public ActionResult Run(IEntity source, QueueAction a) {
			return a != null ? Run(source, new List<QueueAction> {a}) : ActionResult.None;
		}

		public void EnqueueDeferred(IEntity source, List<QueueAction> qa) {
			if (qa != null)
				foreach (var a in qa)
					EnqueueDeferred(source, a);
		}

		public void EnqueueDeferred(IEntity source, ActionGraph g) {
			// Don't queue unimplemented cards
			if (g != null)
				EnqueueDeferred(source, g.Unravel());
		}

		public void EnqueueDeferred(IEntity source, QueueAction a) {
			if (a == null)
				return;
#if _QUEUE_DEBUG
			DebugLog.WriteLine("Queue (Game " + Game.GameId + "): Queueing action " + a + " for " + source.ShortDescription + " at depth " + Depth);
#endif
			var e = initializeAction(source, a);
			if (OnQueueing != null) {
				OnQueueing(this, e);
				// TODO: Count the number of arguments the cancelled action would take and remove those too
				if (!e.Cancel)
					Queue.AddBack(e);
			}
			else
				Queue.AddBack(e);

			OnQueued?.Invoke(this, e);
		}

		public void EnqueueDeferred(Action a) {
			Paused = true;
			a();
			Paused = false;
		}

		public List<ActionResult> ProcessBlock(object UserData = null) {
#if _QUEUE_DEBUG
			DebugLog.WriteLine("Queue (Game " + Game.GameId + "): Start processing current block");
			var depth = Depth;
#endif
			var result = ProcessAll(UserData, Depth);
#if _QUEUE_DEBUG
			System.Diagnostics.Debug.Assert(depth == Depth + 1);
			DebugLog.WriteLine("Queue (Game " + Game.GameId + "): End processing current block");
#endif
			return result;
		}

		public List<ActionResult> ProcessAll(object UserData = null, int MaxUnwindDepth = 0) {
			return ProcessAllAsync(UserData, MaxUnwindDepth).Result;
		}

		public async Task<List<ActionResult>> ProcessAllAsync(object UserData = null, int MaxUnwindDepth = 0) {
			while (await ProcessOneAsync(UserData, MaxUnwindDepth))
				;
			// Return whatever is left on the stack
			var stack = new List<ActionResult>(ResultStack);
			if (Paused || !IsEmpty)
				return stack;
			ResultStack.Clear();
			stack.Reverse();
			return stack;
		}

		public bool ProcessOne(object UserData = null, int MaxUnwindDepth = 0) {
			return ProcessOneAsync(UserData, MaxUnwindDepth).Result;
		}

		public async Task<bool> ProcessOneAsync(object UserData = null, int MaxUnwindDepth = 0) {
			if (Paused)
				return false;

			// Unwind queue when blocks are empty
			while (IsBlockEmpty && Depth > MaxUnwindDepth && EndBlock())
				;

			// Exit if we have unwound the entire queue or reached the unwind limit
			if (IsBlockEmpty)
				return false;

			// Get next action and make sure it's up to date if cloned from another game
			var action = Queue.RemoveFront();
			action.Game = Game;
			if (action.Source.Game.GameId != Game.GameId)
				action.Source = Game.Entities[action.Source.Id];

			// TODO: Make it work when not all of the arguments are supplied, for flexible syntax

			// Get arguments for action from stack
			action.Args = new List<ActionResult>();
			for (int i = 0; i < action.Action.Args.Count; i++)
				action.Args.Add(ResultStack.Pop());
			action.Args.Reverse();

			// Replace current UserData with new UserData if supplied
			if (UserData != null)
				this.UserData = UserData;
			action.UserData = this.UserData;

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
			AddItem(action);

			// Run action and push results onto stack
#if _QUEUE_DEBUG
			DebugLog.WriteLine("Queue (Game " + Game.GameId + "): Running action " + action + " for " + action.Source.ShortDescription + " at depth " + Depth);
#endif
			var result = action.Action.Execute(action.Game, action.Source, action.Args);
			if (result.HasResult)
				ResultStack.Push(result);

			// The >= allows the current block to unwind for ProcessBlock()
			while (IsBlockEmpty && Depth >= MaxUnwindDepth && EndBlock())
				;

			OnAction?.Invoke(this, action);

			return !action.Cancel;
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
			s += "Current block:\n";
			foreach (var a in Queue)
				s += a + "\n";

			foreach (var b in QueueStack) {
				s += "\nStacked:\n";
				foreach (var a in b)
					s += a + "\n";
			}
			return s;
		}

		public object Clone() {
			return new ActionQueue(this);
		}
	}
}
