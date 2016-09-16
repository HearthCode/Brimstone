using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Brimstone
{
	public abstract class ListTree<TItem> : IEnumerable<TItem>
	{
		public ListTree<TItem> Parent { get; }
		public Queue<TItem> Delta { get; } = new Queue<TItem>();
		public int SequenceNumber { get; private set; }
		public int ParentBranchEntry { get; }

		public ListTree(ListTree<TItem> parent = null) {
			if (parent != null) {
				Parent = parent;
				SequenceNumber = Parent.SequenceNumber;
				ParentBranchEntry = SequenceNumber;
			}
			else {
				Parent = null;
				SequenceNumber = 0;
				ParentBranchEntry = 0;
			}
		}

		protected void AddItem(TItem i) {
			Delta.Enqueue(i);
			SequenceNumber++;
		}

		public IEnumerable<TItem> DeltaTo(int childBranchPoint) {
			return Delta.Take(childBranchPoint - ParentBranchEntry);
		}

		// Return the delta from the point where the specified tree was created
		public IEnumerable<TItem> DeltaSince(ListTree<TItem> tree) {
			if (ReferenceEquals(tree, null))
				return null;

			if (ReferenceEquals(tree, this))
				return Delta;

			IEnumerable<TItem> delta = null;

			bool found = false;
			int branchPoint = SequenceNumber;

			for (var t = this; t != null && !found; t = t.Parent) {
				delta = delta != null ? t.DeltaTo(branchPoint).Concat(delta) : t.DeltaTo(branchPoint);
				branchPoint = t.ParentBranchEntry;
				found = t == tree;
			}
			return found ? delta : null;
		}

		public ListTree<TItem> LowestCommonAncestor(ListTree<TItem> parent) 
		{
			// Get all ancestors of each PowerHistory log
			var ancestorsA = new Stack<ListTree<TItem>>();
			var ancestorsB = new Stack<ListTree<TItem>>();

			for (var tree = this; tree != null; tree = tree.Parent)
				ancestorsA.Push(tree);
			for (var tree = parent; tree != null; tree = tree.Parent)
				ancestorsB.Push(tree);

			// Search from root game to find lowest common ancestor of each game
			// TODO: This also needs to work when there is no LCA
			ListTree<TItem> lca = null;
			foreach (var pair in ancestorsA.Zip(ancestorsB, (x, y) => new { A = x, B = y }))
				if (pair.A != pair.B)
					break;
				else {
					lca = pair.A;
				}
			return lca;
		}

		/// <summary>
		/// Returns an enumerator that iterates through the list in chronological order, including all ancestors
		/// </summary>
		/// <returns></returns>
		public IEnumerator<TItem> GetEnumerator() {
			if (ParentBranchEntry == 0)
				return Delta.GetEnumerator();

			return Parent.Concat(Delta).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}

		public override string ToString() {
			string treeString = string.Empty;
			foreach (var p in Delta)
				treeString += p.ToString() + "\n";
			return treeString;
		}
	}
}
