// This program guarantees to find the number of unique board states (excluding entity IDs)
// for a given play scenario - used for testing purposes against tree search results

// It also checks the game hashing algorithm for hash collisions

// This is very slow and consumes a lot of memory but gives an accurate count of
// unique game states resulting from any scenario

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Brimstone;

namespace Test1
{
	public class Brimstone
	{
		// Configure test parameters here
		public const int MaxMinions = 7;
		public const int NumBoomBots = 1;
		public const string FillMinion = "Bloodfen Raptor";
		public static bool BoomBotTest = false;
		public static bool ArcaneMissilesTest = true;
		public static bool ConsoleOutput = false;

		static void Main(string[] args) {
			var cOut = Console.Out;

			// Create initial game state
			Console.WriteLine("Initializing game state...");
			Console.SetOut(TextWriter.Null);

			var game = new Game(HeroClass.Druid, HeroClass.Druid, PowerHistory: true);
			game.Player1.Deck.Fill();
			game.Player2.Deck.Fill();
			game.Start();

			for (int i = 0; i < MaxMinions - NumBoomBots; i++)
				game.CurrentPlayer.Give(FillMinion).Play();
			for (int i = 0; i < NumBoomBots; i++)
				game.CurrentPlayer.Give("Boom Bot").Play();
			game.BeginTurn();
			for (int i = 0; i < MaxMinions - NumBoomBots; i++)
				game.CurrentPlayer.Give(FillMinion).Play();
			for (int i = 0; i < NumBoomBots; i++)
				game.CurrentPlayer.Give("Boom Bot").Play();

			Console.SetOut(cOut);

			// The total number of clones made
			int cloneCount = 0;

			// All of the final game states found
			var leafNodeGames = new HashSet<Game>();

			// Subvert the Action processor to spawn clones when random decisions would be taken
			game.ActionQueue.OnActionStarting += (o, e) => {
				ActionQueue queue = o as ActionQueue;

				// Choosing a random entity (minion in this case)
				// Clone and start processing for every possibility
				if (e.Action is RandomChoice) {
					Console.WriteLine("");
					Console.WriteLine("Depth: " + e.Game.Depth);
					foreach (Entity entity in e.Args[RandomChoice.ENTITIES]) {
						// When cloning occurs, RandomChoice has been pulled from the action queue,
						// so we can just insert a fixed item at the start of the queue and restart the queue
						// to effectively replace it
						Game cloned = (Game)e.Game.CloneState();
						cloneCount++;
						cloned.ActionQueue.InsertDeferred(e.Source, entity);
						cloned.ActionQueue.ProcessAll();
						// If the action queue is empty, we have reached a leaf node game state
						if (cloned.ActionQueue.Queue.Count == 0)
							leafNodeGames.Add(cloned);
						// Stop action queue on this copy of the game (don't take random action or continue)
						e.Cancel = true;
					}
					Console.WriteLine("Dpeth: " + (e.Game.Depth - 1));
					Console.WriteLine("");
				}

				// Choosing a random value (damage amount in this case)
				// Clone and start processing for every possibility
				if (e.Action is RandomAmount) {
					Console.WriteLine("");
					Console.WriteLine("Depth: " + e.Game.Depth);
					for (int i = e.Args[RandomAmount.MIN]; i <= e.Args[RandomAmount.MAX]; i++) {
						// When cloning occurs, RandomAmount has been pulled from the action queue,
						// so we can just insert a fixed number at the start of the queue and restart the queue
						// to effectively replace it
						Game cloned = (Game)e.Game.CloneState();
						cloneCount++;
						cloned.ActionQueue.InsertDeferred(e.Source, i);
						cloned.ActionQueue.ProcessAll();
						// If the action queue is empty, we have reached a leaf node game state
						if (cloned.ActionQueue.Queue.Count == 0)
							leafNodeGames.Add(cloned);
						// Stop action queue on this copy of the game (don't take random action or continue)
						e.Cancel = true;
						Console.WriteLine("");
					}
					Console.WriteLine("Dpeth: " + (e.Game.Depth - 1));
					Console.WriteLine("");
				}
			};

			Console.WriteLine("Building search tree...");
			if (!ConsoleOutput)
				Console.SetOut(TextWriter.Null);

			// Start timing
			var sw = new Stopwatch();
			sw.Start();

			// Find the Boom Bot to kill
			if (BoomBotTest) {
				var BoomBot = game.CurrentPlayer.Board.First(t => t.Card.Id == "GVG_110t") as Minion;
				BoomBot.Hit(1);
			}

			if (ArcaneMissilesTest) {
				game.CurrentPlayer.Give("Arcane Missiles").Play();
			}

			// Print intermediate results
			Console.SetOut(cOut);
			Console.WriteLine("{0} branches in {1}ms", cloneCount, sw.ElapsedMilliseconds);
			Console.WriteLine("{0} leaf node games kept", leafNodeGames.Count);

			Console.WriteLine("");
			Console.WriteLine("Finding unique game states...");

			// Find unique games
			var uniqueGames = new HashSet<Game>();

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

					if (!e2.SetEquals(e1)) {
						// Check for game state hash collision (if this fires, it's a bug in the hashing algorithm)
						if (root.Entities.FuzzyGameHash == g.Entities.FuzzyGameHash)
							Console.WriteLine("Game hash collision on two unique game states");
						else
							different.Add(g);
					}
				}
				uniqueGames.Add(root);
				leafNodeGames = different;
				Console.WriteLine("{0} games remaining to process ({1} unique games found so far)", different.Count, uniqueGames.Count);
			}

			foreach (var g in uniqueGames) {
				Console.WriteLine("Player 1:\r\n" + g.Player1 + "\r\n" + g.Player1.Board);
				Console.WriteLine("Player 2:\r\n" + g.Player2 + "\r\n" + g.Player2.Board);
				Console.WriteLine("");
			}
			Console.WriteLine("{0} unique games found", uniqueGames.Count);
		}
	}
}
