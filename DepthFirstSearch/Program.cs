// Example of depth-first game state searching

using System;
using System.Diagnostics;
using System.Linq;
using System.IO;
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
			game.Start(1);

			for (int i = 0; i < MaxMinions - NumBoomBots; i++)
				game.CurrentPlayer.Give(FillMinion).Play();
			for (int i = 0; i < NumBoomBots; i++)
				game.CurrentPlayer.Give("Boom Bot").Play();
			game.BeginTurn();
			for (int i = 0; i < MaxMinions - NumBoomBots; i++)
				game.CurrentPlayer.Give(FillMinion).Play();
			for (int i = 0; i < NumBoomBots; i++)
				game.CurrentPlayer.Give("Boom Bot").Play();

			var ArcaneMissiles = game.CurrentPlayer.Give("Arcane Missiles");

			Console.SetOut(cOut);

			// Start timing
			var sw = new Stopwatch();
			sw.Start();

			if (!ConsoleOutput)
				Console.SetOut(TextWriter.Null);

			Cards.FromName("Arcane Missiles").Behaviour.Battlecry = Actions.Damage(Actions.RandomOpponentCharacter, 1) * 2;
			Cards.FromName("Boom Bot").Behaviour.Deathrattle = Actions.Damage(Actions.RandomOpponentMinion, Actions.RandomAmount(1, 4));

			// Perform the search
			var tree = RandomOutcomeSearch.Build(
				Game: game,
				SearchMode: new DepthFirstActionWalker(),
				Action: () => {
					if (BoomBotTest) {
						var BoomBot = game.CurrentPlayer.Board.First(t => t.Card.Id == "GVG_110t") as Minion;
						BoomBot.Hit(1);
					}

					if (ArcaneMissilesTest) {
						ArcaneMissiles.Play();
					}
				}
			);

			// Print benchmark results
			Console.SetOut(cOut);
			Console.WriteLine("{0} branches in {1}ms", tree.NodeCount, sw.ElapsedMilliseconds);
			Console.WriteLine("{0} intermediate clones pruned ({1} unique branches kept)", tree.NodeCount - tree.LeafNodeCount, tree.LeafNodeCount);

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
