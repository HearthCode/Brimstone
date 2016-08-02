//#define _TREE_DEBUG

using System;
using System.Collections.Generic;
using System.Linq;

namespace Brimstone
{
	public interface ITreeSearcher
	{
		void Visitor(Game cloned, GameTree tree, QueueActionEventArgs e);
		HashSet<Game> GetUniqueGames();
	}

	public enum SearchMode
	{
		None,
		Naive,
		DepthFirst,
		BreadthFirst
	}

	public class GameNode
	{
		public Game Game { get; }
		public GameNode Parent { get; }
		public HashSet<GameNode> Children { get; }
		public double Probability { get; }

		public GameNode(Game Game, GameNode Parent = null, double Probability = 1.0, bool TrackChildren = true) {
			this.Game = Game;
			this.Parent = Parent;
			this.Probability = Probability;
			if (TrackChildren) {
				Children = new HashSet<GameNode>();
				if (Parent != null)
					Parent.AddChild(this);
			}
		}

		public void AddChild(GameNode child) {
			Children.Add(child);
		}

		public GameNode Branch(double Probability = 1.0) {
			var clone = Game.CloneState() as Game;
			var node = new GameNode(clone, this, Probability, Children != null);
			clone.CustomData = node;
			return node;
		}
	}

	public class GameTree
	{
		public GameNode RootNode { get; }
		public bool TrackChildren { get; set; }
		public int NodeCount { get; protected set; } = 0;
		public int LeafNodeCount { get; set; } = 0;

		private ITreeSearcher searcher = null;
		private HashSet<Game> uniqueGames = new HashSet<Game>();

		public HashSet<Game> GetUniqueGames() {
			if (uniqueGames.Count > 0)
				return uniqueGames;
			uniqueGames = searcher.GetUniqueGames();
			return uniqueGames;
		}

		public GameTree(Game Root, SearchMode SearchMode, bool CloneRoot = false) {
			var rootGame = CloneRoot ? Root.CloneState() as Game : Root;

			TrackChildren = (SearchMode == SearchMode.None);
			RootNode = new GameNode(Game: rootGame, TrackChildren: TrackChildren);

			if (SearchMode != SearchMode.None) {
				RootNode.Game.ActionQueue.ReplaceAction<RandomChoice>(replaceRandomChoice);
				RootNode.Game.ActionQueue.ReplaceAction<RandomAmount>(replaceRandomAmount);
				RootNode.Game.CustomData = RootNode;

				switch (SearchMode) {
					case SearchMode.Naive: searcher = new NaiveTreeSearch(); break;
					case SearchMode.DepthFirst: searcher = new DepthFirstTreeSearch(); break;
				}
			}
		}

		public static GameTree BuildFor(Game Game, Action Action, SearchMode SearchMode = SearchMode.BreadthFirst) {
			var tree = new GameTree(Game, SearchMode);
			Action();
			return tree;
		}

		protected void replaceRandomChoice(ActionQueue q, QueueActionEventArgs e) {
			// Choosing a random entity (minion in this case)
			// Clone and start processing for every possibility
#if _TREE_DEBUG
			Console.WriteLine("");
			Console.WriteLine("--> Depth: " + e.Game.Depth);
#endif
			foreach (Entity entity in e.Args[RandomChoice.ENTITIES]) {
				// When cloning occurs, RandomChoice has been pulled from the action queue,
				// so we can just insert a fixed item at the start of the queue and restart the queue
				// to effectively replace it
				var cloned = ((GameNode)e.Game.CustomData).Branch().Game;
				NodeCount++;
				cloned.ActionQueue.InsertDeferred(e.Source, entity);
				cloned.ActionQueue.ProcessAll();
				searcher.Visitor(cloned, this, e);
			}
#if _TREE_DEBUG
			Console.WriteLine("<-- Depth: " + e.Game.Depth);
			Console.WriteLine("");
#endif
		}

		protected void replaceRandomAmount(ActionQueue q, QueueActionEventArgs e) {
			// Choosing a random value (damage amount in this case)
			// Clone and start processing for every possibility
#if _TREE_DEBUG
			Console.WriteLine("");
			Console.WriteLine("--> Depth: " + e.Game.Depth);
#endif
			for (int i = e.Args[RandomAmount.MIN]; i <= e.Args[RandomAmount.MAX]; i++) {
				// When cloning occurs, RandomAmount has been pulled from the action queue,
				// so we can just insert a fixed number at the start of the queue and restart the queue
				// to effectively replace it
				var cloned = ((GameNode)e.Game.CustomData).Branch().Game;
				NodeCount++;
				cloned.ActionQueue.InsertDeferred(e.Source, i);
				cloned.ActionQueue.ProcessAll();
				searcher.Visitor(cloned, this, e);
			}
#if _TREE_DEBUG
			Console.WriteLine("<-- Depth: " + e.Game.Depth);
			Console.WriteLine("");
#endif
		}
	}

