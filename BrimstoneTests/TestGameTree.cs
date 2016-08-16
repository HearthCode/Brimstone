using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Brimstone;

namespace BrimstoneTests
{
	[TestFixture]
	public class TestGameTree
	{
		enum TestAction
		{
			BoomBot,
			ArcaneMissiles
		}

		[Test]
		public void TestTreeSearchProbabilities(
			[Values(typeof(NaiveTreeSearch), typeof(DepthFirstTreeSearch), typeof(BreadthFirstTreeSearch))] Type SearchMode) {

			var game = _setupGame(MaxMinions: 3, NumBoomBots: 1, FillMinion: "River Crocolisk");
			var uniqueGames = _search(game, (ITreeSearcher)Activator.CreateInstance(SearchMode), TestAction.BoomBot);

			// Check we got the expected number of unique game states
			Assert.AreEqual(18, uniqueGames.Count);

			// Check that all the probabilities add up to 100%
			Assert.AreEqual(1.0, uniqueGames.Values.Sum(), 0.0000001);

			// Check that each game state's probability is correct

			// 12/48
			var opponentRCDead = uniqueGames.Where(x => x.Key.Player1.Board.Where(y => y.Card.Name == "River Crocolisk").Count() == 1).First().Value;
			Assert.AreEqual(1.0 / 4.0, opponentRCDead, 0.0000001);

			// 12/48
			var opponentFace = uniqueGames.Where(x => x.Key.Player1.Hero.Health < 30).Select(x => x.Value).ToList();
			foreach (var p in opponentFace)
				Assert.AreEqual(3.0 / 48.0, p, 0.0000001);

			// 4/48
			var friendlyFace = uniqueGames.Where(x => x.Key.Player2.Hero.Health < 30).Select(x => x.Value).ToList();
			foreach (var p in friendlyFace)
				Assert.AreEqual(1.0 / 48.0, p, 0.0000001);

			// 4/48
			var bothBoomBotsRCDead = uniqueGames.Where(x => x.Key.Player1.Board.Concat(x.Key.Player2.Board).Where(y => y.Card.Name == "Boom Bot").Count() == 0
														&& x.Key.Player2.Board.Where(y => y.Card.Name == "River Crocolisk").Count() == 1).First().Value;
			Assert.AreEqual(4.0 / 48.0, bothBoomBotsRCDead, 0.0000001);

			// 12/48
			var opponentRCDamaged = uniqueGames.Where(x => x.Key.Player1.Board.Where(y => y.Card.Name == "River Crocolisk").Count() == 2
													&& x.Key.Player1.Board.Where(y => y.Card.Name == "Boom Bot").Count() == 1
													&& x.Key.Player1.Hero.Health == 30).Select(x => x.Value).ToList();
			foreach (var p in opponentRCDamaged)
				Assert.AreEqual(3.0 / 48.0, p, 0.0000001);

			// 4/48
			var friendlyRCDamaged = uniqueGames.Where(x => x.Key.Player2.Board.Where(y => y.Card.Name == "River Crocolisk").Count() == 2
													&& x.Key.Player1.Board.Where(y => y.Card.Name == "Boom Bot").Count() == 0)
													.Select(x => x.Value).ToList();
			foreach (var p in friendlyRCDamaged)
				Assert.AreEqual(1.0 / 48.0, p, 0.0000001);
		}

		// NOTE: Naive tree searching is too slow for this test so we omit it
		[Test]
		public void TestTreeSearchUniqueness(
			[Values(typeof(DepthFirstTreeSearch), typeof(BreadthFirstTreeSearch))] Type SearchMode) {

			// Our paper-verified tests use Boom Bots that cannot go face, and Arcane Missiles which fires only 2 missiles
			Cards.FromName("Boom Bot").Behaviour.Deathrattle = Actions.Damage(Actions.RandomOpponentMinion, Actions.RandomAmount(1, 4));
			Cards.FromName("Arcane Missiles").Behaviour.Battlecry = Actions.Damage(Actions.RandomOpponentCharacter, 1) * 2;

			var game = _setupGame(MaxMinions: 7, NumBoomBots: 2, FillMinion: "Bloodfen Raptor");
			var uniqueGames = _search(game, (ITreeSearcher)Activator.CreateInstance(SearchMode), TestAction.ArcaneMissiles).Keys;

			// Check we got the expected number of unique game states
			Assert.AreEqual(154, uniqueGames.Count);

			// Check that each filtered tree section has the expected number of game states
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

			Assert.AreEqual(17, noBoomBotsDead.Count());
			Assert.AreEqual(36, oneBoomBotDead.Count());
			Assert.AreEqual(16, bothOpponentBoomBotsDead.Count());
			Assert.AreEqual(21, oneFoneOBoomBotsDead.Count());
			Assert.AreEqual(42, oneFtwoOBoomBotsDead.Count());
			Assert.AreEqual(0, twoFoneOBoomBotsDead.Count());
			Assert.AreEqual(22, allBoomBotsDead.Count());

			Assert.AreEqual(154, 17 + 36 + 16 + 21 + 42 + 0 + 22);
		}

		private Dictionary<Game, double> _search(Game game, ITreeSearcher searcher, TestAction testAction) {
			var tree = GameTree.Build(
				Game: game,
				SearchMode: searcher,
				Action: () => {
					// This is the action that shall be taken to build the tree
					switch (testAction) {
						case TestAction.BoomBot:
							((Minion)game.CurrentPlayer.Board.First(t => t.Card.Name == "Boom Bot")).Hit(1);
							break;

						case TestAction.ArcaneMissiles:
							((Spell)game.CurrentPlayer.Hand.First(t => t.Card.Name == "Arcane Missiles")).Play();
							break;
					}
				}
			);
			return tree.GetUniqueGames();
		}

		private Game _setupGame(int MaxMinions, int NumBoomBots, string FillMinion) {
			// Console output slows down these tests dramatically
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

			game.CurrentPlayer.Give("Arcane Missiles");
			return game;
		}
	}
}