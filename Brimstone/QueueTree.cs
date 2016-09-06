using System;
using System.Collections.Generic;
using System.Linq;

namespace Brimstone
{
	public class QueueNode
	{
		public Game Owner { get; }
		public QueueActionEventArgs Data { get; }
		public int Depth { get; }
		public QueueNode Parent { get; }
		public QueueNode NextInsertionPoint { get; set; }
		public Dictionary<Game, QueueNode> Next { get; } = new Dictionary<Game, QueueNode>();

		public QueueNode(Game owner, QueueActionEventArgs data, QueueNode parent = null, int depth = 0) {
			Owner = owner;
			Data = data;
			Parent = parent;
			Depth = depth;
		}
	}

	public class QueueTree : ICloneable
	{
		private QueueNode RootNode;
		private QueueNode CurrentInsertionPoint;
		private QueueNode CurrentDequeuePoint;
		private QueueNode PreviousDequeuePoint;
		private QueueNode NextParentToDequeue;
		private Game Tracking;

		public Game Game { get; set; }
		public int Depth { get; private set; }
		public bool IsBranchEmpty => CurrentDequeuePoint == null;
		public bool IsEmpty => CurrentDequeuePoint == null && Depth == 0;

		public event Action<QueueNode> OnBranchResolved;
		public event Action OnTreeResolved;

		public QueueTree() { }

		public QueueTree(QueueTree cloneFrom) {
			// Does not clone events
			Depth = cloneFrom.Depth;
			RootNode = cloneFrom.RootNode;
			CurrentInsertionPoint = cloneFrom.CurrentInsertionPoint;
			CurrentDequeuePoint = cloneFrom.CurrentDequeuePoint;
			PreviousDequeuePoint = cloneFrom.PreviousDequeuePoint;
			NextParentToDequeue = cloneFrom.NextParentToDequeue;
			Tracking = cloneFrom.Tracking;
		}

		public void Enqueue(QueueActionEventArgs action) {
			if (RootNode == null && Depth == 0) {
#if _QUEUE_DEBUG
				DebugLog.WriteLine("QueueTree [" + Game.GameId + "]: Seeding root node with " + action);
#endif
				RootNode = new QueueNode(Game, action);
				CurrentInsertionPoint = RootNode;
				CurrentDequeuePoint = RootNode;
				PreviousDequeuePoint = null;
				NextParentToDequeue = null;
				Tracking = Game;
			}
			else {
				QueueNode node;
				// Null insertion point means the tree is empty but we have spawned a new branch with no root action
				if (CurrentInsertionPoint != null && Depth == CurrentInsertionPoint.Depth)
				{
#if _QUEUE_DEBUG
					DebugLog.WriteLine("QueueTree [" + Game.GameId + "]: Adding node " + action + " - at depth " + Depth + " as sibling");
#endif
					CurrentInsertionPoint.Next.Add(Game, node = new QueueNode(Game, action, CurrentInsertionPoint.Parent, Depth));
				} else {
#if _QUEUE_DEBUG
					DebugLog.WriteLine("QueueTree [" + Game.GameId + "]: Adding node " + action + " - at depth " + Depth + " as child");
#endif
					CurrentDequeuePoint = node = new QueueNode(Game, action, CurrentInsertionPoint, Depth);
				}
				CurrentInsertionPoint = node;
			}
		}

		public QueueActionEventArgs Current()
		{
			PreviousDequeuePoint = CurrentDequeuePoint;
			if (CurrentDequeuePoint == null) {
				if (RootNode == null) {
#if _QUEUE_DEBUG
					DebugLog.WriteLine("QueueTree [" + Game.GameId + "]: Attempt to dequeue when tree is empty");
#endif
					return null;
				}
				CurrentDequeuePoint = RootNode;
#if _QUEUE_DEBUG
				DebugLog.WriteLine("QueueTree [" + Game.GameId + "]: Restarting from root node");
#endif
			}
#if _QUEUE_DEBUG
			DebugLog.WriteLine("QueueTree [" + Game.GameId + "]: Fetched " + CurrentDequeuePoint.Data + " - at depth " + Depth);
#endif
			// Let nodes we are finished with go out of scope so they can be garbage collected
			if (Depth == 0)
				RootNode = CurrentDequeuePoint;

			return CurrentDequeuePoint.Data;
		}