	public class NaiveTreeSearch : ITreeSearcher
	{
		private HashSet<Game> leafNodeGames = new HashSet<Game>();

		public void Visitor(Game cloned, GameTree tree, QueueActionEventArgs e) {
			// If the action queue is empty, we have reached a leaf node game state
			if (cloned.ActionQueue.Queue.Count == 0) {
				tree.LeafNodeCount++;
				leafNodeGames.Add(cloned);
			}
		}

		public HashSet<Game> GetUniqueGames() {
			HashSet<Game> uniqueGames = new HashSet<Game>();

			while (leafNodeGames.Count > 0) {
				var root = leafNodeGames.Take(1).ToList()[0];
				leafNodeGames.Remove(root);
				var different = new HashSet<Game>();

				// Hash every entity
				// WARNING: This relies on a good hash function!
				var e1 = new HashSet<IEntity>(root.Entities, new FuzzyEntityComparer());

				foreach (var g in leafNodeGames) {
					if (g.Entities.Count != root.Entities.Count) {
						different.Add(g);
						continue;
					}
					var e2 = new HashSet<IEntity>(g.Entities, new FuzzyEntityComparer());
#if _TREE_DEBUG
					if (e2.Count < g.Entities.Count || e1.Count < root.Entities.Count) {
						// Potential hash collision
						var c = (e2.Count < g.Entities.Count ? e2 : e1);
						var g2 = (c == e2 ? g : root);
						var ent = g2.Entities.Select(x => x.FuzzyHash).ToList();
						// Build list of collisions
						var collisions = new Dictionary<int, IEntity>();
						foreach (var e in g2.Entities) {
							if (collisions.ContainsKey(e.FuzzyHash)) {
								// It's not a coliision if the tag set differs only by entity ID
								bool collide = false;
								foreach (var tagPair in e.Zip(collisions[e.FuzzyHash], (x, y) => new { A = x, B = y })) {
									if (tagPair.A.Key == GameTag.ENTITY_ID && tagPair.B.Key == GameTag.ENTITY_ID)
										continue;
									if (tagPair.A.Key != tagPair.A.Key || tagPair.B.Value != tagPair.B.Value) {
										collide = true;
										break;
									}
								}
								if (collide) {
									Console.WriteLine(collisions[e.FuzzyHash]);
									Console.WriteLine(e);
									Console.WriteLine(collisions[e.FuzzyHash].FuzzyHash + " " + e.FuzzyHash);
									throw new Exception("Hash collision - not safe to compare games");
								}
							}
							else
								collisions.Add(e.FuzzyHash, e);
						}
					}
#endif
					if (!e2.SetEquals(e1)) {
						different.Add(g);
					}
				}
				uniqueGames.Add(root);
				leafNodeGames = different;
#if _TREE_DEBUG
				Console.WriteLine("{0} games remaining to process ({1} unique games found so far)", different.Count, uniqueGames.Count);
#endif
			}
			return uniqueGames;
		}
	}

	public class DepthFirstTreeSearch : ITreeSearcher
	{
		private HashSet<Game> uniqueGames = new HashSet<Game>(new FuzzyGameComparer());

		public void Visitor(Game cloned, GameTree tree, QueueActionEventArgs e) {
			// If the action queue is empty, we have reached a leaf node game state
			// so compare it for equality with other final game states
			if (cloned.ActionQueue.Queue.Count == 0)
				if (!cloned.EquivalentTo(e.Game)) {
					tree.LeafNodeCount++;
					// This will cause the game to be discarded if its fuzzy hash matches any other final game state
#if _TREE_DEBUG
					var oc = uniqueGames.Count;
#endif
					uniqueGames.Add(cloned);

#if _TREE_DEBUG
					if (oc < uniqueGames.Count)
						Console.WriteLine("UNIQUE GAME FOUND ({0})", oc + 1);
					else
						Console.WriteLine("DUPLICATE GAME FOUND");
#endif
				}
		}

		public HashSet<Game> GetUniqueGames() {
			return uniqueGames;
		}
	}
}