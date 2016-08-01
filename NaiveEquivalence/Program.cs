// This program guarantees to find the number of unique board states (excluding entity IDs)
// for a given play scenario - used for testing purposes against tree search results

// It also checks the game hashing algorithm for hash collisions

// This is very slow and consumes a lot of memory but gives an accurate count of
// unique game states resulting from any scenario

using System;
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
		public const int NumBoomBots = 2;
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

			// Start timing
			var sw = new Stopwatch();
			sw.Start();

			// Build search tree
			Console.WriteLine("Building search tree...");
			if (!ConsoleOutput)
				Console.SetOut(TextWriter.Null);

			var tree = NaiveGameTree.BuildFor(game, () => {
				if (BoomBotTest) {
					var BoomBot = game.CurrentPlayer.Board.First(t => t.Card.Id == "GVG_110t") as Minion;
					BoomBot.Hit(1);
				}

				if (ArcaneMissilesTest) {
					game.CurrentPlayer.Give("Arcane Missiles").Play();
				}
			});

			// Print intermediate results
			Console.SetOut(cOut);
			Console.WriteLine("{0} branches in {1}ms", tree.NodeCount, sw.ElapsedMilliseconds);
			Console.WriteLine("{0} leaf node games kept", tree.LeafNodeCount);
			Console.WriteLine("");

			// Find unique games
			Console.WriteLine("Finding unique game states...");

			var uniqueGames = tree.GetUniqueGames();

			Console.WriteLine("{0} unique games found in {1}ms", uniqueGames.Count, sw.ElapsedMilliseconds);
		}
	}
}
