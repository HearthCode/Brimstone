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
