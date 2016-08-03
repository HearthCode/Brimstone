// Example of breadth-first game state searching

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using Brimstone;

namespace Test1
{
	public class BreadthFirstSearch
	{
		public const int MaxMinions = 7;

		static void Main(string[] args) {
			// =========================
			// Create initial game state
			// =========================

			var game = new Game(HeroClass.Druid, HeroClass.Druid, PowerHistory: true);
			game.Start();
	
			// Change number of Boom Bots here!
			const int boomBots = 2;

			for (int i = 0; i < MaxMinions - boomBots; i++)
				game.CurrentPlayer.Give("Bloodfen Raptor").Play();
			for (int i = 0; i < boomBots; i++)
				game.CurrentPlayer.Give("Boom Bot").Play();
			game.BeginTurn();
			for (int i = 0; i < MaxMinions - boomBots; i++)
				game.CurrentPlayer.Give("Bloodfen Raptor").Play();
			for (int i = 0; i < boomBots; i++)
				game.CurrentPlayer.Give("Boom Bot").Play();

			var ArcaneMissiles = game.CurrentPlayer.Give("Arcane Missiles");

			// Turn off console output because it affects the benchmark results
			var cOut = Console.Out;
			Console.SetOut(TextWriter.Null);

			// Start timing
			var sw = new Stopwatch();
			sw.Start();

			// Create first layer of nodes underneath the root node
			// and add them to the search queue, then do breadth-first search
			var tree = GameTree.BuildFor(Game: game, SearchMode: SearchMode.BreadthFirst, Action: () => {
				ArcaneMissiles.Play();
			});

			// Print benchmark results
			Console.SetOut(cOut);
			Console.WriteLine("{0} branches in {1}ms", tree.NodeCount, sw.ElapsedMilliseconds);
			Console.WriteLine("{0} intermediate clones pruned ({1} unique branches kept)", tree.NodeCount - tree.LeafNodeCount, tree.LeafNodeCount);
			Console.WriteLine("{0} fuzzy unique game states found", tree.GetUniqueGames().Count);
		}
	}
}
