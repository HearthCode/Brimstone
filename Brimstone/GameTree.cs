//#define _TREE_DEBUG

using System;
using System.Collections.Generic;
using System.Linq;

namespace Brimstone
{
	public interface ITreeSearcher
	{
		void Visitor(Game cloned, GameTree tree, QueueActionEventArgs e);
		void PostAction(ActionQueue q, GameTree tree, QueueActionEventArgs e);
		void PostProcess(GameTree tree);
		HashSet<Game> GetUniqueGames();
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

		// The total number of clones including the root node in this tree
		public int NodeCount { get; protected set; } = 0;

		// The total number of non-pruned leaf nodes kept
		public int LeafNodeCount { get; set; } = 0;

		private ITreeSearcher searcher = null;
		private HashSet<Game> uniqueGames = new HashSet<Game>();

		public HashSet<Game> GetUniqueGames() {
			if (uniqueGames.Count > 0)
				return uniqueGames;
			uniqueGames = searcher.GetUniqueGames();
			return uniqueGames;
		}

		public GameTree(Game Root, ITreeSearcher SearchMode = null, bool CloneRoot = false) {
			var rootGame = CloneRoot ? Root.CloneState() as Game : Root;

			TrackChildren = (SearchMode == null);
			RootNode = new GameNode(Game: rootGame, TrackChildren: TrackChildren);

			if (SearchMode != null) {
				RootNode.Game.ActionQueue.ReplaceAction<RandomChoice>(replaceRandomChoice);
				RootNode.Game.ActionQueue.ReplaceAction<RandomAmount>(replaceRandomAmount);
				RootNode.Game.ActionQueue.OnAction += (o, e) => {
					searcher.PostAction(o as ActionQueue, this, e);
				};
			}
			rootGame.CustomData = RootNode;
			searcher = SearchMode;
		}

		public void Run(Action Action) {
			Action();
			searcher.PostProcess(this);
		}

		public static GameTree BuildFor(Game Game, Action Action, ITreeSearcher SearchMode = null) {
			if (SearchMode == null)
				SearchMode = new BreadthFirstTreeSearch();
			var tree = new GameTree(Game, SearchMode);
			tree.Run(Action);
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

		public void PostAction(ActionQueue q, GameTree t, QueueActionEventArgs e) {	}

		public void PostProcess(GameTree t) { }

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

		public void PostAction(ActionQueue q, GameTree t, QueueActionEventArgs e) { }

		public void PostProcess(GameTree t) { }

		public HashSet<Game> GetUniqueGames() {
			return uniqueGames;
		}
	}

	public class BreadthFirstTreeSearch : ITreeSearcher
	{
		private HashSet<Game> uniqueGames = new HashSet<Game>(new FuzzyGameComparer());

		// The pruned search queue for the current search depth
		private HashSet<Game> searchQueue = new HashSet<Game>(new FuzzyGameComparer());

		// The fuzzy game hash for the game state we are currently executing before exeuction started
		// Used to check if the game state changes after an action completes
		// TODO: Make thread-safe
		private int preActionHash = 0;

		public void Visitor(Game cloned, GameTree tree, QueueActionEventArgs e) { }

		// When an in-game action completes, check if the game state has changed
		// Some actions (like selectors) won't cause the game state to change,
		// so we continue running these until a game state change occurs
		public void PostAction(ActionQueue q, GameTree t, QueueActionEventArgs e) {
			if (e.Game.Entities.FuzzyGameHash != preActionHash) {
				// If the action queue is empty, we have reached a leaf node game state
				// so compare it for equality with other final game states
				if (e.Game.ActionQueue.Queue.Count == 0) {
					t.LeafNodeCount++;
					// This will cause the game to be discarded if its fuzzy hash matches any other final game state
#if _TREE_DEBUG
					var oc = uniqueGames.Count;
#endif
					uniqueGames.Add(e.Game);
#if _TREE_DEBUG
					if (oc < uniqueGames.Count)
						Console.WriteLine("UNIQUE GAME FOUND ({0})", oc + 1);
					else
						Console.WriteLine("DUPLICATE GAME FOUND");
#endif
				}
				else {
					// The game state has changed but there are more actions to do
					// (which may or may not involve further cloning) so add it to the search queue
#if _TREE_DEBUG
					Console.WriteLine("QUEUEING FOR NEXT SEARCH");
#endif
					searchQueue.Add(e.Game);
				}
#if _TREE_DEBUG
				Console.WriteLine("");
#endif
				e.Cancel = true;
			}
		}

		public void PostProcess(GameTree t) {
			// Breadth-first processing loop
			while (searchQueue.Count > 0) {
#if _TREE_DEBUG
				Console.WriteLine("QUEUE SIZE: " + searchQueue.Count);
#endif
				// Copy the search queue and clear the current one; it will be refilled
				var nextQueue = new HashSet<Game>(searchQueue);
				searchQueue.Clear();

				// Process each game's action queue until it is interrupted by OnAction above
				foreach (var g in nextQueue) {
					preActionHash = g.Entities.FuzzyGameHash;
					g.ActionQueue.ProcessAll();
				}
#if _TREE_DEBUG
				Console.WriteLine("=======================");
				Console.WriteLine("CLONES SO FAR: " + t.NodeCount + " / " + t.LeafNodeCount);
				Console.WriteLine("UNIQUE GAMES SO FAR: " + uniqueGames.Count);
				Console.WriteLine("NEW QUEUE SIZE: " + searchQueue.Count + "\r\n");
#endif
			}
		}

		public HashSet<Game> GetUniqueGames() {
			return uniqueGames;
		}
	}
}