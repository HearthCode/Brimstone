/*
	Copyright 2016, 2017 Katy Coe

	This file is part of Brimstone.

	Brimstone is free software: you can redistribute it and/or modify
	it under the terms of the GNU Lesser General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	Brimstone is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU Lesser General Public License for more details.

	You should have received a copy of the GNU Lesser General Public License
	along with Brimstone.  If not, see <http://www.gnu.org/licenses/>.
*/

// Example of asynchronous (non-blocking) depth-first game state searching

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Brimstone;
using Brimstone.Entities;
using Brimstone.Tree;
using static Brimstone.Actions;

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

		static void Main(string[] args) {
			DepthFirstAsync().Wait();
		}

		static async Task DepthFirstAsync() {
			// Create initial game state
			Console.WriteLine("Initializing game state...");

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

			Cards.FromName("Arcane Missiles").Behaviour.Battlecry = Damage(RandomOpponentHealthyCharacter, 1) * 2;
			Cards.FromId("GVG_110t").Behaviour.Deathrattle = Damage(RandomOpponentHealthyMinion, RandomAmount(1, 4));

			// Start timing
			var sw = new Stopwatch();
			sw.Start();

			// Start the search going on another thread and retrieve its task handle
			var treeTask = RandomOutcomeSearch.BuildAsync(
				Game: game,
				SearchMode: new DepthFirstActionWalker(),
				Action: () => {
					if (BoomBotTest) {
						var BoomBot = game.CurrentPlayer.Board.First(t => t.Card.Id == "GVG_110t");
						BoomBot.Hit(1);
					}

					if (ArcaneMissilesTest) {
						ArcaneMissiles.Play();
					}
				}
			);

			// Now we can do other stuff until our tree search results are ready!
			while (!treeTask.IsCompleted) {
				Console.Write(".");
				await Task.Delay(100);
			}

			// Get the results
			var tree = treeTask.Result;

			Console.WriteLine("");

			// Print benchmark results
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
