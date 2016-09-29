// This program guarantees to find the number of unique board states (excluding entity IDs)
// for a given play scenario - used for testing purposes against tree search results

// It also checks the game hashing algorithm for hash collisions

// This is very slow and consumes a lot of memory but gives an accurate count of
// unique game states resulting from any scenario

using System;
using System.Diagnostics;
using System.Linq;
using Brimstone;
using Brimstone.Entities;
using Brimstone.Tree;
using static Brimstone.Actions;

namespace Test1
{
	public class Brimstone
	{
		// Configure test parameters here
		public const int MaxMinions = 3;
		public const int NumBoomBots = 1;
		public const string FillMinion = "River Crocolisk";
		public static bool BoomBotTest = false;
		public static bool ArcaneMissilesTest = true;

		static void Main(string[] args) {
			// Create initial game state
			Console.WriteLine("Initializing game state...");

			var game = new Game(HeroClass.Druid, HeroClass.Druid, PowerHistory: true);
			game.Player1.Deck.Fill();
			game.Player2.Deck.Fill();
			game.Start(FirstPlayer: 1, SkipMulligan: true);

			for (int i = 0; i < MaxMinions - NumBoomBots; i++)
				game.CurrentPlayer.Give(FillMinion).Play();
			for (int i = 0; i < NumBoomBots; i++)
				game.CurrentPlayer.Give("GVG_110t").Play();
			game.EndTurn();
			for (int i = 0; i < MaxMinions - NumBoomBots; i++)
				game.CurrentPlayer.Give(FillMinion).Play();
			for (int i = 0; i < NumBoomBots; i++)
				game.CurrentPlayer.Give("GVG_110t").Play();

			Cards.FromName("Arcane Missiles").Behaviour.Battlecry = Damage(RandomOpponentHealthyCharacter, 1) * 2;
			Cards.FromId("GVG_110t").Behaviour.Deathrattle = Damage(RandomOpponentHealthyMinion, RandomAmount(1, 4));

			// Start timing
			var sw = new Stopwatch();
			sw.Start();

			// Build search tree
			Console.WriteLine("Building search tree...");

			var tree = RandomOutcomeSearch.Build(
				Game: game,
				SearchMode: new NaiveActionWalker(),
				Action: () => {
					if (BoomBotTest) {
						var BoomBot = game.CurrentPlayer.Board.First(t => t.Card.Id == "GVG_110t");
						BoomBot.Hit(1);
					}

					if (ArcaneMissilesTest) {
						game.CurrentPlayer.Give("Arcane Missiles").Play();
					}
				}
			);

			// Print intermediate results
			Console.WriteLine("{0} branches in {1}ms", tree.NodeCount, sw.ElapsedMilliseconds);
			Console.WriteLine("{0} leaf node games kept", tree.LeafNodeCount);
			Console.WriteLine("");

			// Find unique games
			Console.WriteLine("Finding unique game states...");

			var uniqueGames = tree.GetUniqueGames();
			sw.Stop();

			foreach (var kv in uniqueGames) {
				Console.WriteLine(Math.Round(kv.Value * 100, 2) + "%: ");
				Console.WriteLine("{0:s}", kv.Key);
			}
			Console.WriteLine("{0} unique games found in {1}ms", uniqueGames.Count, sw.ElapsedMilliseconds);
		}
	}
}