		public void Stack() {
			NextParentToDequeue = CurrentDequeuePoint;
			if (CurrentDequeuePoint != null)
				CurrentDequeuePoint.NextInsertionPoint = CurrentInsertionPoint;
			Depth++;
			// In case there is no root node...
			if (Tracking == null)
				Tracking = Game;
#if _QUEUE_DEBUG
			DebugLog.WriteLine("QueueTree [" + Game.GameId + "]: Created new branch at depth " + Depth + " (current dequeue point is: " + (CurrentDequeuePoint?.Data.ToString() ?? "<end of branch>") + ")");
#endif
		}

		public void MoveNext() {
			if (CurrentDequeuePoint == null) {
#if _QUEUE_DEBUG
				DebugLog.WriteLine("QueueTree [" + Game.GameId + "]: Cannot advance further at depth " + Depth);
#endif
				return;
			}
			if (CurrentDequeuePoint == PreviousDequeuePoint) {
				QueueNode nextDequeuePoint;
				while (!CurrentDequeuePoint.Next.TryGetValue(Tracking, out nextDequeuePoint) && Tracking != Game) {
					Tracking = GetNextOldestTrackingChild();
				}
				CurrentDequeuePoint = nextDequeuePoint;
#if _QUEUE_DEBUG
				DebugLog.WriteLine("QueueTree [" + Game.GameId + "]: Moved to next node: " + (CurrentDequeuePoint?.Data.ToString() ?? "<end of branch>") + " at depth " + Depth);
#endif
			}
		}

		private Game GetNextOldestTrackingChild() {
			var nextTrackingChild = Game;
			// TODO: Should work even when PowerHistory is disabled
			Game parent;
			do
			{
				parent = ((PowerHistory) nextTrackingChild.PowerHistory.Parent)?.Game;
				if (parent != Tracking)
					nextTrackingChild = parent;
			} while (parent != Tracking && nextTrackingChild != null);
			return nextTrackingChild;
		}

		public void Unwind(int MaxUnwindDepth)
		{
#if _QUEUE_DEBUG
			DebugLog.WriteLine("QueueTree [" + Game.GameId + "]: Unwinding to minimum depth " + MaxUnwindDepth);
			bool changed = false;
#endif
			while (IsBranchEmpty && Depth >= MaxUnwindDepth && Depth > 0) {
#if _QUEUE_DEBUG
				DebugLog.WriteLine("QueueTree [" + Game.GameId + "]: Reached end of branch at depth " + Depth);
#endif
				PreviousDequeuePoint = CurrentDequeuePoint = NextParentToDequeue;
				CurrentInsertionPoint = CurrentDequeuePoint?.NextInsertionPoint;
				OnBranchResolved?.Invoke(PreviousDequeuePoint);
				Depth--;
				NextParentToDequeue = CurrentDequeuePoint;
				MoveNext();
#if _QUEUE_DEBUG
				changed = true;
#endif
			}

			// Queue empty
			if (IsEmpty) {
				RootNode = PreviousDequeuePoint = CurrentDequeuePoint = CurrentInsertionPoint = null;
				Depth = 0;
				OnTreeResolved?.Invoke();
#if _QUEUE_DEBUG
				DebugLog.WriteLine("QueueTree [" + Game.GameId + "]: Reached end of tree");
				changed = true;
#endif
			}
#if _QUEUE_DEBUG
			if (changed)
				DebugLog.WriteLine("QueueTree [" + Game.GameId + "]: Next node to dequeue after unwind is: " + (CurrentDequeuePoint?.Data.ToString() ?? "<none>") + " at depth " + Depth);
#endif
		}

		public object Clone() {
			return new QueueTree(this);
		}
	}
}
