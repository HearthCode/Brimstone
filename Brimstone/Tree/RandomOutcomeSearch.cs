using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Brimstone.QueueActions;
using Brimstone.Entities;
using Brimstone.Exceptions;

namespace Brimstone.Tree
{
	public interface ITreeActionWalker
	{
		// (Optional) code to execute after each node's ActionQueue is processed or cancelled
		void Visitor(ProbabilisticGameNode cloned, GameTree<GameNode> tree, QueueActionEventArgs e);
		// (Optional) code to execute after each non-cancelled action in a node's ActionQueue completes
		void PostAction(ActionQueue q, GameTree<GameNode> tree, QueueActionEventArgs e);
		// (Optional) code to execute after all nodes have been processed
		// NOTE: This can start a new round of processing if desired (eg. BFS)
		Task PostProcess(GameTree<GameNode> tree);
		// Return all of the unique game states (leaf nodes) found in the search results
		Dictionary<Game, double> GetUniqueGames();
	}

	public class RandomOutcomeSearch : GameTree<GameNode>
	{
		public bool Parallel { get; }

		private ITreeActionWalker searcher = null;
		private Dictionary<Game, double> uniqueGames = new Dictionary<Game, double>();

		public RandomOutcomeSearch(Game Root, ITreeActionWalker SearchMode = null, bool? Parallel = null)
			: base(new ProbabilisticGameNode(Root, TrackChildren: false)) {
			this.Parallel = Parallel ?? Settings.ParallelTreeSearch;

			if (SearchMode == null)
				SearchMode = new BreadthFirstActionWalker();

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
			searcher = SearchMode;
		}

		public Dictionary<Game, double> GetUniqueGames() {
			if (uniqueGames.Count > 0)
				return uniqueGames;
			uniqueGames = searcher.GetUniqueGames();
			return uniqueGames;
		}

		public void Run(Action Action) {
			RootNode.Game.ActionQueue.UserData = RootNode;
			Action();
			searcher.PostProcess(this).Wait();
		}

		public async Task RunAsync(Action Action) {
			RootNode.Game.ActionQueue.UserData = RootNode;
			await Task.Run(() => Action());
			await searcher.PostProcess(this);
		}

		public static RandomOutcomeSearch Build(Game Game, Action Action, ITreeActionWalker SearchMode = null) {
			var tree = new RandomOutcomeSearch(Game, SearchMode);
			tree.Run(Action);
			return tree;
		}

		public static async Task<RandomOutcomeSearch> BuildAsync(Game Game, Action Action, ITreeActionWalker SearchMode = null) {
			var tree = new RandomOutcomeSearch(Game, SearchMode);
			await tree.RunAsync(Action);
			return tree;
		}

		public static Dictionary<Game, double> Find(Game Game, Action Action, ITreeActionWalker SearchMode = null) {
			return Build(Game, Action, SearchMode).GetUniqueGames();
		}

		public static async Task<Dictionary<Game, double>> FindAsync(Game Game, Action Action, ITreeActionWalker SearchMode = null) {
			var tree = await BuildAsync(Game, Action, SearchMode);
			return tree.GetUniqueGames();
		}

		/* TODO: We can remove a lot of unnecessary cloning where we currently have this
		   "double-branching" effect of random choice A then random choice B then some game state change.
		   Find a way to only clone the merged aggregate of such multiple random choices */
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
				var clonedNode = ((ProbabilisticGameNode)e.UserData).Branch(perItemWeight);
				NodeCount++;
				clonedNode.Game.ActionQueue.StackPush((Entity)clonedNode.Game.Entities[entity.Id]);
				clonedNode.Game.ActionQueue.ProcessAll(clonedNode);
				searcher.Visitor(clonedNode, this, e);
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
				var clonedNode = ((ProbabilisticGameNode)e.UserData).Branch(perItemWeight);
				NodeCount++;
				clonedNode.Game.ActionQueue.StackPush(i);
				clonedNode.Game.ActionQueue.ProcessAll(clonedNode);
				searcher.Visitor(clonedNode, this, e);
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
						var clonedNode = ((ProbabilisticGameNode)e.UserData).Branch(perItemWeight);
						NodeCount++;
						clonedNode.Game.ActionQueue.StackPush((Entity) clonedNode.Game.Entities[entity.Id]);
						await clonedNode.Game.ActionQueue.ProcessAllAsync(clonedNode);
						searcher.Visitor(clonedNode, this, e);
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
						var clonedNode = ((ProbabilisticGameNode)e.UserData).Branch(perItemWeight);
						NodeCount++;
						clonedNode.Game.ActionQueue.StackPush(i);
						await clonedNode.Game.ActionQueue.ProcessAllAsync(clonedNode);
						searcher.Visitor(clonedNode, this, e);
					})
				)
			);
#if _TREE_DEBUG
			DebugLog.WriteLine("<-- Depth: " + e.Game.Depth);
			DebugLog.WriteLine("");
