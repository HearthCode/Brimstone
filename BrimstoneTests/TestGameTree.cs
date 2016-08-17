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
			[Values(typeof(NaiveTreeSearch), typeof(DepthFirstTreeSearch), typeof(BreadthFirstTreeSearch))] Type SearchMode,
			[Values(true,false)] bool Parallel) {

			Settings.ParallelTreeSearch = Parallel;

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
			[Values(typeof(DepthFirstTreeSearch), typeof(BreadthFirstTreeSearch))] Type SearchMode,
			[Values(true, false)] bool Parallel) {

			Settings.ParallelTreeSearch = Parallel;

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
			return RandomOutcomeSearch.Find(
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

		[Test]
		public void TestTreeUsage([Values(true,false)] bool ParallelClone) {
			Settings.ParallelClone = ParallelClone;

			var game = _setupGame(7, 2, "Bloodfen Raptor");

			// Make a new tree with the game as the root
			var tree = new GameTree<ProbabilisticGameNode>(new ProbabilisticGameNode(game));

			// Add arbitrary number of children
			int children = 3;

			var depth1Nodes = tree.RootNode.Branch(children).ToList();

			// Check the correct number of games were cloned
			Assert.AreEqual(children, depth1Nodes.Count);
			Assert.AreEqual(children, tree.RootNode.Children.Count);

			// Assert all children have correct parent in both GameNode and Game
			foreach (var n in depth1Nodes) {
				Assert.AreSame(game, n.Parent.Game);
				Assert.AreSame(tree.RootNode, n.Parent);
			}

			// Check all child games are unique but exact clones
			var childGameIds = new List<int>();
			foreach (var n in depth1Nodes) {
				Assert.False(childGameIds.Contains(n.Game.GameId));
				Assert.True(game.EquivalentTo(n.Game));
				childGameIds.Add(n.Game.GameId);
			}

			// Get all games from nodes
			var depth1Games = depth1Nodes.Select(n => n.Game).ToList();

			// Do something different on each child
			depth1Games[0].CurrentPlayer.Give("Flame Juggler").Play();
			depth1Games[1].CurrentPlayer.Give("Arcane Missiles").Play();
			depth1Games[2].CurrentPlayer.Give("Whirlwind").Play();

			// Check every game is different
			var childHashes = new List<int>();
			foreach (var g in depth1Games) {
				Assert.False(childHashes.Contains(g.Entities.FuzzyGameHash));
				childHashes.Add(g.Entities.FuzzyGameHash);
			}

			// Do a random action on the first game in depth 1 and add all possible outcomes as children
			Minion FirstBoomBot = depth1Games[0].CurrentPlayer.Board.Where(x => x.Card.Name == "Boom Bot").First() as Minion;
			var boomBotResults = RandomOutcomeSearch.Find(depth1Games[0], () => FirstBoomBot.Hit(1));

			var depth2Nodes = depth1Nodes[0].AddChildren(boomBotResults).ToList();

			// Check the correct number of games were cloned
			Assert.AreEqual(boomBotResults.Count, depth2Nodes.Count);
			Assert.AreEqual(boomBotResults.Count, depth1Nodes[0].Children.Count);

			// Assert all children have correct parent in both GameNode and Game
			foreach (var n in depth2Nodes) {
				Assert.AreSame(depth1Games[0], n.Parent.Game);
				Assert.AreSame(depth1Nodes[0], n.Parent);
			}
		}
	}
}