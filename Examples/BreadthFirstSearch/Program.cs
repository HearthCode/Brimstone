// Example of breadth-first game state searching

using System;
using System.Diagnostics;
using System.Linq;
using Brimstone;
using Brimstone.Entities;
using Brimstone.Tree;
using static Brimstone.Actions;

namespace Test1
{
	public class BreadthFirstSearch
	{
		// Configure test parameters here
		public const int MaxMinions = 7;
		public const int NumBoomBots = 2;
		public const string FillMinion = "Bloodfen Raptor";
		public static bool BoomBotTest = false;
		public static bool ArcaneMissilesTest = true;

		static void Main(string[] args) {
			for (int missiles = 1; missiles <= 6; missiles++)
			{
				// Create initial game state
				Console.WriteLine("Initializing game state for {0} missiles test...", missiles);

				var game = new Game(HeroClass.Druid, HeroClass.Druid, PowerHistory: true);
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

				var ArcaneMissiles = game.CurrentPlayer.Give("Arcane Missiles");

				Cards.FromName("Arcane Missiles").Behaviour.Battlecry = Damage(RandomOpponentHealthyCharacter, 1) * missiles;
				Cards.FromId("GVG_110t").Behaviour.Deathrattle = Damage(RandomOpponentHealthyMinion, RandomAmount(1, 4));

				// Start timing
				var sw = new Stopwatch();
				sw.Start();

				// Create first layer of nodes underneath the root node
				// and add them to the search queue, then do breadth-first search
				var tree = RandomOutcomeSearch.Build(
					Game: game,
					SearchMode: new BreadthFirstActionWalker(),
					Action: () =>
					{
						if (BoomBotTest)
						{
							var BoomBot = game.CurrentPlayer.Board.First(t => t.Card.Id == "GVG_110t");
							BoomBot.Hit(1);
						}

						if (ArcaneMissilesTest)
						{
							ArcaneMissiles.Play();
						}
					}
					);

				// Print benchmark results
				Console.WriteLine("{0} branches in {1}ms", tree.NodeCount, sw.ElapsedMilliseconds);
				Console.WriteLine("{0} intermediate clones pruned ({1} unique branches kept)", tree.NodeCount - tree.LeafNodeCount,
					tree.LeafNodeCount);

				var uniqueGames = tree.GetUniqueGames();
				sw.Stop();

				Console.WriteLine("{0} unique games found in {1}ms", uniqueGames.Count, sw.ElapsedMilliseconds);
				Console.WriteLine("");
			}
		}
	}
}
