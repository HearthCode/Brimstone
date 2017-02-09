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

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Brimstone;
using Brimstone.Entities;
using Brimstone.QueueActions;
using Brimstone.Tree;
using static Brimstone.Actions;

namespace BrimstoneTests
{
	[TestFixture]
	class TestActionGraph
	{
		private List<QueueAction> originalBoomBot;
		private List<QueueAction> originalMissiles;

		[SetUp]
		public void Setup() {
			originalBoomBot = Cards.FromId("GVG_110t").Behaviour.Deathrattle;
			originalMissiles = Cards.FromName("Arcane Missiles").Behaviour.Battlecry;
		}

		[TearDown]
		public void Teardown() {
			Cards.FromId("GVG_110t").Behaviour.Deathrattle = originalBoomBot;
			Cards.FromName("Arcane Missiles").Behaviour.Battlecry = originalMissiles;
		}

		[Test]
		public void TestPartialArgumentSyntax() {
			// Arrange
			var ActionGraphs = new List<ActionGraph> {
				// The default
				Damage(RandomOpponentHealthyMinion, RandomAmount(1, 4)),
				// Swapped named arguments
				Damage(Amount: RandomAmount(1,4), Targets: RandomOpponentHealthyMinion),
				// No arguments to Damage
				RandomOpponentHealthyMinion.Then(1).Then(4).Then(RandomAmount()).Then(Damage()),
				RandomOpponentHealthyMinion.Then(RandomAmount(1, 4)).Then(Damage()),
				// No Targets argument to Damage
				RandomOpponentHealthyMinion.Then(Damage(Amount: RandomAmount(1, 4))),
				RandomOpponentHealthyMinion.Then(Damage(Amount: ((ActionGraph)1).Then(4).Then(RandomAmount()))),
				// No Amount argument to damage
				RandomAmount(1, 4).Then(Damage(Targets: RandomOpponentHealthyMinion)),
				((ActionGraph)1).Then(4).Then(RandomAmount()).Then(Damage(Targets: RandomOpponentHealthyMinion))
			};

			Cards.FromName("Arcane Missiles").Behaviour.Battlecry = Damage(RandomOpponentHealthyCharacter, 1) * 1;

			foreach (var graph in ActionGraphs) {
				// Arrange
				var game = _setupGame(7, 2, "Bloodfen Raptor");

				// This converts the ActionGraph to a List<QueueAction>
				Cards.FromId("GVG_110t").Behaviour.Deathrattle = graph;

				// Act
				var count = RandomOutcomeSearch.Find(game, () => game.CurrentPlayer.Hand.First(t => t.Card.Name == "Arcane Missiles").Play()).Count;

				// Assert
				// 1 missile produces 30 game states if Boom Bot deathrattle works correctly
				Assert.AreEqual(30, count);
			}
		}

		[Test]
		public void TestQueueCancellation() {
			var game = new Game(HeroClass.Druid, HeroClass.Druid, PowerHistory: true);
			game.Start(1, SkipMulligan: true);

			for (int i = 0; i < 2; i++)
				game.CurrentPlayer.Give("Bloodfen Raptor").Play();
			game.CurrentPlayer.Give("GVG_110t").Play();
			game.EndTurn();
			game.CurrentPlayer.Give("Bloodfen Raptor").Play();
			for (int i = 0; i < 2; i++)
				game.CurrentPlayer.Give("GVG_110t").Play();
			game.EndTurn();

			var arcane = game.CurrentPlayer.Give("Arcane Missiles");

			arcane.Card.Behaviour.Battlecry =
				Damage(game.CurrentOpponent.Board[2], 1)
					.Then(Damage(game.CurrentOpponent.Board[3], 1));

			int clones = 0;
			int keptClones = 0;

			game.ActionQueue.OnActionStarting += (o, e) => {
				ActionQueue queue = o as ActionQueue;
				if (e.Action is RandomChoice) {
					foreach (var entity in e.Args[RandomChoice.ENTITIES]) {
						clones++;
						Game cloned = e.Game.CloneState();
						cloned.ActionQueue.StackPush((Entity)cloned.Entities[entity.Id]);
						cloned.ActionQueue.ProcessAll();
						if (!cloned.ActionQueue.LastActionCancelled)
							keptClones++;
						e.Cancel = true;
					}
				}
				if (e.Action is RandomAmount) {
					for (int i = e.Args[Brimstone.QueueActions.RandomAmount.MIN];
					i <= e.Args[Brimstone.QueueActions.RandomAmount.MAX]; i++) {
						clones++;
						Game cloned = e.Game.CloneState();
						cloned.ActionQueue.StackPush(i);
						cloned.ActionQueue.ProcessAll();
						if (!cloned.ActionQueue.LastActionCancelled)
							keptClones++;
						e.Cancel = true;
					}
				}
			};
			arcane.Play();

			Assert.AreEqual(1250, clones);
			Assert.AreEqual(888, keptClones);
		}

		private Game _setupGame(int MaxMinions, int NumBoomBots, string FillMinion) {
			var game = new Game(HeroClass.Druid, HeroClass.Druid, PowerHistory: true);
			game.Start(1, SkipMulligan: true);

			for (int i = 0; i < MaxMinions - NumBoomBots; i++)
				game.CurrentPlayer.Give(FillMinion).Play();
			for (int i = 0; i < NumBoomBots; i++)
				game.CurrentPlayer.Give("GVG_110t").Play();
			game.EndTurn();
			for (int i = 0; i < MaxMinions - NumBoomBots; i++)
				game.CurrentPlayer.Give(FillMinion).Play();
			for (int i = 0; i < NumBoomBots; i++)
				game.CurrentPlayer.Give("GVG_110t").Play();

			game.CurrentPlayer.Give("Arcane Missiles");
			return game;
		}
	}
}
