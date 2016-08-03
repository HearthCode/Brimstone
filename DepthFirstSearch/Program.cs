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

			// Turn off console output because it affects the benchmark results
			var cOut = Console.Out;
			Console.SetOut(TextWriter.Null);

			// Start timing
			var sw = new Stopwatch();
			sw.Start();

			// Perform the search
			var tree = GameTree.BuildFor(
				Game: game,
				SearchMode: new DepthFirstTreeSearch(),
				Action: () => { game.CurrentPlayer.Give("Arcane Missiles").Play(); }
			);

			// Print benchmark results
			Console.SetOut(cOut);

			Console.WriteLine("{0} branches in {1}ms", tree.NodeCount, sw.ElapsedMilliseconds);
			Console.WriteLine("{0} intermediate clones pruned ({1} unique branches kept)", tree.NodeCount - tree.LeafNodeCount, tree.LeafNodeCount);
			Console.WriteLine("{0} fuzzy unique game states found", tree.GetUniqueGames().Count);
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
