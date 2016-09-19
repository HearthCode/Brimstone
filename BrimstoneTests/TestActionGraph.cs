using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Brimstone;
using Brimstone.Entities;
using Brimstone.Tree;
using static Brimstone.Behaviours;

namespace BrimstoneTests
{
	[TestFixture]
	class TestActionGraph
	{
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

			var originalMissiles = Cards.FromName("Arcane Missiles").Behaviour.Battlecry;
			var originalBoomBot = Cards.FromId("GVG_110t").Behaviour.Deathrattle;
			var newMissiles = Damage(RandomOpponentHealthyCharacter, 1) * 1;

			foreach (var graph in ActionGraphs) {
				// Arrange
				var game = _setupGame(7, 2, "Bloodfen Raptor");
				Cards.FromName("Arcane Missiles").Behaviour.Battlecry = newMissiles;

				// This converts the ActionGraph to a List<QueueAction>
				Cards.FromId("GVG_110t").Behaviour.Deathrattle = graph;

				// Act
				var count = RandomOutcomeSearch.Find(game, () => game.CurrentPlayer.Hand.First(t => t.Card.Name == "Arcane Missiles").Play()).Count;

				// Reset
				Cards.FromName("Arcane Missiles").Behaviour.Battlecry = originalMissiles;
				Cards.FromId("GVG_110t").Behaviour.Deathrattle = originalBoomBot;

				// Assert
				// 1 missile produces 30 game states if Boom Bot deathrattle works correctly
				Assert.AreEqual(30, count);
			}

			// Put stuff back to normal
			Cards.FromName("Arcane Missiles").Behaviour.Battlecry = Damage(RandomOpponentHealthyCharacter, 1) * 3;

		}

		private Game _setupGame(int MaxMinions, int NumBoomBots, string FillMinion) {
			var game = new Game(HeroClass.Druid, HeroClass.Druid, PowerHistory: false);
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
