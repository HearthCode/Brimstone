//#define _TREE_DEBUG

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Brimstone
{
	public interface ITreeSearcher
	{
		// (Optional) code to execute after each node's ActionQueue is processed or cancelled
		void Visitor(Game cloned, GameTree<GameNode> tree, QueueActionEventArgs e);
		// (Optional) code to execute after each non-cancelled action in a node's ActionQueue completes
		void PostAction(ActionQueue q, GameTree<GameNode> tree, QueueActionEventArgs e);
		// (Optional) code to execute after all nodes have been processed
		// NOTE: This can start a new round of processing if desired (eg. BFS)
		Task PostProcess(GameTree<GameNode> tree);
		// Return all of the unique game states (leaf nodes) found in the search results
		Dictionary<Game, double> GetUniqueGames();
	}

	public class GameNode
	{
		public Game Game { get; }
		public GameNode Parent { get; set; }
		public HashSet<GameNode> Children { get; }

		public GameNode(Game Game, GameNode Parent = null, bool TrackChildren = true) {
			this.Game = Game;
			this.Parent = Parent;
			if (TrackChildren) {
				Children = new HashSet<GameNode>();
				if (Parent != null)
					Parent.AddChild(this);
			}
		}

		public void AddChild(GameNode Child) {
			Child.Parent = this;
			Children.Add(Child);
		}

		public GameNode AddChild(Game Child) {
			// Creating GameNode also calls AddChild
			return new GameNode(Child, this, Children != null);
		}

		public HashSet<GameNode> AddChildren(IEnumerable<Game> Children) {
			var newChildren = new HashSet<GameNode>();
			foreach (var child in Children)
				newChildren.Add(AddChild(child));
			return newChildren;
		}

		public void AddChildren(IEnumerable<GameNode> Children) {
			foreach (var child in Children)
				this.Children.Add(child);
		}

		public GameNode Branch() {
			var clone = Game.GetClone();
			var node = new GameNode(clone, this, Children != null);
			// TODO: Remove
			clone.CustomData = node;
			return node;
		}

		public IEnumerable<GameNode> Branch(int Quantity) {
			var clones = Game.GetClones(Quantity);
			var nodes = clones.Select(c => new GameNode(c, this, Children != null));
			return nodes;
		}
	}

	public class WeightedGameNode : GameNode {
		public double Weight { get; set; }

		public WeightedGameNode(Game Game, GameNode Parent = null, double Weight = 1.0, bool TrackChildren = true)
			: base(Game, Parent, TrackChildren) {
			this.Weight = Weight;
		}

		public WeightedGameNode AddChild(Game Child, double Weight = 1.0) {
			// Creating GameNode also calls AddChild
			return new WeightedGameNode(Child, this, Weight, Children != null);
		}

		public HashSet<GameNode> AddChildren(IEnumerable<Game> Children, double Weight = 1.0) {
			var newChildren = new HashSet<GameNode>();
			foreach (var child in Children)
				newChildren.Add(AddChild(child, Weight));
			return newChildren;
		}

		public IEnumerable<WeightedGameNode> AddChildren(Dictionary<Game, double> Children) {
			// Creating GameNode also calls AddChild
			return Children.Select(kv => new WeightedGameNode(kv.Key, this, kv.Value, Children != null));
		}

		public WeightedGameNode Branch(double Weight = 1.0) {
			var clone = Game.GetClone();
			var node = new WeightedGameNode(clone, this, Weight, Children != null);
			clone.CustomData = node;
			return node;
		}

		public IEnumerable<WeightedGameNode> Branch(int Quantity, double Weight = 1.0) {
			var clones = Game.GetClones(Quantity);
			var nodes = clones.Select(c => new WeightedGameNode(c, this, Weight, Children != null));
			return nodes;
		}
	}

	public class ProbabilisticGameNode : WeightedGameNode {
		public double Probability { get; set; }

		public ProbabilisticGameNode(Game Game, ProbabilisticGameNode Parent = null, double Weight = 1.0, bool TrackChildren = true)
			: base(Game, Parent, Weight, TrackChildren) {

			Probability = (Parent != null ? Parent.Probability * Weight : Weight);
		}

		public ProbabilisticGameNode AddChild(Game Child, double Weight = 1.0) {
			// Creating GameNode also calls AddChild
			return new ProbabilisticGameNode(Child, this, Weight, Children != null);
		}

		public HashSet<GameNode> AddChildren(IEnumerable<Game> Children, double Weight = 1.0) {
			var newChildren = new HashSet<GameNode>();
			foreach (var child in Children)
				newChildren.Add(AddChild(child, Weight));
			return newChildren;
		}

		public IEnumerable<ProbabilisticGameNode> AddChildren(Dictionary<Game, double> Children) {
			// Creating GameNode also calls AddChild
			return Children.Select(kv => new ProbabilisticGameNode(kv.Key, this, kv.Value, Children != null));
		}

		public ProbabilisticGameNode Branch(double Weight = 1.0) {
			var clone = Game.GetClone();
			var node = new ProbabilisticGameNode(clone, this, Weight, Children != null);
			clone.CustomData = node;
			return node;
		}

		public IEnumerable<ProbabilisticGameNode> Branch(int Quantity, double Weight = 1.0) {
			var clones = Game.GetClones(Quantity);
			var nodes = clones.Select(c => new ProbabilisticGameNode(c, this, Weight, Children != null));
			return nodes;
		}
	}

	public class GameTree<TNode> where TNode : GameNode {
		public TNode RootNode { get; }
		public bool TrackChildren { get; set; }

		// The total number of clones including the root node in this tree
		private volatile int _nodeCount = 0;
		public int NodeCount { get { return _nodeCount; } protected set { _nodeCount = value; } }

		// The total number of non-pruned leaf nodes kept
		private volatile int _leafNodeCount = 0;
		public int LeafNodeCount { get { return _leafNodeCount; } set { _leafNodeCount = value; } }

		public GameTree(TNode RootNode) {
			TrackChildren = true;
			this.RootNode = RootNode;
			// TODO: Remove
			RootNode.Game.CustomData = RootNode;
		}
	}

	public static class GameTree
	{
		public static GameTree<GameNode> From(Game Root) {
			return new GameTree<GameNode>(new GameNode(Root));
		}
	}
	
	public class RandomOutcomeSearch : GameTree<GameNode>
	{
		public bool Parallel { get; }

		private ITreeSearcher searcher = null;
		private Dictionary<Game, double> uniqueGames = new Dictionary<Game, double>();

		public RandomOutcomeSearch(Game Root, ITreeSearcher SearchMode = null, bool? Parallel = null)
			: base(new ProbabilisticGameNode(Root, TrackChildren: false)) {
			this.Parallel = Parallel ?? Settings.ParallelTreeSearch;

			if (SearchMode == null)
				SearchMode = new BreadthFirstTreeSearch();

			if (this.Parallel) {
				RootNode.Game.ActionQueue.ReplaceAction<RandomChoice>(replaceRandomChoiceParallel);
				RootNode.Game.ActionQueue.ReplaceAction<RandomAmount>(replaceRandomAmountParallel);
			}
			else {
				RootNode.Game.ActionQueue.ReplaceAction<RandomChoice>(replaceRandomChoice);
				RootNode.Game.ActionQueue.ReplaceAction<RandomAmount>(replaceRandomAmount);
			}

			RootNode.Game.ActionQueue.OnAction += (o, e) => {
				searcher.PostAction(o as ActionQueue, this, e);
			};
			Root.CustomData = RootNode;
			searcher = SearchMode;
		}

		public Dictionary<Game, double> GetUniqueGames() {
			if (uniqueGames.Count > 0)
				return uniqueGames;
			uniqueGames = searcher.GetUniqueGames();
			return uniqueGames;
		}

		public void Run(Action Action) {
			Action();
			searcher.PostProcess(this).Wait();
		}

		public async Task RunAsync(Action Action) {
			Action();
			await searcher.PostProcess(this);
		}

		public static RandomOutcomeSearch Build(Game Game, Action Action, ITreeSearcher SearchMode = null) {
			var tree = new RandomOutcomeSearch(Game, SearchMode);
			tree.Run(Action);
			return tree;
		}

		public static async Task<RandomOutcomeSearch> BuildAsync(Game Game, Action Action, ITreeSearcher SearchMode = null) {
			var tree = new RandomOutcomeSearch(Game, SearchMode);
			await tree.RunAsync(Action);
			return tree;
		}

		protected Task replaceRandomChoice(ActionQueue q, QueueActionEventArgs e) {
			// Choosing a random entity (minion in this case)
			// Clone and start processing for every possibility
#if _TREE_DEBUG
			DebugLog.WriteLine("");
			DebugLog.WriteLine("--> Depth: " + e.Game.Depth);
#endif
			double perItemWeight = 1.0 / e.Args[RandomChoice.ENTITIES].Count();
			foreach (Entity entity in e.Args[RandomChoice.ENTITIES]) {
				// When cloning occurs, RandomChoice has been pulled from the action queue,
				// so we can just insert a fixed item at the start of the queue and restart the queue
				// to effectively replace it
				var cloned = ((ProbabilisticGameNode)e.Game.CustomData).Branch(perItemWeight).Game;
				NodeCount++;
				cloned.ActionQueue.InsertDeferred(e.Source, entity);
				cloned.ActionQueue.ProcessAll();
				searcher.Visitor(cloned, this, e);
			}
#if _TREE_DEBUG
			DebugLog.WriteLine("<-- Depth: " + e.Game.Depth);
			DebugLog.WriteLine("");
#endif
			return Task.FromResult(0);
		}

		protected Task replaceRandomAmount(ActionQueue q, QueueActionEventArgs e) {
			// Choosing a random value (damage amount in this case)
			// Clone and start processing for every possibility
#if _TREE_DEBUG
			DebugLog.WriteLine("");
			DebugLog.WriteLine("--> Depth: " + e.Game.Depth);
#endif
			double perItemWeight = 1.0 / ((e.Args[RandomAmount.MAX] - e.Args[RandomAmount.MIN]) + 1);
			for (int i = e.Args[RandomAmount.MIN]; i <= e.Args[RandomAmount.MAX]; i++) {
				// When cloning occurs, RandomAmount has been pulled from the action queue,
				// so we can just insert a fixed number at the start of the queue and restart the queue
				// to effectively replace it
				var cloned = ((ProbabilisticGameNode)e.Game.CustomData).Branch(perItemWeight).Game;
				NodeCount++;
				cloned.ActionQueue.InsertDeferred(e.Source, i);
				cloned.ActionQueue.ProcessAll();
				searcher.Visitor(cloned, this, e);
			}
#if _TREE_DEBUG
			DebugLog.WriteLine("<-- Depth: " + e.Game.Depth);
			DebugLog.WriteLine("");
#endif
			return Task.FromResult(0);
		}

		protected async Task replaceRandomChoiceParallel(ActionQueue q, QueueActionEventArgs e) {
			// Choosing a random entity (minion in this case)
			// Clone and start processing for every possibility
#if _TREE_DEBUG
			DebugLog.WriteLine("");
			DebugLog.WriteLine("--> Depth: " + e.Game.Depth);
#endif
			double perItemWeight = 1.0 / e.Args[RandomChoice.ENTITIES].Count();
			await Task.WhenAll(
				e.Args[RandomChoice.ENTITIES].Select(entity =>
					Task.Run(async () => {
					// When cloning occurs, RandomChoice has been pulled from the action queue,
					// so we can just insert a fixed item at the start of the queue and restart the queue
					// to effectively replace it
					var cloned = ((ProbabilisticGameNode)e.Game.CustomData).Branch(perItemWeight).Game;
						NodeCount++;
						cloned.ActionQueue.InsertDeferred(e.Source, (Entity)entity);
						await cloned.ActionQueue.ProcessAllAsync();
						searcher.Visitor(cloned, this, e);
					})
				)
			);
#if _TREE_DEBUG
			DebugLog.WriteLine("<-- Depth: " + e.Game.Depth);
			DebugLog.WriteLine("");
#endif
		}

		protected async Task replaceRandomAmountParallel(ActionQueue q, QueueActionEventArgs e) {
			// Choosing a random value (damage amount in this case)
			// Clone and start processing for every possibility
#if _TREE_DEBUG
			DebugLog.WriteLine("");
			DebugLog.WriteLine("--> Depth: " + e.Game.Depth);
#endif
			double perItemWeight = 1.0 / ((e.Args[RandomAmount.MAX] - e.Args[RandomAmount.MIN]) + 1);
			await Task.WhenAll(
				Enumerable.Range(e.Args[RandomAmount.MIN], (e.Args[RandomAmount.MAX] - e.Args[RandomAmount.MIN]) + 1).Select(i =>
					Task.Run(async () => {
						// When cloning occurs, RandomAmount has been pulled from the action queue,
						// so we can just insert a fixed number at the start of the queue and restart the queue
						// to effectively replace it
						var cloned = ((ProbabilisticGameNode)e.Game.CustomData).Branch(perItemWeight).Game;
						NodeCount++;
						cloned.ActionQueue.InsertDeferred(e.Source, i);
						await cloned.ActionQueue.ProcessAllAsync();
						searcher.Visitor(cloned, this, e);
					})
				)
			);
#if _TREE_DEBUG
			DebugLog.WriteLine("<-- Depth: " + e.Game.Depth);
			DebugLog.WriteLine("");
#endif
		}
	}

	public abstract class TreeSearch : ITreeSearcher
	{
		public GameTree<GameNode> Tree { get; set; }

		public abstract Dictionary<Game, double> GetUniqueGames();
		public virtual void PostAction(ActionQueue q, GameTree<GameNode> tree, QueueActionEventArgs e) { }
		public virtual Task PostProcess(GameTree<GameNode> tree) { return Task.FromResult(0); }
		public virtual void Visitor(Game cloned, GameTree<GameNode> tree, QueueActionEventArgs e) { }
	}

	public class NaiveTreeSearch : TreeSearch
	{
		private HashSet<Game> leafNodeGames = new HashSet<Game>();

		public override void Visitor(Game cloned, GameTree<GameNode> tree, QueueActionEventArgs e) {
			// If the action queue is empty, we have reached a leaf node game state
			// TODO: Optimize to use TLS and avoid spinlocks
			if (cloned.ActionQueue.Queue.Count == 0) {
				tree.LeafNodeCount++;
				lock (leafNodeGames) {
					leafNodeGames.Add(cloned);
				}
			}
		}

		public override Dictionary<Game, double> GetUniqueGames() {
			var uniqueGames = new Dictionary<Game, double>();

			// TODO: Parallelize
			while (leafNodeGames.Count > 0) {
				var root = leafNodeGames.Take(1).ToList()[0];
				leafNodeGames.Remove(root);
				uniqueGames.Add(root, ((ProbabilisticGameNode)root.CustomData).Probability);
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
									DebugLog.WriteLine(collisions[e.FuzzyHash].ToString());
									DebugLog.WriteLine(e.ToString());
									DebugLog.WriteLine(collisions[e.FuzzyHash].FuzzyHash + " " + e.FuzzyHash);
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
						uniqueGames[root] += ((ProbabilisticGameNode)g.CustomData).Probability;
					}
				}
				leafNodeGames = different;
#if _TREE_DEBUG
				DebugLog.WriteLine("{0} games remaining to process ({1} unique games found so far)", different.Count, uniqueGames.Count);
#endif
			}
			return uniqueGames;
		}
	}
	
	public class DepthFirstTreeSearch : TreeSearch
	{
		private Dictionary<Game, double> uniqueGames = new Dictionary<Game, double>(new FuzzyGameComparer());

		public override void Visitor(Game cloned, GameTree<GameNode> tree, QueueActionEventArgs e) {
			// If the action queue is empty, we have reached a leaf node game state
			// so compare it for equality with other final game states
			if (cloned.ActionQueue.Queue.Count == 0)
				if (!cloned.EquivalentTo(e.Game)) {
					tree.LeafNodeCount++;
					// This will cause the game to be discarded if its fuzzy hash matches any other final game state
					// TODO: Optimize to use TLS and avoid spinlocks
					lock (uniqueGames) {
						if (!uniqueGames.ContainsKey(cloned)) {
							uniqueGames.Add(cloned, ((ProbabilisticGameNode)cloned.CustomData).Probability);
#if _TREE_DEBUG
							DebugLog.WriteLine("UNIQUE GAME FOUND ({0})", uniqueGames.Count);
#endif
						}
						else {
							uniqueGames[cloned] += ((ProbabilisticGameNode)cloned.CustomData).Probability;
#if _TREE_DEBUG
							DebugLog.WriteLine("DUPLICATE GAME FOUND");
#endif
						}
					}
				}
		}

		public override Dictionary<Game, double> GetUniqueGames() {
			return uniqueGames;
		}
	}

	public class BreadthFirstTreeSearch : TreeSearch
	{
		// The maximum number of task threads to split the search queue up into
		public int MaxDegreesOfParallelism { get; set; } = 5;

		// The minimum number of game nodes that have to be in the queue in order to activate parallelization
		// for a particular depth
		public int MinNodesToParallelize { get; set; } = 100;

		// All of the unique leaf node games found
		private Dictionary<Game, double> uniqueGames = new Dictionary<Game, double>(new FuzzyGameComparer());

		// The pruned search queue for the current search depth
		private Dictionary<Game, double> searchQueue = new Dictionary<Game, double>(new FuzzyGameComparer());

		// Per-thread storage for partitioned search queue and unique games found
		// (merged with the main lists after each depth is searched)
		private ThreadLocal<Dictionary<Game, double>> tlsSearchQueue = new ThreadLocal<Dictionary<Game, double>>(() => new Dictionary<Game, double>(new FuzzyGameComparer()), trackAllValues: true);
		private ThreadLocal<Dictionary<Game, double>> tlsUniqueGames = new ThreadLocal<Dictionary<Game, double>>(() => new Dictionary<Game, double>(new FuzzyGameComparer()), trackAllValues: true);

		// When an in-game action completes, check if the game state has changed
		// Some actions (like selectors) won't cause the game state to change,
		// so we continue running these until a game state change occurs
		public override void PostAction(ActionQueue q, GameTree<GameNode> t, QueueActionEventArgs e) {
			// This game will be on the same thread as the calling task in parallel mode if it hasn't been cloned
			// If it has been cloned, it may be on a different thread
			if (e.Game.Entities.Changed) {
				// If the action queue is empty, we have reached a leaf node game state
				// so compare it for equality with other final game states
				if (e.Game.ActionQueue.Queue.Count == 0) {
					t.LeafNodeCount++;
					// This will cause the game to be discarded if its fuzzy hash matches any other final game state
					if (!tlsUniqueGames.Value.ContainsKey(e.Game)) {
						tlsUniqueGames.Value.Add(e.Game, ((ProbabilisticGameNode)e.Game.CustomData).Probability);
#if _TREE_DEBUG
						DebugLog.WriteLine("UNIQUE GAME FOUND ({0})", uniqueGames.Count);
#endif
					}
					else {
						tlsUniqueGames.Value[e.Game] += ((ProbabilisticGameNode)e.Game.CustomData).Probability;
#if _TREE_DEBUG
						DebugLog.WriteLine("DUPLICATE GAME FOUND");
#endif
					}
				}
				else {
					// The game state has changed but there are more actions to do
					// (which may or may not involve further cloning) so add it to the search queue
#if _TREE_DEBUG
					DebugLog.WriteLine("QUEUEING FOR NEXT SEARCH");
#endif
					if (!tlsSearchQueue.Value.ContainsKey(e.Game))
						tlsSearchQueue.Value.Add(e.Game, ((ProbabilisticGameNode)e.Game.CustomData).Probability);
					else
						tlsSearchQueue.Value[e.Game] += ((ProbabilisticGameNode)e.Game.CustomData).Probability;
				}
#if _TREE_DEBUG
				DebugLog.WriteLine("");
#endif
				e.Cancel = true;
			}
		}

		// This is the entry point after the root node has been pushed into the queue
		// and the first change to the game has occurred
		public override async Task PostProcess(GameTree<GameNode> t) {
			// Breadth-first processing loop
			do {
				// Merge the TLS lists into the main lists
				foreach (var sq in tlsSearchQueue.Values) {
					foreach (var qi in sq) {
						if (!searchQueue.ContainsKey(qi.Key))
							searchQueue.Add(qi.Key, qi.Value);
						else
							searchQueue[qi.Key] += qi.Value;
					}
				}
				foreach (var ug in tlsUniqueGames.Values) {
					foreach (var qi in ug) {
						if (!uniqueGames.ContainsKey(qi.Key))
							uniqueGames.Add(qi.Key, qi.Value);
						else
							uniqueGames[qi.Key] += qi.Value;
					}
				}
#if _TREE_DEBUG
				DebugLog.WriteLine("QUEUE SIZE: " + searchQueue.Count);
#endif
				// Wipe the TLS lists
				tlsSearchQueue = new ThreadLocal<Dictionary<Game, double>>(() => new Dictionary<Game, double>(new FuzzyGameComparer()), trackAllValues: true);
				tlsUniqueGames = new ThreadLocal<Dictionary<Game, double>>(() => new Dictionary<Game, double>(new FuzzyGameComparer()), trackAllValues: true);

				// Quit if we have processed all nodes and none of them have children (all leaf nodes)
				if (searchQueue.Count == 0)
					break;

				// Copy the search queue and clear the current one; it will be refilled
				var nextQueue = new Dictionary<Game, double>(searchQueue);
				searchQueue.Clear();

				// Only parallelize if there are sufficient nodes to do so
				if (nextQueue.Count >= MinNodesToParallelize && ((RandomOutcomeSearch)t).Parallel) {
					// Process each game's action queue until it is interrupted by OnAction above
					await Task.WhenAll(
						// Split search queue into MaxDegreesOfParallelism partitions
						from partition in Partitioner.Create(nextQueue).GetPartitions(MaxDegreesOfParallelism)
						// Run each partition in its own task
						select Task.Run(async delegate {
#if _TREE_DEBUG
							var count = 0;
							DebugLog.WriteLine("Start partition run");
#endif
							using (partition)
								while (partition.MoveNext()) {
									// Process each node
									var kv = partition.Current;
									((ProbabilisticGameNode)kv.Key.CustomData).Probability = kv.Value;
									await kv.Key.ActionQueue.ProcessAllAsync();
#if _TREE_DEBUG
									count++;
#endif
								}
#if _TREE_DEBUG
							DebugLog.WriteLine("End run with partition size {0}", count);
#endif
						}));
#if _TREE_DEBUG
				DebugLog.WriteLine("=======================");
				DebugLog.WriteLine("CLONES SO FAR: " + t.NodeCount + " / " + t.LeafNodeCount);
				DebugLog.WriteLine("UNIQUE GAMES SO FAR: " + uniqueGames.Count);
				DebugLog.WriteLine("NEW QUEUE SIZE: " + searchQueue.Count + "\r\n");
#endif
				}
				else {
#if _TREE_DEBUG
					DebugLog.WriteLine("Start single-threaded run");
#endif
					// Process each node in the search queue sequentially
					foreach (var kv in nextQueue) {
						((ProbabilisticGameNode)kv.Key.CustomData).Probability = kv.Value;
						await kv.Key.ActionQueue.ProcessAllAsync();
					}
#if _TREE_DEBUG
					DebugLog.WriteLine("End single-threaded run");
#endif
				}
			} while (true);
		}

		public override Dictionary<Game, double> GetUniqueGames() {
			return uniqueGames;
		}
	}
}