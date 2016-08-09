// Example of breadth-first game state searching

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using Brimstone;

namespace Test1
{
	public class Brimstone
	{
		// Configure test parameters here
		public const int MaxMinions = 3;
		public const int NumBoomBots = 1;
		public const string FillMinion = "River Crocolisk";
		public static bool BoomBotTest = true;
		public static bool ArcaneMissilesTest = false;
		public static bool ConsoleOutput = false;

		static void Main(string[] args) {
			var cOut = Console.Out;

			// Create initial game state
			Console.WriteLine("Initializing game state...");
			Console.SetOut(TextWriter.Null);

			var game = new Game(HeroClass.Druid, HeroClass.Druid, PowerHistory: true);
			game.Player1.Deck.Fill();
			game.Player2.Deck.Fill();
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

			Console.SetOut(cOut);

			// Start timing
			var sw = new Stopwatch();
			sw.Start();

			if (!ConsoleOutput)
				Console.SetOut(TextWriter.Null);

			// Perform the search
			var tree = GameTree.BuildFor(
				Game: game,
				SearchMode: new DepthFirstTreeSearch(),
				Action: () => {
					if (BoomBotTest) {
						var BoomBot = game.CurrentPlayer.Board.First(t => t.Card.Id == "GVG_110t") as Minion;
						BoomBot.Hit(1);
					}

					if (ArcaneMissilesTest) {
						game.CurrentPlayer.Give("Arcane Missiles").Play();
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

			/*
						var noBoomBotsDead = uniqueGames.Where(x => x.Player1.Board.Concat(x.Player2.Board).Where(y => y.Card.Name == "Boom Bot").Count() == 4);
						var oneBoomBotDead = uniqueGames.Where(x => x.Player1.Board.Concat(x.Player2.Board).Where(y => y.Card.Name == "Boom Bot").Count() == 3);
						var bothOpponentBoomBotsDead = uniqueGames.Where(x => x.CurrentPlayer.Board.Where(y => y.Card.Name == "Boom Bot").Count() == 2
																	&& x.CurrentPlayer.Opponent.Board.Where(y => y.Card.Name == "Boom Bot").Count() == 0);
						var oneFoneOBoomBotsDead = uniqueGames.Where(x => x.CurrentPlayer.Board.Where(y => y.Card.Name == "Boom Bot").Count() == 1
																	&& x.CurrentPlayer.Opponent.Board.Where(y => y.Card.Name == "Boom Bot").Count() == 1);
						var oneFtwoOBoomBotsDead = uniqueGames.Where(x => x.CurrentPlayer.Board.Where(y => y.Card.Name == "Boom Bot").Count() == 1
																	&& x.CurrentPlayer.Opponent.Board.Where(y => y.Card.Name == "Boom Bot").Count() == 0);
						var twoFoneOBoomBotsDead = uniqueGames.Where(x => x.CurrentPlayer.Board.Where(y => y.Card.Name == "Boom Bot").Count() == 0
																	&& x.CurrentPlayer.Opponent.Board.Where(y => y.Card.Name == "Boom Bot").Count() == 1);
						var allBoomBotsDead = uniqueGames.Where(x => x.Player1.Board.Concat(x.Player2.Board).Where(y => y.Card.Name == "Boom Bot").Count() == 0);

						Console.WriteLine(noBoomBotsDead.Count());
						Console.WriteLine(oneBoomBotDead.Count());
						Console.WriteLine(bothOpponentBoomBotsDead.Count());
						Console.WriteLine(oneFoneOBoomBotsDead.Count());
						Console.WriteLine(oneFtwoOBoomBotsDead.Count());
						Console.WriteLine(twoFoneOBoomBotsDead.Count());
						Console.WriteLine(allBoomBotsDead.Count());
						*/
		}
	}
}
