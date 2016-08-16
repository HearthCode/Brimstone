//#define _TREE_DEBUG

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Brimstone
{
	public interface ITreeSearcher
	{
		void Visitor(Game cloned, GameTree tree, QueueActionEventArgs e);
		void PostAction(ActionQueue q, GameTree tree, QueueActionEventArgs e);
		void PostProcess(GameTree tree);
		Dictionary<Game, double> GetUniqueGames();
	}

	public class GameNode
	{
		public Game Game { get; }
		public GameNode Parent { get; }
		public HashSet<GameNode> Children { get; }
		public double Weight { get; set; }
		public double Probability { get; set; }

		public GameNode(Game Game, GameNode Parent = null, double Weight = 1.0, bool TrackChildren = true) {
			this.Game = Game;
			this.Parent = Parent;
			this.Weight = Weight;
			Probability = (Parent != null ? Parent.Probability * Weight : Weight);
			if (TrackChildren) {
				Children = new HashSet<GameNode>();
				if (Parent != null)
					Parent.AddChild(this);
			}
		}

		public void AddChild(GameNode child) {
			Children.Add(child);
		}

		public GameNode Branch(double Weight = 1.0) {
			var clone = Game.CloneState() as Game;
			var node = new GameNode(clone, this, Weight, Children != null);
			clone.CustomData = node;
			return node;
		}
	}

	public class GameTree
	{
		public GameNode RootNode { get; }
		public bool TrackChildren { get; set; }

		// The total number of clones including the root node in this tree
		private volatile int _nodeCount = 0;
		public int NodeCount { get { return _nodeCount; } protected set { _nodeCount = value; } }

		// The total number of non-pruned leaf nodes kept
		private volatile int _leafNodeCount = 0;
		public int LeafNodeCount { get { return _leafNodeCount; } set { _leafNodeCount = value; } }

		private ITreeSearcher searcher = null;
		private Dictionary<Game, double> uniqueGames = new Dictionary<Game, double>();

		public Dictionary<Game, double> GetUniqueGames() {
			if (uniqueGames.Count > 0)
				return uniqueGames;
			uniqueGames = searcher.GetUniqueGames();
			return uniqueGames;
		}

		public GameTree(Game Root, ITreeSearcher SearchMode = null, bool CloneRoot = false, bool? Parallel = null) {
			var rootGame = CloneRoot ? Root.CloneState() as Game : Root;
			var parallel = Parallel ?? Settings.ParallelTreeSearch;

			TrackChildren = (SearchMode == null);
			RootNode = new GameNode(Game: rootGame, TrackChildren: TrackChildren);

			if (SearchMode != null) {
				if (parallel) {
					RootNode.Game.ActionQueue.ReplaceAction<RandomChoice>(replaceRandomChoiceParallel);
					RootNode.Game.ActionQueue.ReplaceAction<RandomAmount>(replaceRandomAmountParallel);
				} else {
					RootNode.Game.ActionQueue.ReplaceAction<RandomChoice>(replaceRandomChoice);
					RootNode.Game.ActionQueue.ReplaceAction<RandomAmount>(replaceRandomAmount);
				}
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

		protected Task replaceRandomChoice(ActionQueue q, QueueActionEventArgs e) {
			// Choosing a random entity (minion in this case)
			// Clone and start processing for every possibility
#if _TREE_DEBUG
			Console.WriteLine("");
			Console.WriteLine("--> Depth: " + e.Game.Depth);
#endif
			double perItemWeight = 1.0 / e.Args[RandomChoice.ENTITIES].Count();
			foreach (Entity entity in e.Args[RandomChoice.ENTITIES]) {
				// When cloning occurs, RandomChoice has been pulled from the action queue,
				// so we can just insert a fixed item at the start of the queue and restart the queue
				// to effectively replace it
				var cloned = ((GameNode)e.Game.CustomData).Branch(perItemWeight).Game;
				NodeCount++;
				cloned.ActionQueue.InsertDeferred(e.Source, entity);
				cloned.ActionQueue.ProcessAll();
				searcher.Visitor(cloned, this, e);
			}
#if _TREE_DEBUG
			Console.WriteLine("<-- Depth: " + e.Game.Depth);
			Console.WriteLine("");
#endif
			return Task.FromResult(0);
		}

		protected Task replaceRandomAmount(ActionQueue q, QueueActionEventArgs e) {
			// Choosing a random value (damage amount in this case)
			// Clone and start processing for every possibility
#if _TREE_DEBUG
			Console.WriteLine("");
			Console.WriteLine("--> Depth: " + e.Game.Depth);
#endif
			double perItemWeight = 1.0 / ((e.Args[RandomAmount.MAX] - e.Args[RandomAmount.MIN]) + 1);
			for (int i = e.Args[RandomAmount.MIN]; i <= e.Args[RandomAmount.MAX]; i++) {
				// When cloning occurs, RandomAmount has been pulled from the action queue,
				// so we can just insert a fixed number at the start of the queue and restart the queue
				// to effectively replace it
				var cloned = ((GameNode)e.Game.CustomData).Branch(perItemWeight).Game;
				NodeCount++;
				cloned.ActionQueue.InsertDeferred(e.Source, i);
				cloned.ActionQueue.ProcessAll();
				searcher.Visitor(cloned, this, e);
			}
#if _TREE_DEBUG
			Console.WriteLine("<-- Depth: " + e.Game.Depth);
			Console.WriteLine("");
#endif
			return Task.FromResult(0);
		}

		protected async Task replaceRandomChoiceParallel(ActionQueue q, QueueActionEventArgs e) {
			// Choosing a random entity (minion in this case)
			// Clone and start processing for every possibility
#if _TREE_DEBUG
			Console.WriteLine("");
			Console.WriteLine("--> Depth: " + e.Game.Depth);
#endif
			double perItemWeight = 1.0 / e.Args[RandomChoice.ENTITIES].Count();
			await Task.WhenAll(
				e.Args[RandomChoice.ENTITIES].Select(entity =>
					Task.Run(async () => {
					// When cloning occurs, RandomChoice has been pulled from the action queue,
					// so we can just insert a fixed item at the start of the queue and restart the queue
					// to effectively replace it
					var cloned = ((GameNode)e.Game.CustomData).Branch(perItemWeight).Game;
						NodeCount++;
						cloned.ActionQueue.InsertDeferred(e.Source, (Entity)entity);
						await cloned.ActionQueue.ProcessAllAsync();
						searcher.Visitor(cloned, this, e);
					})
				)
			);
#if _TREE_DEBUG
			Console.WriteLine("<-- Depth: " + e.Game.Depth);
			Console.WriteLine("");
#endif
		}

		protected async Task replaceRandomAmountParallel(ActionQueue q, QueueActionEventArgs e) {
			// Choosing a random value (damage amount in this case)
			// Clone and start processing for every possibility
#if _TREE_DEBUG
			Console.WriteLine("");
			Console.WriteLine("--> Depth: " + e.Game.Depth);
#endif
			double perItemWeight = 1.0 / ((e.Args[RandomAmount.MAX] - e.Args[RandomAmount.MIN]) + 1);
			await Task.WhenAll(
				Enumerable.Range(e.Args[RandomAmount.MIN], (e.Args[RandomAmount.MAX] - e.Args[RandomAmount.MIN]) + 1).Select(i =>
					Task.Run(async () => {
						// When cloning occurs, RandomAmount has been pulled from the action queue,
						// so we can just insert a fixed number at the start of the queue and restart the queue
						// to effectively replace it
						var cloned = ((GameNode)e.Game.CustomData).Branch(perItemWeight).Game;
						NodeCount++;
						cloned.ActionQueue.InsertDeferred(e.Source, i);
						await cloned.ActionQueue.ProcessAllAsync();
						searcher.Visitor(cloned, this, e);
					})
				)
			);
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
				lock (leafNodeGames) {
					leafNodeGames.Add(cloned);
				}
			}
		}

		public void PostAction(ActionQueue q, GameTree t, QueueActionEventArgs e) {	}

		public void PostProcess(GameTree t) { }

		public Dictionary<Game, double> GetUniqueGames() {
			var uniqueGames = new Dictionary<Game, double>();

			while (leafNodeGames.Count > 0) {
				var root = leafNodeGames.Take(1).ToList()[0];
				leafNodeGames.Remove(root);
				uniqueGames.Add(root, ((GameNode)root.CustomData).Probability);
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
					} else {
						uniqueGames[root] += ((GameNode)g.CustomData).Probability;
					}
				}
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
		private Dictionary<Game, double> uniqueGames = new Dictionary<Game, double>(new FuzzyGameComparer());

		public void Visitor(Game cloned, GameTree tree, QueueActionEventArgs e) {
			// If the action queue is empty, we have reached a leaf node game state
			// so compare it for equality with other final game states
			if (cloned.ActionQueue.Queue.Count == 0)
				if (!cloned.EquivalentTo(e.Game)) {
					tree.LeafNodeCount++;
					// This will cause the game to be discarded if its fuzzy hash matches any other final game state
					lock (uniqueGames) {
						if (!uniqueGames.ContainsKey(cloned)) {
							uniqueGames.Add(cloned, ((GameNode)cloned.CustomData).Probability);
#if _TREE_DEBUG
							Console.WriteLine("UNIQUE GAME FOUND ({0})", uniqueGames.Count);
#endif
						}
						else {
							uniqueGames[cloned] += ((GameNode)cloned.CustomData).Probability;
#if _TREE_DEBUG
							Console.WriteLine("DUPLICATE GAME FOUND");
#endif
						}
					}
				}
		}

		public void PostAction(ActionQueue q, GameTree t, QueueActionEventArgs e) { }

		public void PostProcess(GameTree t) { }

		public Dictionary<Game, double> GetUniqueGames() {
			return uniqueGames;
		}
	}

	public class BreadthFirstTreeSearch : ITreeSearcher
	{
		private Dictionary<Game, double> uniqueGames = new Dictionary<Game, double>(new FuzzyGameComparer());

		// The pruned search queue for the current search depth
		private Dictionary<Game, double> searchQueue = new Dictionary<Game, double>(new FuzzyGameComparer());

		public void Visitor(Game cloned, GameTree tree, QueueActionEventArgs e) { }

		// When an in-game action completes, check if the game state has changed
		// Some actions (like selectors) won't cause the game state to change,
		// so we continue running these until a game state change occurs
		public void PostAction(ActionQueue q, GameTree t, QueueActionEventArgs e) {
			if (e.Game.Entities.Changed) {
				// If the action queue is empty, we have reached a leaf node game state
				// so compare it for equality with other final game states
				if (e.Game.ActionQueue.Queue.Count == 0) {
					t.LeafNodeCount++;
					// This will cause the game to be discarded if its fuzzy hash matches any other final game state
					lock (uniqueGames) {
						if (!uniqueGames.ContainsKey(e.Game)) {
							uniqueGames.Add(e.Game, ((GameNode)e.Game.CustomData).Probability);
#if _TREE_DEBUG
						Console.WriteLine("UNIQUE GAME FOUND ({0})", uniqueGames.Count);
#endif
						}
						else {
							uniqueGames[e.Game] += ((GameNode)e.Game.CustomData).Probability;
#if _TREE_DEBUG
						Console.WriteLine("DUPLICATE GAME FOUND");
#endif
						}
					}
				}
				else {
					// The game state has changed but there are more actions to do
					// (which may or may not involve further cloning) so add it to the search queue
#if _TREE_DEBUG
					Console.WriteLine("QUEUEING FOR NEXT SEARCH");
#endif
					lock (searchQueue) {
						if (!searchQueue.ContainsKey(e.Game))
							searchQueue.Add(e.Game, ((GameNode)e.Game.CustomData).Probability);
						else
							searchQueue[e.Game] += ((GameNode)e.Game.CustomData).Probability;
					}
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
				var nextQueue = new Dictionary<Game, double>(searchQueue);
				searchQueue.Clear();

				// Process each game's action queue until it is interrupted by OnAction above
				foreach (var kv in nextQueue) {
					((GameNode)kv.Key.CustomData).Probability = kv.Value;
					kv.Key.ActionQueue.ProcessAll();
				}
#if _TREE_DEBUG
				Console.WriteLine("=======================");
				Console.WriteLine("CLONES SO FAR: " + t.NodeCount + " / " + t.LeafNodeCount);
				Console.WriteLine("UNIQUE GAMES SO FAR: " + uniqueGames.Count);
				Console.WriteLine("NEW QUEUE SIZE: " + searchQueue.Count + "\r\n");
#endif
			}
		}

		public Dictionary<Game, double> GetUniqueGames() {
			return uniqueGames;
		}
	}
}