#endif
		}
	}

	public abstract class TreeActionWalker : ITreeActionWalker
	{
		public GameTree<GameNode> Tree { get; set; }

		public abstract Dictionary<Game, double> GetUniqueGames();
		public virtual void PostAction(ActionQueue q, GameTree<GameNode> tree, QueueActionEventArgs e) { }
		public virtual Task PostProcess(GameTree<GameNode> tree) { return Task.FromResult(0); }
		public virtual void Visitor(ProbabilisticGameNode cloned, GameTree<GameNode> tree, QueueActionEventArgs e) { }
	}

	public class NaiveActionWalker : TreeActionWalker
	{
		private HashSet<ProbabilisticGameNode> leafNodeGames = new HashSet<ProbabilisticGameNode>();

		public override void Visitor(ProbabilisticGameNode cloned, GameTree<GameNode> tree, QueueActionEventArgs e) {
			// If the action queue is empty, we have reached a leaf node game state
			// TODO: Optimize to use TLS and avoid spinlocks
			if (cloned.Game.ActionQueue.IsEmpty) {
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
				uniqueGames.Add(root.Game, root.Probability);
				var different = new HashSet<ProbabilisticGameNode>();

				// Hash every entity
				// WARNING: This relies on a good hash function!
				var e1 = new HashSet<IEntity>(root.Game.Entities, new FuzzyEntityComparer());

				foreach (var n in leafNodeGames) {
					if (n.Game.Entities.Count != root.Game.Entities.Count) {
						different.Add(n);
						continue;
					}
					var e2 = new HashSet<IEntity>(n.Game.Entities, new FuzzyEntityComparer());
#if _TREE_DEBUG
					if (e2.Count < n.Game.Entities.Count || e1.Count < root.Game.Entities.Count) {
						// Potential hash collision
						var c = (e2.Count < n.Game.Entities.Count ? e2 : e1);
						var g2 = (c == e2 ? n.Game : root.Game);
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
									throw new TreeSearchException("Hash collision - not safe to compare games");
								}
							}
							else
								collisions.Add(e.FuzzyHash, e);
						}
					}
#endif
					if (!e2.SetEquals(e1)) {
						different.Add(n);
					}
					else {
						uniqueGames[root.Game] += root.Probability;
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

	public class DepthFirstActionWalker : TreeActionWalker
	{
		private Dictionary<Game, double> uniqueGames = new Dictionary<Game, double>(new FuzzyGameComparer());

		public override void Visitor(ProbabilisticGameNode cloned, GameTree<GameNode> tree, QueueActionEventArgs e) {
			// If the action queue is empty, we have reached a leaf node game state
			// so compare it for equality with other final game states
			if (cloned.Game.ActionQueue.IsEmpty)
				if (!cloned.Game.EquivalentTo(e.Game)) {
					tree.LeafNodeCount++;
					// This will cause the game to be discarded if its fuzzy hash matches any other final game state
					// TODO: Optimize to use TLS and avoid spinlocks
					lock (uniqueGames) {
						if (!uniqueGames.ContainsKey(cloned.Game)) {
							uniqueGames.Add(cloned.Game, cloned.Probability);
#if _TREE_DEBUG
							DebugLog.WriteLine("UNIQUE GAME FOUND ({0}) - Hash: {1:x8}", uniqueGames.Count, cloned.Game.FuzzyGameHash);
							DebugLog.WriteLine("{0:S}", cloned.Game);
#endif
						}
						else {
							uniqueGames[cloned.Game] += cloned.Probability;
#if _TREE_DEBUG
							DebugLog.WriteLine("DUPLICATE GAME FOUND - Hash: {0:x8}", cloned.Game.FuzzyGameHash);
#endif
						}
					}
				}
		}

		public override Dictionary<Game, double> GetUniqueGames() {
			return uniqueGames;
		}
	}

	public class BreadthFirstActionWalker : TreeActionWalker
	{
		// The maximum number of task threads to split the search queue up into
		public int MaxDegreesOfParallelism { get; set; } = System.Environment.ProcessorCount;

		// The minimum number of game nodes that have to be in the queue in order to activate parallelization
		// for a particular depth
		public int MinNodesToParallelize { get; set; } = 100;

		// All of the unique leaf node games found
		private Dictionary<Game, ProbabilisticGameNode> uniqueGames = new Dictionary<Game, ProbabilisticGameNode>(new FuzzyGameComparer());

		// The pruned search queue for the current search depth
		private Dictionary<Game, ProbabilisticGameNode> searchQueue = new Dictionary<Game, ProbabilisticGameNode>(new FuzzyGameComparer());

		// Per-thread storage for partitioned search queue and unique games found
		// (merged with the main lists after each depth is searched)
		private ThreadLocal<Dictionary<Game, ProbabilisticGameNode>> tlsSearchQueue
			= new ThreadLocal<Dictionary<Game, ProbabilisticGameNode>>(
				() => new Dictionary<Game, ProbabilisticGameNode>(new FuzzyGameComparer()), trackAllValues: true);
		private ThreadLocal<Dictionary<Game, ProbabilisticGameNode>> tlsUniqueGames
			= new ThreadLocal<Dictionary<Game, ProbabilisticGameNode>>(
				() => new Dictionary<Game, ProbabilisticGameNode>(new FuzzyGameComparer()), trackAllValues: true);

		// When an in-game action completes, check if the game state has changed
		// Some actions (like selectors) won't cause the game state to change,
		// so we continue running these until a game state change occurs
		public override void PostAction(ActionQueue q, GameTree<GameNode> t, QueueActionEventArgs e) {
			// This game will be on the same thread as the calling task in parallel mode if it hasn't been cloned
			// If it has been cloned, it may be on a different thread
			if (e.Game.Changed) {
				e.Game.Changed = false;

				// If the action queue is empty, we have reached a leaf node game state
				// so compare it for equality with other final game states
				if (e.Game.ActionQueue.IsEmpty) {
					t.LeafNodeCount++;
					// This will cause the game to be discarded if its fuzzy hash matches any other final game state
					if (!tlsUniqueGames.Value.ContainsKey(e.Game)) {
						tlsUniqueGames.Value.Add(e.Game, e.UserData as ProbabilisticGameNode);
#if _TREE_DEBUG
						DebugLog.WriteLine("UNIQUE GAME FOUND ({0}) - Hash: {1:x8}", uniqueGames.Count + tlsUniqueGames.Value.Count, e.Game.FuzzyGameHash);
						DebugLog.WriteLine("{0:S}", e.Game);
#endif
					}
					else {
						tlsUniqueGames.Value[e.Game].Probability += ((ProbabilisticGameNode)e.UserData).Probability;
#if _TREE_DEBUG
						DebugLog.WriteLine("DUPLICATE GAME FOUND - Hash: {0:x8}", e.Game.FuzzyGameHash);
#endif
					}
				}
				else {
					// The game state has changed but there are more actions to do
					// (which may or may not involve further cloning) so add it to the search queue
#if _TREE_DEBUG
					DebugLog.WriteLine("QUEUEING GAME " + e.Game.GameId + " FOR NEXT SEARCH");
#endif
					if (!tlsSearchQueue.Value.ContainsKey(e.Game))
						tlsSearchQueue.Value.Add(e.Game, e.UserData as ProbabilisticGameNode);
					else
						tlsSearchQueue.Value[e.Game].Probability += ((ProbabilisticGameNode)e.UserData).Probability;
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
							searchQueue[qi.Key].Probability += qi.Value.Probability;
					}
				}
				foreach (var ug in tlsUniqueGames.Values) {
					foreach (var qi in ug) {
						if (!uniqueGames.ContainsKey(qi.Key))
							uniqueGames.Add(qi.Key, qi.Value);
						else
							uniqueGames[qi.Key].Probability += qi.Value.Probability;
					}
				}
#if _TREE_DEBUG
				DebugLog.WriteLine("QUEUE SIZE: " + searchQueue.Count);
#endif
				// Wipe the TLS lists
				tlsSearchQueue = new ThreadLocal<Dictionary<Game, ProbabilisticGameNode>>(() => new Dictionary<Game, ProbabilisticGameNode>(new FuzzyGameComparer()), trackAllValues: true);
				tlsUniqueGames = new ThreadLocal<Dictionary<Game, ProbabilisticGameNode>>(() => new Dictionary<Game, ProbabilisticGameNode>(new FuzzyGameComparer()), trackAllValues: true);

				// Quit if we have processed all nodes and none of them have children (all leaf nodes)
				if (searchQueue.Count == 0)
					break;

				// Copy the search queue and clear the current one; it will be refilled
				var nextQueue = new Dictionary<Game, ProbabilisticGameNode>(searchQueue);
				searchQueue.Clear();

				// Only parallelize if there are sufficient nodes to do so
				if (nextQueue.Count >= MinNodesToParallelize && ((RandomOutcomeSearch)t).Parallel && MaxDegreesOfParallelism > 1) {
					// Process each game's action queue until it is interrupted by OnAction above
					await Task.WhenAll(
						// Split search queue into MaxDegreesOfParallelism partitions
						from partition in Partitioner.Create(nextQueue).GetPartitions(MaxDegreesOfParallelism)
							// Run each partition in its own task
						select Task.Run(async delegate {
#if _TREE_DEBUG
							var count = 0;
							DebugLog.WriteLine("Start partition run with " + MaxDegreesOfParallelism + " degrees of parallelism");
#endif
							using (partition)
								while (partition.MoveNext()) {
									// Process each node
									var kv = partition.Current;
									await kv.Key.ActionQueue.ProcessAllAsync(kv.Value);
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
						await kv.Key.ActionQueue.ProcessAllAsync(kv.Value);
					}
#if _TREE_DEBUG
					DebugLog.WriteLine("End single-threaded run");
#endif
				}
			} while (true);
		}

		public override Dictionary<Game, double> GetUniqueGames() {
			return uniqueGames.ToDictionary(x => x.Key, x => x.Value.Probability);
		}
	}
}
