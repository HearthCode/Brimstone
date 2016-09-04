using System;
using System.Collections.Generic;

namespace Brimstone
{
	public class QueueNode
	{
		public QueueActionEventArgs Data { get; }
		public int Depth { get; }
		public QueueNode NextSameDepth { get; set; }
		public QueueNode NextChildDepth { get; set; }

		public QueueNode(QueueActionEventArgs data, int depth = 0) {
			Data = data;
			Depth = depth;
		}
	}

	public class QueueTree
	{
		public Stack<QueueNode> InsertionPoints { get; }
		public Stack<QueueNode> DequeuePoints { get; }
		public int Depth => InsertionPoints.Count;
		public QueueNode RootNode { get; set; }
		public QueueNode CurrentInsertionPoint { get; private set; }
		public QueueNode CurrentDequeuePoint { get; private set; }

		public event Action OnBranchResolved;
		public event Action OnTreeResolved;

		public QueueTree() {
			InsertionPoints = new Stack<QueueNode>();
			DequeuePoints = new Stack<QueueNode>();
		}

		public void Enqueue(QueueActionEventArgs action) {
			if (RootNode == null) {
#if _QUEUE_DEBUG
				DebugLog.WriteLine("QueueTree: Seeding root node with " + action);
#endif
				RootNode = new QueueNode(action);
				CurrentInsertionPoint = RootNode;
				CurrentDequeuePoint = RootNode;
			}
			else {
				var node = new QueueNode(action, Depth);
				if (node.Depth == CurrentInsertionPoint.Depth) {
#if _QUEUE_DEBUG
					DebugLog.WriteLine("QueueTree: Adding node " + action + " - at depth " + Depth + " as sibling");
#endif
					CurrentInsertionPoint.NextSameDepth = node;
				}
				else {
#if _QUEUE_DEBUG
					DebugLog.WriteLine("QueueTree: Adding node " + action + " - at depth " + Depth + " as child");
#endif
					CurrentInsertionPoint.NextChildDepth = node;
				}
				CurrentInsertionPoint = node;
				if (CurrentDequeuePoint == null)
					CurrentDequeuePoint = node;
			}
		}

		public QueueActionEventArgs Dequeue(int MaxUnwindDepth = 0) {
			if (CurrentDequeuePoint == null)
				return null;

			var node = CurrentDequeuePoint;
			CurrentDequeuePoint = node.NextChildDepth ?? node.NextSameDepth;
#if _QUEUE_DEBUG
			DebugLog.WriteLine("QueueTree: Dequeued " + node.Data + " - at depth " + Depth + "; next dequeue will be at "
				+ (CurrentDequeuePoint == node.NextChildDepth && CurrentDequeuePoint != null? "child" :
						(CurrentDequeuePoint == node.NextSameDepth && CurrentDequeuePoint != null? "same" :
						"parent (if no new nodes enqueued)")) + " depth");
#endif
			// Let nodes we are finished with go out of scope so they can be garbage collected
			if (Depth == 0)
				RootNode = node;
			return node.Data;
		}

		public int Stack() {
			InsertionPoints.Push(CurrentInsertionPoint);
			DequeuePoints.Push(CurrentDequeuePoint);
#if _QUEUE_DEBUG
			DebugLog.WriteLine("QueueTree: Created new branch at depth " + Depth);
#endif
			return Depth;
		}

		public void Unwind(int MaxUnwindDepth) {
			while (CurrentDequeuePoint == null && Depth >= MaxUnwindDepth && Depth > 0) {
#if _QUEUE_DEBUG
				DebugLog.WriteLine("QueueTree: Reached end of branch at depth " + Depth);
#endif
				OnBranchResolved?.Invoke();
				CurrentDequeuePoint = DequeuePoints.Pop();
				CurrentInsertionPoint = InsertionPoints.Pop();
			}

			// Queue empty
			if (CurrentDequeuePoint == null && Depth == 0) {
#if _QUEUE_DEBUG
				DebugLog.WriteLine("QueueTree: Reached end of tree");
#endif
				RootNode = null;
				OnTreeResolved?.Invoke();
				if (CurrentDequeuePoint != null || Depth > 0)
					Unwind(MaxUnwindDepth);
			}
#if _QUEUE_DEBUG
			if (CurrentDequeuePoint != null)
				DebugLog.WriteLine("QueueTree: Next node to dequeue after unwind is " + CurrentDequeuePoint.Data + " at depth " + Depth);
#endif
		}
	}
}
