#define _USE_QUEUE
//#define _USE_TREE

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Brimstone.Actions;

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
		public ActionResult[] Args { get; set; }
		public bool Cancel { get; set; }
		public object UserData { get; set; }

		public QueueActionEventArgs(Game g, IEntity s, QueueAction a, ActionResult[] p = null, object u = null) {
			Game = g;
			Source = s;
			Action = a;
			Args = p;
			UserData = u;
			Cancel = false;
		}

		public override string ToString() {
			string s = string.Format("Game {0:x8}: {1} ({2}) -> {3}", Game.FuzzyGameHash, Source.Card.Name, Source.Id, Action);
			if (Args != null && Args.Any()) {
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
#if _USE_QUEUE
		public Stack<Deque<QueueActionEventArgs>> QueueStack = new Stack<Deque<QueueActionEventArgs>>();
		public Deque<QueueActionEventArgs> Queue;
		private readonly Stack<BlockStart> BlockStack;
#endif
#if _USE_TREE
		public QueueTree Tree { get; }
#endif
		public ImmutableStack<ActionResult> ResultStack;
		public IEnumerable<QueueActionEventArgs> History => this;

		public bool Paused { get; set; }
		public bool LastActionCancelled { get; private set; }
		public bool HasHistory { get; }

		public object UserData { get; set; }

		public event EventHandler<QueueActionEventArgs> OnQueueing;
		public event EventHandler<QueueActionEventArgs> OnQueued;
		public event EventHandler<QueueActionEventArgs> OnActionStarting;
		public event EventHandler<QueueActionEventArgs> OnAction;

		private ImmutableDictionary<Type, Func<ActionQueue, QueueActionEventArgs, Task>> ReplacedActions;
#if _USE_TREE
		public bool IsBlockEmpty => Tree.IsBranchEmpty;
		public bool IsEmpty => Tree.IsEmpty;
		public int Depth => Tree.Depth;
#endif
#if _USE_QUEUE
		public bool IsBlockEmpty => Queue.Count == 0;
		public bool IsEmpty => Queue.Count == 0 && Depth == 0;
		public int Depth => QueueStack.Count;
#endif

		public ActionQueue(Game game, bool actionHistory, object userData = null) : base(null) {
			Game = game;
			Paused = false;
			HasHistory = actionHistory;
			UserData = userData;
#if _USE_QUEUE
			Queue = new Deque<QueueActionEventArgs>();
			BlockStack = new Stack<BlockStart>();
#endif
			ResultStack = ImmutableStack.Create<ActionResult>();
			ReplacedActions = ImmutableDictionary.Create<Type, Func<ActionQueue, QueueActionEventArgs, Task>>();
#if _USE_TREE
			Tree = new QueueTree {Game = game};
			Tree.OnBranchResolved += EndBlock;
			Tree.OnTreeResolved += game.OnQueueEmpty;
#endif
		}

		public ActionQueue(ActionQueue cloneFrom) : base(cloneFrom) {
#if _USE_QUEUE
			// BlockStart is immutable and uses only value types so we can just shallow clone
			BlockStack = new Stack<BlockStart>(cloneFrom.BlockStack.Reverse());
			foreach (var queue in cloneFrom.QueueStack.Reverse())
				QueueStack.Push(new Deque<QueueActionEventArgs>(queue.Select(q => (QueueActionEventArgs) q.Clone())));
			Queue = new Deque<QueueActionEventArgs>(cloneFrom.Queue.Select(q => (QueueActionEventArgs) q.Clone()));
#endif
			ResultStack = cloneFrom.ResultStack;
			ReplacedActions = cloneFrom.ReplacedActions;
			HasHistory = cloneFrom.HasHistory;
			Paused = cloneFrom.Paused;
			LastActionCancelled = cloneFrom.LastActionCancelled;
			// Events are immutable so this creates copies
			OnQueueing = cloneFrom.OnQueueing;
			OnQueued = cloneFrom.OnQueued;
			OnActionStarting = cloneFrom.OnActionStarting;
			OnAction = cloneFrom.OnAction;
			// Copy user data
			UserData = cloneFrom.UserData;
#if _USE_TREE
			// Copy queue tree
			Tree = (QueueTree)cloneFrom.Tree.Clone();
			Tree.OnBranchResolved += EndBlock;
#endif
		}

		public void Attach(Game game) {
			Game = game;
#if _USE_TREE
			Tree.Game = game;
			Tree.OnTreeResolved += game.OnQueueEmpty;
#endif
		}

		public void StartBlock(IEntity source, List<QueueAction> qa, BlockStart gameBlock = null) {
			if (qa == null) {
				Game.OnBlockEmpty(gameBlock);
				return;
			}
#if _QUEUE_DEBUG
			DebugLog.WriteLine("Queue (Game " + Game.GameId + "): Spawning new queue at depth " + (Depth + 1) + " for " + source.ShortDescription + " with actions: " +
			                   string.Join(" ", qa.Select(a => a.ToString())) + " for action block: " + (gameBlock?.ToString() ?? "none"));
#endif
#if _USE_TREE
			Tree.Stack();
#endif
#if _USE_QUEUE
			QueueStack.Push(Queue);
			Queue = new Deque<QueueActionEventArgs>();
			BlockStack.Push(gameBlock);
#endif
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

#if _USE_TREE
		// TODO: Obselete. Needs to be updated
		private void EndBlock(QueueNode parent)
		{
			var block = parent?.Data.Action as GameBlock;
			if (block != null)
				Game.OnBlockEmpty(block.Block);
		}
#endif

#if _USE_QUEUE
		private async Task EndBlockAsync() {
			if (Depth > 0) {
#if _QUEUE_DEBUG
				System.Diagnostics.Debug.Assert(IsBlockEmpty);
				DebugLog.WriteLine("Queue (Game " + Game.GameId + "): Destroying queue at depth " + Depth);
#endif
				Queue = QueueStack.Pop();
				var gameBlock = BlockStack.Pop();
				if (gameBlock != null)
					await Game.OnBlockEmptyAsync(gameBlock);
			}
			// When the queue is empty, notify the game - it may refill it with new actions
			if (IsEmpty)
				Game.OnQueueEmpty();
		}
#endif

		// Gets a QueueAction that can put into the queue
		private QueueActionEventArgs initializeAction(IEntity source, QueueAction qa) {
			return new QueueActionEventArgs(Game, source, qa);
		}

		public IEnumerable<ActionResult> RunMultiResult(IEntity source, List<QueueAction> qa) {
			if (qa == null)
				return new List<ActionResult>();
			StartBlock(source, qa);
			return ProcessBlock();
		}

		public IEnumerable<ActionResult> RunMultiResult(IEntity source, ActionGraph g) {
			return g != null ? RunMultiResult(source, g.Unravel()) : new List<ActionResult>();
		}

		public IEnumerable<ActionResult> RunMultiResult(IEntity source, QueueAction a) {
			return a != null ? RunMultiResult(source, new List<QueueAction> {a}) : new List<ActionResult>();
		}

		public ActionResult Run(IEntity source, List<QueueAction> qa) {
			return RunAsync(source, qa).Result;
		}

		public async Task<ActionResult> RunAsync(IEntity source, List<QueueAction> qa) {
			if (qa == null)
				return ActionResult.None;
			// TODO: If qa.Count == 1 and qa[0] is GameBlock then find a shortcut to avoid double-nesting
			StartBlock(source, qa);
			return (await ProcessBlockAsync())?.FirstOrDefault() ?? ActionResult.None;
		}

		public ActionResult Run(IEntity source, ActionGraph g) {
			return RunAsync(source, g).Result;
		}

		public async Task<ActionResult> RunAsync(IEntity source, ActionGraph g) {
			return g != null ? await RunAsync(source, g.Unravel()) : ActionResult.None;
		}

		public ActionResult Run(IEntity source, QueueAction a) {
			return RunAsync(source, a).Result;
		}

		public async Task<ActionResult> RunAsync(IEntity source, QueueAction a) {
			return a != null ? await RunAsync(source, new List<QueueAction> { a }) : ActionResult.None;
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
				if (!e.Cancel) {
#if _USE_QUEUE
					Queue.AddBack(e);
#endif
#if _USE_TREE
					Tree.Enqueue(e);
#endif
				}
			}
			else {
#if _USE_QUEUE
					Queue.AddBack(e);
#endif
#if _USE_TREE
				Tree.Enqueue(e);
#endif
			}
			OnQueued?.Invoke(this, e);
		}

		public void EnqueueDeferred(Action a) {
			Paused = true;
			a();
			Paused = false;
		}

		public IEnumerable<ActionResult> ProcessBlock(object UserData = null) {
			return ProcessBlockAsync(UserData).Result;
		}

		public async Task<IEnumerable<ActionResult>> ProcessBlockAsync(object UserData = null) {
#if _QUEUE_DEBUG
			DebugLog.WriteLine("Queue (Game " + Game.GameId + "): Start processing current block");
			var depth = Depth;
#endif
			var result = await ProcessAllAsync(UserData, Depth);
#if _QUEUE_DEBUG
			// Block might not be finished if queue was cancelled
			if (!LastActionCancelled)
				DebugLog.WriteLine("Queue (Game " + Game.GameId + "): End processing current block");
#endif
			return result;
		}

		public IEnumerable<ActionResult> ProcessAll(object UserData = null, int MaxUnwindDepth = 0) {
			return ProcessAllAsync(UserData, MaxUnwindDepth).Result;
		}

		public async Task<IEnumerable<ActionResult>> ProcessAllAsync(object UserData = null, int MaxUnwindDepth = 0) {
			while (await ProcessOneAsync(UserData, MaxUnwindDepth))
				;
			// Return whatever is left on the stack
			if (LastActionCancelled)
				return null;
			var stack = ResultStack;
			if (!Paused && IsEmpty)
				StackClear();
			return stack.Reverse();
		}

		public bool ProcessOne(object UserData = null, int MaxUnwindDepth = 0) {
			return ProcessOneAsync(UserData, MaxUnwindDepth).Result;
		}

		public async Task<bool> ProcessOneAsync(object UserData = null, int MaxUnwindDepth = 0) {
			if (Paused)
				return false;

			// Exit if the current branch is empty
			if (IsBlockEmpty)
				return false;

			LastActionCancelled = false;

			// Get next action and make sure it's up to date if cloned from another game
#if _USE_TREE
			var action = Tree.Current();
#endif
#if _USE_QUEUE
			var action = Queue.RemoveFront();
#endif
			action.Game = Game;
			if (action.Source.Game.GameId != Game.GameId)
				action.Source = Game.Entities[action.Source.Id];

#if _QUEUE_DEBUG
			DebugLog.WriteLine("Queue (Game " + Game.GameId + "): Dequeued action " + action + " for " + action.Source.ShortDescription + " at depth " + Depth);
#endif
			// TODO: Make it work when not all of the arguments are supplied, for flexible syntax

			// Get arguments for action from stack
			action.Args = new ActionResult[action.Action.Args.Count];
			for (int i = 0; i < action.Action.Args.Count; i++) {
				var arg = StackPop();
				List<IEntity> eList = arg;
				if (eList != null && eList.Count > 0 && eList[0].Game != Game)
					arg= new List<IEntity>(eList.Select(e => Game.Entities[e.Id]));
				action.Args[action.Action.Args.Count - i - 1] = arg;
			}

			// Replace current UserData with new UserData if supplied
			if (UserData != null)
				this.UserData = UserData;
			action.UserData = this.UserData;

			if (OnActionStarting != null) {
				OnActionStarting(this, action);
				if (action.Cancel) {
#if _USE_TREE
					Tree.MoveNext();
#endif
					LastActionCancelled = true;
					return false;
				}
			}
			var actionType = action.Action.GetType();
			if (ReplacedActions.ContainsKey(actionType)) {
				await ReplacedActions[actionType](this, action);
				// action.Cancel implied when action is replaced
				LastActionCancelled = true;
#if _USE_TREE
				Tree.MoveNext();
#endif
				return false;
			}
			if (HasHistory)
				AddItem(action);

			// Run action and push results onto stack
#if _QUEUE_DEBUG
			DebugLog.WriteLine("Queue (Game " + Game.GameId + "): Running action " + action + " for " + action.Source.ShortDescription + " at depth " + Depth);
#endif
			var result = action.Action.Run(action.Game, action.Source, action.Args);
			if (result.HasResult)
				StackPush(result);

#if _USE_TREE
			Tree.MoveNext();
			Tree.Unwind(MaxUnwindDepth);
#endif
#if _USE_QUEUE
			// The >= allows the current block to unwind for ProcessBlock()
			while (IsBlockEmpty && Depth >= MaxUnwindDepth) {
				await EndBlockAsync();
				if (IsEmpty)
					break;
			}
#endif
			OnAction?.Invoke(this, action);

			// Propagate cancellation up the chain by only changing it if not already set
			if (!LastActionCancelled)
				LastActionCancelled = action.Cancel;

			return !LastActionCancelled;
		}

		// Skip over an item (used when cloning mid-action to avoid an infinite loop)
		// NOTE: Does nothing when using a QueueStack because dequeued items are automatically removed
		public void MoveNext() {
#if _USE_TREE
			Tree.MoveNext();
#endif
		}

		public void StackPush(ActionResult i) {
			ResultStack = ResultStack.Push(i);
		}

		public ActionResult StackPop() {
			var i = ResultStack.Peek();
			ResultStack = ResultStack.Pop();
			return i;
		}

		public void StackClear() {
			ResultStack = ResultStack.Clear();
		}

		public void ReplaceAction<QAT>(Func<ActionQueue, QueueActionEventArgs, Task> evt) {
			ReplacedActions = ReplacedActions.SetItem(typeof(QAT), evt);
		}

		public string StackToString() {
			string s = string.Empty;
			foreach (var r in ResultStack)
				s += r + "\n";
			return s;
		}

		public override string ToString() {
			string s = string.Empty;
#if _USE_QUEUE
			s += "Current block:\n";
			foreach (var a in Queue)
				s += a + "\n";

			foreach (var b in QueueStack) {
				s += "\nStacked:\n";
				foreach (var a in b)
					s += a + "\n";
			}
#endif
			// TODO: QueueTree output
			return s;
		}

		public object Clone() {
			return new ActionQueue(this);
		}
	}
}
