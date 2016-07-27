﻿// Example of breadth-first game state searching

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
			game.Player1.Deck.Fill();
			game.Player2.Deck.Fill();
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

			// The total number of clones made
			int cloneCount = 0;

			// The number of clones kept for equality testing (not pruned)
			int keptClonesCount = 0;

			// All of the unique game states found
			var uniqueGames = new HashSet<Game>(new FuzzyGameComparer());

			// Subvert the Action processor to spawn clones when random decisions would be taken
			game.ActionQueue.OnActionStarting += (o, e) => {
				ActionQueue queue = o as ActionQueue;

				// Choosing a random entity (minion in this case)
				// Clone and start processing for every possibility
				if (e.Action is RandomChoice) {
					foreach (Entity entity in e.Args[RandomChoice.ENTITIES]) {
						// When cloning occurs, RandomChoice has been pulled from the action queue,
						// so we can just insert a fixed item at the start of the queue and restart the queue
						// to effectively replace it
						Game cloned = (Game)e.Game.CloneState();
						cloneCount++;
						cloned.ActionQueue.InsertDeferred(e.Source, entity);
						cloned.ActionQueue.ProcessAll();
						// If the action queue is empty, we have reached a leaf node game state
						// so compare it for equality with other final game states
						if (cloned.ActionQueue.Queue.Count == 0)
							if (!cloned.EquivalentTo(e.Game)) {
								keptClonesCount++;
								// This will cause the game to be discarded if its fuzzy hash matches any other final game state
								uniqueGames.Add(cloned);
							}
						// Stop action queue on this copy of the game (don't take random action or continue)
						e.Cancel = true;
					}
				}

				// Choosing a random value (damage amount in this case)
				// Clone and start processing for every possibility
				if (e.Action is RandomAmount) {
					for (int i = e.Args[RandomAmount.MIN]; i <= e.Args[RandomAmount.MAX]; i++) {
						// When cloning occurs, RandomAmount has been pulled from the action queue,
						// so we can just insert a fixed number at the start of the queue and restart the queue
						// to effectively replace it
						Game cloned = (Game)e.Game.CloneState();
						cloneCount++;
						cloned.ActionQueue.InsertDeferred(e.Source, i);
						cloned.ActionQueue.ProcessAll();
						// If the action queue is empty, we have reached a leaf node game state
						// so compare it for equality with other final game states
						if (cloned.ActionQueue.Queue.Count == 0)
							if (!cloned.EquivalentTo(e.Game)) {
								keptClonesCount++;
								// This will cause the game to be discarded if its fuzzy hash matches any other final game state
								uniqueGames.Add(cloned);
							}
						// Stop action queue on this copy of the game (don't take random action or continue)
						e.Cancel = true;
					}
				}
			};

			// Turn off console output because it affects the benchmark results
			var cOut = Console.Out;
			Console.SetOut(TextWriter.Null);

			// Start timing
			var sw = new Stopwatch();
			sw.Start();

			// Find the Boom Bot to kill
			var BoomBot = game.CurrentPlayer.Board.First(t => t.Card.Id == "GVG_110t") as Minion;

			// Perform the search
			BoomBot.Hit(1);

			// Print benchmark results
			Console.SetOut(cOut);
			Console.WriteLine("{0} branches in {1}ms", cloneCount, sw.ElapsedMilliseconds);
			Console.WriteLine("{0} intermediate clones pruned ({1} unique branches kept)", cloneCount - keptClonesCount, keptClonesCount);
			Console.WriteLine("{0} fuzzy unique game states found", uniqueGames.Count);
		}
	}
}