/*
	Copyright 2016, 2017 Katy Coe

	This file is part of Brimstone.

	Brimstone is free software: you can redistribute it and/or modify
	it under the terms of the GNU Lesser General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	Brimstone is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU Lesser General Public License for more details.

	You should have received a copy of the GNU Lesser General Public License
	along with Brimstone.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Brimstone.QueueActions;
using Brimstone.Entities;
using Brimstone.PowerActions;

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
		public Stack<Deque<QueueActionEventArgs>> QueueStack = new Stack<Deque<QueueActionEventArgs>>();
		public Deque<QueueActionEventArgs> Queue;
		private readonly Stack<BlockStart> BlockStack;
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
		public bool IsBlockEmpty => Queue.Count == 0;
		public bool IsEmpty => Queue.Count == 0 && Depth == 0;
		public int Depth => QueueStack.Count;

		public ActionQueue(Game game, bool actionHistory, object userData = null) : base(null) {
			Game = game;
			Paused = false;
			HasHistory = actionHistory;
			UserData = userData;
			Queue = new Deque<QueueActionEventArgs>();
			BlockStack = new Stack<BlockStart>();
			ResultStack = ImmutableStack.Create<ActionResult>();
			ReplacedActions = ImmutableDictionary.Create<Type, Func<ActionQueue, QueueActionEventArgs, Task>>();
		}

		public ActionQueue(ActionQueue cloneFrom) : base(cloneFrom) {
			// BlockStart is immutable and uses only value types so we can just shallow clone
			BlockStack = new Stack<BlockStart>(cloneFrom.BlockStack.Reverse());
			foreach (var queue in cloneFrom.QueueStack.Reverse())
				QueueStack.Push(new Deque<QueueActionEventArgs>(queue.Select(q => (QueueActionEventArgs)q.Clone())));
			Queue = new Deque<QueueActionEventArgs>(cloneFrom.Queue.Select(q => (QueueActionEventArgs)q.Clone()));
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
		}

		public void Attach(Game game) {
			Game = game;
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
			QueueStack.Push(Queue);
			Queue = new Deque<QueueActionEventArgs>();
			BlockStack.Push(gameBlock);
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

		private void EndBlock() {
			if (Depth > 0) {
#if _QUEUE_DEBUG
				System.Diagnostics.Debug.Assert(IsBlockEmpty);
				DebugLog.WriteLine("Queue (Game " + Game.GameId + "): Destroying queue at depth " + Depth);
#endif
				Queue = QueueStack.Pop();
				var gameBlock = BlockStack.Pop();
				if (gameBlock != null)
					Game.OnBlockEmpty(gameBlock);
			}
			// When the queue is empty, notify the game - it may refill it with new actions
			if (IsEmpty)
				Game.OnQueueEmpty();
		}

		// Gets a QueueAction that can put into the queue
		private QueueActionEventArgs initializeAction(IEntity source, QueueAction qa) {
			return new QueueActionEventArgs(Game, source, qa);
		}

		public ActionResult Run(IEntity source, List<QueueAction> qa) {
			return RunAsync(source, qa).Result;
		}

		public Task<ActionResult> RunAsync(IEntity source, List<QueueAction> qa) {
			if (qa == null)
				return Task.FromResult(ActionResult.None);
			// TODO: If qa.Count == 1 and qa[0] is GameBlock then find a shortcut to avoid double-nesting
			StartBlock(source, qa);
			return ProcessBlockAsync();
		}

		public ActionResult Run(IEntity source, ActionGraph g) {
			return RunAsync(source, g).Result;
		}

		public Task<ActionResult> RunAsync(IEntity source, ActionGraph g) {
			return g != null ? RunAsync(source, g.Unravel()) : Task.FromResult(ActionResult.None);
		}

		public ActionResult Run(IEntity source, QueueAction a) {
			return RunAsync(source, a).Result;
		}

		public Task<ActionResult> RunAsync(IEntity source, QueueAction a) {
			return a != null ? RunAsync(source, new List<QueueAction> { a }) : Task.FromResult(ActionResult.None);
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
					Queue.AddBack(e);
				}
			}
			else {
				Queue.AddBack(e);
			}
			OnQueued?.Invoke(this, e);
		}

		public void EnqueueDeferred(Action a) {
			Paused = true;
			a();
			Paused = false;
		}

		public ActionResult ProcessBlock(object UserData = null) {
			return ProcessBlockAsync(UserData).Result;
		}

		public Task<ActionResult> ProcessBlockAsync(object UserData = null) {
#if _QUEUE_DEBUG
			DebugLog.WriteLine("Queue (Game " + Game.GameId + "): Start processing current block");
			var depth = Depth;

			// Block might not be finished if queue was cancelled
			var result = ProcessAllAsync(UserData, Depth).ContinueWith(x => {
				if (!LastActionCancelled)
					DebugLog.WriteLine("Queue (Game " + Game.GameId + "): End processing current block");
				return x.Result;
			});
#else
			var result = ProcessAllAsync(UserData, Depth);
#endif
			return result;
		}

		public ActionResult ProcessAll(object UserData = null, int MaxUnwindDepth = 0) {
			return ProcessAllAsync(UserData, MaxUnwindDepth).Result;
		}

		public async Task<ActionResult> ProcessAllAsync(object UserData = null, int MaxUnwindDepth = 0, bool one = false) {
			LastActionCancelled = false;
			bool DoneOne = false;
			while (!Paused && !IsBlockEmpty && !LastActionCancelled && !DoneOne) {
				if (one)
					DoneOne = true;
				// Get next action and make sure it's up to date if cloned from another game
				var action = Queue.RemoveFront();
				action.Game = Game;
				if (action.Source.Game.GameId != Game.GameId)
					action.Source = Game.Entities[action.Source.Id];
#if _QUEUE_DEBUG
				DebugLog.WriteLine("Queue (Game " + Game.GameId + "): Dequeued action " + action + " for " + action.Source.ShortDescription + " at depth " + Depth);
#endif
				// Get needed arguments for action from stack
				action.Args = new ActionResult[action.Action.Args.Count];
				for (int i = action.Action.Args.Count - 1; i >= 0; i--) {
					// Prefer PreCompiledQueueAction items as arguments
					var arg = action.Action.CompiledArgs[i];
					if (!arg.HasResult) {
						// Otherwise prefer EagerQueueAction items as arguments
						if (action.Action.EagerArgs[i] != null) {
							arg = action.Action.EagerArgs[i].Run(Game, action.Source, null);
						}
						else {
							// Otherwise use the ResultStack to get regular QueueAction items as arguments
							// In this round, only pop arguments for which ActionGraph parameters were supplied
							// to the QueueAction as these will be at the top of the stack
							if (action.Action.Args[i] != null) {
								arg = StackPop();
								List<IEntity> eList = arg;
								if (eList != null && eList.Count > 0 && eList[0].Game != Game)
									arg = new List<IEntity>(eList.Select(e => Game.Entities[e.Id]));
							}
						}
					}
					action.Args[i] = arg;
				}
				// Now go through all of the arguments that weren't supplied as ActionGraphs and pop them off the stack
				for (int i = action.Action.Args.Count - 1; i >= 0; i--) {
					if (action.Action.Args[i] == null) {
						var arg = StackPop();
						List<IEntity> eList = arg;
						if (eList != null && eList.Count > 0 && eList[0].Game != Game)
							arg = new List<IEntity>(eList.Select(e => Game.Entities[e.Id]));
						action.Args[i] = arg;
					}
				}

				// Replace current UserData with new UserData if supplied
				if (UserData != null)
					this.UserData = UserData;
				action.UserData = this.UserData;

				if (OnActionStarting != null) {
					OnActionStarting(this, action);
					if (action.Cancel) {
						LastActionCancelled = true;
						continue;
					}
				}
				var actionType = action.Action.GetType();
				if (ReplacedActions.ContainsKey(actionType)) {
					await ReplacedActions[actionType](this, action);
					// action.Cancel implied when action is replaced
					LastActionCancelled = true;
					continue;
				}
				if (HasHistory)
					AddItem(action);

				// Run action and push results onto stack
#if _QUEUE_DEBUG
				DebugLog.WriteLine("Queue (Game " + Game.GameId + "): Running action " + action + " for " + action.Source.ShortDescription + " at depth " + Depth);
#endif
				var @async = action.Action as QueueActionAsync;
				var result = (@async != null
					? await @async.RunAsync(action.Game, action.Source, action.Args)
					: action.Action.Run(action.Game, action.Source, action.Args));
				if (result.HasResult)
					StackPush(result);
				// The >= allows the current block to unwind for ProcessBlock()
				while (IsBlockEmpty && Depth >= MaxUnwindDepth) {
					EndBlock();
					if (IsEmpty)
						break;
				}
#if _QUEUE_DEBUG
				DebugLog.WriteLine("Queue (Game " + Game.GameId + "): Finished action " + action + " for " + action.Source.ShortDescription + " at depth " + Depth);
#endif
				OnAction?.Invoke(this, action);

				// Propagate cancellation up the chain by only changing it if not already set
				LastActionCancelled = action.Cancel;
			}
			// Return whatever is left on the stack
			if (LastActionCancelled)
				return ActionResult.None;
			var finalResult = ResultStack.Reverse().FirstOrDefault();
			if (!Paused && IsEmpty)
				StackClear();
			return finalResult;
		}

		public ActionResult ProcessOne(object UserData = null, int MaxUnwindDepth = 0) {
			return ProcessAllAsync(UserData, MaxUnwindDepth, true).Result;
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
