using Brimstone.Entities;

namespace Brimstone.Tree
{
	public class GameTree<TNode> where TNode : GameNode {
		public TNode RootNode { get; }

		// The total number of clones including the root node in this tree
		// TODO: Doesn't work automatically
		private volatile int _nodeCount = 0;
		public int NodeCount { get { return _nodeCount; } protected set { _nodeCount = value; } }

		// The total number of non-pruned leaf nodes kept
		// TODO: Doesn't work automatically
		private volatile int _leafNodeCount = 0;
		public int LeafNodeCount { get { return _leafNodeCount; } set { _leafNodeCount = value; } }

		public GameTree(TNode RootNode) {
			this.RootNode = RootNode;
		}
	}

	public static class GameTree
	{
		public static GameTree<GameNode> From(Game Root) {
			return new GameTree<GameNode>(new GameNode(Root));
		}
	}
}
