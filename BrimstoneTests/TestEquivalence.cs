using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Brimstone;

namespace BrimstoneTests
{
	[TestFixture]
	public class TestEquivalence
	{
		[Test]
		public void TestEntityEquivalence() {
			// Arrange
			var game1 = new Game(HeroClass.Druid, HeroClass.Warrior, PowerHistory: true);
			var game2 = new Game(HeroClass.Druid, HeroClass.Warrior, PowerHistory: true);

			// Act
			var wisp1 = game1.Player1.Give("Wisp");

			// Assert

			// Different wisp, same entity ID, same controller, same game, same hand position
			var wisp2 = game2.Player1.Give("Wisp");

			Assert.AreEqual(wisp1.Id, wisp2.Id);
			Assert.AreEqual(wisp1[GameTag.ZONE_POSITION], wisp2[GameTag.ZONE_POSITION]);
			Assert.AreEqual(wisp1, wisp2);

			Assert.AreEqual(wisp1.FuzzyHash, wisp2.FuzzyHash);

			// Different wisp, different entity ID, same controller, same/different game, different hand position
			var wisp3 = game1.Player1.Give("Wisp");

			Assert.AreNotEqual(wisp1.Id, wisp3.Id);
			Assert.AreEqual(wisp1.Controller.Id, wisp3.Controller.Id);
			Assert.AreNotEqual(wisp1[GameTag.ZONE_POSITION], wisp3[GameTag.ZONE_POSITION]);
			Assert.AreNotEqual(wisp1, wisp3);
			Assert.AreNotEqual(wisp2, wisp3);

			Assert.AreEqual(wisp1.FuzzyHash, wisp3.FuzzyHash);
			Assert.AreEqual(wisp2.FuzzyHash, wisp3.FuzzyHash);

			// Different wisp, same entity ID, different controller, same game, same hand position
			wisp2.Controller = game1.Player2;

			Assert.AreEqual(wisp1.Id, wisp2.Id);
			Assert.AreNotEqual(wisp1.Controller.Id, wisp2.Controller.Id);
			Assert.AreEqual(wisp1[GameTag.ZONE_POSITION], wisp2[GameTag.ZONE_POSITION]);

			Assert.AreNotEqual(wisp1, wisp2);
			Assert.AreNotEqual(wisp1.FuzzyHash, wisp2.FuzzyHash);

			// Clone a wisp, check their states are equal
			// NOTE: Cloning an entity detaches it from game and controller by default
			var wisp4 = game1.Player1.Give("Wisp");
			var wisp5 = wisp4.CloneState();
			wisp5.Game = game1;
			wisp5.Controller = wisp4.Controller;

			Assert.AreEqual(wisp4, wisp5);
			Assert.AreEqual(wisp4.FuzzyHash, wisp5.FuzzyHash);

			// Change one of them, check their states are not equal
			wisp5[GameTag.ZONE] = (int)Zone.PLAY;
			Assert.AreNotEqual(wisp4, wisp5);
			Assert.AreNotEqual(wisp4.FuzzyHash, wisp5.FuzzyHash);

			// Change it back
			wisp5[GameTag.ZONE] = (int)Zone.HAND;
			Assert.AreEqual(wisp4, wisp5);
			Assert.AreEqual(wisp4.FuzzyHash, wisp5.FuzzyHash);

			// Chaange hand position only
			int oldPos = wisp5[GameTag.ZONE_POSITION];
			wisp5[GameTag.ZONE_POSITION] = 5;
			Assert.AreNotEqual(wisp4, wisp5);
			Assert.AreEqual(wisp4.FuzzyHash, wisp5.FuzzyHash);

			// Change entity ID only
			wisp5[GameTag.ZONE_POSITION] = oldPos;
			Assert.AreEqual(wisp4, wisp5);
			wisp5.Id = 1234;
			Assert.AreEqual(wisp4.FuzzyHash, wisp5.FuzzyHash);
			Assert.AreNotEqual(wisp4, wisp5);
		}

		[Test]
		public void TestFuzzyGameEquivalence() {
			// Arrange
			const int MaxMinions = 7;

			var game = new Game(HeroClass.Druid, HeroClass.Warrior, PowerHistory: true);
			game.Player1.Deck.Fill();
			game.Player2.Deck.Fill();
			game.Start();

			// We create a scenario with 7 Totem Golems on one side of the board
			// and a single Boom Bot on the other side

			// Totem Golem IDs: firstId - firstId+6 inclusive
			// Boom Bot ID: firstId+7
			int firstId = -1;
			for (int i = 0; i < MaxMinions; i++) {
				int id = game.CurrentPlayer.Give("Totem Golem").Play().Id;
				if (i == 0)
					firstId = id;
			}
			game.BeginTurn();
			var boomBot = game.CurrentPlayer.Give("Boom Bot").Play() as Minion;

			// Act

			// First, kill a couple of Totem Golems directly and compare for equivalence
			var game1 = game.CloneState() as Game;
			var game2 = game.CloneState() as Game;
			var game3 = game.CloneState() as Game;

			Assert.IsTrue(game1.EquivalentTo(game2));

			// Kill first golem in first game, second golem in second game
			((Minion)game2.Entities[firstId]).Hit(4);
			((Minion)game3.Entities[firstId + 1]).Hit(4);

			// Assert

			// Make sure golems are dead
			Assert.AreEqual((int)Zone.GRAVEYARD, game2.Entities[firstId][GameTag.ZONE]);
			Assert.AreEqual((int)Zone.GRAVEYARD, game3.Entities[firstId + 1][GameTag.ZONE]);

			// Compare games
			Assert.IsTrue(game2.EquivalentTo(game3));

			// Act

			// This time, do a point of damage to two different golems
			// Game state should not be equivalent
			var game4 = game.CloneState() as Game;
			var game5 = game.CloneState() as Game;

			((Minion)game4.Entities[firstId]).Hit(1);
			((Minion)game5.Entities[firstId+1]).Hit(1);

			// Assert

			Assert.IsFalse(game4.EquivalentTo(game5));
			Assert.IsFalse(game4.PowerHistory.EquivalentTo(game5.PowerHistory));

			// Now damage the undamaged ones and this should make the game state equivalent again
			((Minion)game4.Entities[firstId+1]).Hit(1);
			((Minion)game5.Entities[firstId]).Hit(1);

			Assert.IsTrue(game4.EquivalentTo(game5));
			Assert.IsTrue(game4.PowerHistory.EquivalentTo(game5.PowerHistory));
			Assert.IsFalse(game4.PowerHistory.EquivalentTo(game5.PowerHistory, Ordered: true));

			// Act

			// Swap hand positions of a couple of cards
			var wisp1 = game4.Player1.Give("Wisp");
			var raptor1 = game4.Player1.Give("Bloodfen Raptor");
			var wisp2 = game5.Player1.Give("Wisp");
			var raptor2 = game5.Player1.Give("Bloodfen Raptor");
			var wisp2Pos = wisp2[GameTag.ZONE_POSITION];
			wisp2[GameTag.ZONE_POSITION] = raptor2[GameTag.ZONE_POSITION];
			raptor2[GameTag.ZONE_POSITION] = wisp2Pos;

			// Assert

			// Hand position should be ignored in fuzzy comparison
			Assert.IsTrue(game4.EquivalentTo(game5));
			Assert.IsTrue(game4.PowerHistory.EquivalentTo(game5.PowerHistory, IgnoreHandPosition: true));
			Assert.IsFalse(game4.PowerHistory.EquivalentTo(game5.PowerHistory, IgnoreHandPosition: false));

			// Arrange

			// Make Boom Bot death clone for every possible combination of minion choice and damage amount
			var clones = new Queue<Game>();

			game.ActionQueue.OnActionStarting += (o, e) => {
				ActionQueue queue = o as ActionQueue;
				if (e.Action is RandomChoice) {
					foreach (var entity in e.Args[RandomChoice.ENTITIES]) {
						Game cloned = (Game)e.Game.CloneState();
						cloned.ActionQueue.InsertPaused(e.Source, new LazyEntity { EntityId = entity.Id });
						cloned.ActionQueue.ProcessAll();
						if (!cloned.EquivalentTo(e.Game))
							clones.Enqueue(cloned);
						e.Cancel = true;
					}
				}
				if (e.Action is RandomAmount) {
					for (int i = e.Args[RandomAmount.MIN]; i <= e.Args[RandomAmount.MAX]; i++) {
						Game cloned = (Game)e.Game.CloneState();
						cloned.ActionQueue.InsertPaused(e.Source, new FixedNumber { Num = i });
						cloned.ActionQueue.ProcessAll();
						if (!cloned.EquivalentTo(e.Game))
							clones.Enqueue(cloned);
						e.Cancel = true;
					}
				}
			};

			// Act

			// Kill the Boom Bot and generate all possible outcomes
			boomBot.Hit(1);

			// Assert

			// There are 8 * 4 = 32 ways the Boom Bot could trigger
			// TODO: At the moment Boom Bot will not target face, so 28 combinations only
			Assert.AreEqual(28, clones.Count);

			// 7 of these states kill a minion outright, leaving 6 Totem Golems on the board
			// The entity IDs / zone positions may not match, but the game state is essentially the same
			// Therefore there are (7 * 3) + 1 + 4 = 26 or (8 * 4) - 7 + 1 = 26 unique game states
			// TODO: At the moment Boom Bot will not target face, so 22 combinations only

			HashSet<Game> fuzzyUniqueGames = new HashSet<Game>(clones, new FuzzyGameComparer());
			Assert.AreEqual(22, fuzzyUniqueGames.Count);

			// Arrange

			// Do the same thing again but with Bloodfen Raptors
			game = new Game(HeroClass.Druid, HeroClass.Warrior, PowerHistory: true);
			game.Player1.Deck.Fill();
			game.Player2.Deck.Fill();
			game.Start();

			firstId = -1;
			for (int i = 0; i < MaxMinions; i++) {
				int id = game.CurrentPlayer.Give("Bloodfen Raptor").Play().Id;
				if (i == 0)
					firstId = id;
			}
			game.BeginTurn();
			boomBot = game.CurrentPlayer.Give("Boom Bot").Play() as Minion;
			clones.Clear();

			game.ActionQueue.OnActionStarting += (o, e) => {
				ActionQueue queue = o as ActionQueue;
				if (e.Action is RandomChoice) {
					foreach (var entity in e.Args[RandomChoice.ENTITIES]) {
						Game cloned = (Game)e.Game.CloneState();
						cloned.ActionQueue.InsertPaused(e.Source, new LazyEntity { EntityId = entity.Id });
						cloned.ActionQueue.ProcessAll();
						if (!cloned.EquivalentTo(e.Game))
							clones.Enqueue(cloned);
						e.Cancel = true;
					}
				}
				if (e.Action is RandomAmount) {
					for (int i = e.Args[RandomAmount.MIN]; i <= e.Args[RandomAmount.MAX]; i++) {
						Game cloned = (Game)e.Game.CloneState();
						cloned.ActionQueue.InsertPaused(e.Source, new FixedNumber { Num = i });
						cloned.ActionQueue.ProcessAll();
						if (!cloned.EquivalentTo(e.Game))
							clones.Enqueue(cloned);
						e.Cancel = true;
					}
				}
			};

			// Act

			// Kill the Boom Bot and generate all possible outcomes
			boomBot.Hit(1);

			// Assert

			// 2 damage is enough to kill a Bloodfen Raptor, so 2, 3 or 4 damage to a given minion
			// has the same outcome. 8 possible fuzzy unique outcomes.

			Assert.AreEqual(28, clones.Count);
			fuzzyUniqueGames = new HashSet<Game>(clones, new FuzzyGameComparer());
			Assert.AreEqual(8, fuzzyUniqueGames.Count);
		}

		[Test]
		public void TestPowerHistoryEquivalence() {
			// Arrange

			// Build tree

			// Game1: Player1 <- Wisp
			var game1 = new Game(HeroClass.Druid, HeroClass.Warrior, PowerHistory: true);
			game1.Player1.Give("Wisp");

			// Game1 ==> Game2, Game3
			var game2 = game1.CloneState() as Game;
			var game3 = game1.CloneState() as Game;

			// Game2: Player 2 <- Wisp
			game2.Player2.Give("Wisp");

			// Game2 ==> Game4, Game5, Game6
			var game4 = game2.CloneState() as Game;
			var game5 = game2.CloneState() as Game;
			var game6 = game2.CloneState() as Game;

			// Game4: Player1 <- Deathwing
			game4.Player1.Give("Deathwing");

			// Game4 ==> Game7, Game8, Game9
			var game7 = game4.CloneState() as Game;
			var game8 = game4.CloneState() as Game;
			var game9 = game4.CloneState() as Game;

			// Game7: Player1 <- Boom Bot
			game7.Player1.Give("Boom Bot");

			// Game8: Player1 <- Murloc Tinyfin
			game8.Player1.Give("Murloc Tinyfin");

			// Game9: Player1 <- Knife Juggler
			game9.Player1.Give("Knife Juggler");

			// Game9 ==> Game10, Game11, Game12
			var game10 = game9.CloneState() as Game;
			var game11 = game9.CloneState() as Game;
			var game12 = game9.CloneState() as Game;

			// Game4 ==> Game13
			var game13 = game4.CloneState() as Game;

			// Game13: Player1 <- Murloc Tinyfin
			game13.Player1.Give("Murloc Tinyfin");

			// Game13 ==> Game14, Game15
			var game14 = game13.CloneState() as Game;
			var game15 = game13.CloneState() as Game;

			// Game1: P1 Wisp
			// Game2: P1 Wisp, P2 Wisp
			// Game3: P1 Wisp
			// Game4: P1 Wisp, P2 Wisp, P1 Deathwing
			// Game5: P1 Wisp, P2 Wisp
			// Game6: P1 Wisp, P2 Wisp
			// Game7: P1 Wisp, P2 Wisp, P1 Deathwing, P1 Boom Bot
			// Game8: P1 Wisp, P2 Wisp, P1 Deathwing, P1 Murloc Tinyfin
			// Game9: P1 Wisp, P2 Wisp, P1 Deathwing, P1 Knife Juggler
			// Game10: P1 Wisp, P2 Wisp, P1 Deathwing, P1 Knife Juggler
			// Game11: P1 Wisp, P2 Wisp, P1 Deathwing, P1 Knife Juggler
			// Game12: P1 Wisp, P2 Wisp, P1 Deathwing, P1 Knife Juggler
			// Game13: P1 Wisp, P2 Wisp, P1 Deathwing, P1 Murloc Tinyfin

			// Assert
			Assert.NotNull(game4.PowerHistory.DeltaSince(game2));
			Assert.NotNull(game6.PowerHistory.DeltaSince(game1));

			// Make sure that crunched deltas accumulate in the right order
			var pa = new List<PowerAction> {
				new TagChange(1, GameTag.STEP, (int)Step.MAIN_BEGIN),
				new TagChange(1, GameTag.STEP, (int)Step.MAIN_ACTION)
			};
			Assert.AreEqual(new HashSet<TagChange> { new TagChange(1, GameTag.STEP, (int)Step.MAIN_ACTION) }, game1.PowerHistory.CrunchedDelta(pa));

			// Same game, change list is equal to the exact local delta
			Assert.AreEqual(game2.PowerHistory.Delta.Count, game2.PowerHistory.DeltaSince(game2).Count);

			// Game1 does not derive from Game2
			Assert.IsNull(game1.PowerHistory.DeltaSince(game2));

			// Check multiple chains are in correct order
			var multiDelta = game12.PowerHistory.DeltaSince(game1);
			var lastCreatedEntityId = 0;
			foreach (var entry in multiDelta)
				if (entry is CreateEntity) {
					var ce = entry as CreateEntity;
					Assert.Less(lastCreatedEntityId, ce.EntityId);
					lastCreatedEntityId = ce.EntityId;
				}

			// Equivalence testing

			// Game8 is equivalent to Game13, even though Murloc Tinyfin was created in different branches
			Assert.True(game8.PowerHistory.EquivalentTo(game13.PowerHistory));

			// Game8 is different to Game12
			Assert.False(game8.PowerHistory.EquivalentTo(game12.PowerHistory));

			// Game10 is branched from Game9 but contains no changes
			Assert.True(game10.PowerHistory.EquivalentTo(game9.PowerHistory));

			// Game2 is Game2
			Assert.True(game2.PowerHistory.EquivalentTo(game2.PowerHistory));

			// Game10 and Game11 are branched from the same game and contain no changes
			Assert.True(game10.PowerHistory.EquivalentTo(game11.PowerHistory));

			// Add some tags in a different order but which result in the same game state
			game10.Step = Step.FINAL_GAMEOVER;
			game10.NextStep = Step.FINAL_WRAPUP;

			game11.NextStep = Step.FINAL_WRAPUP;
			game11.Step = Step.FINAL_GAMEOVER;

			// Tag order should be ignored, Game10 and Game11 are equivalent
			Assert.True(game10.PowerHistory.EquivalentTo(game11.PowerHistory, Ordered: false));

			// But with precise tag order, they are different
			Assert.False(game10.PowerHistory.EquivalentTo(game11.PowerHistory, Ordered: true));

			// Test the case where the LCA has been modified after branching
			// (every entry after the branch should be ignored)
			game13.Player1.Give("River Crocolisk");
			game14.Step = Step.FINAL_GAMEOVER;
			game15.Step = Step.FINAL_GAMEOVER;

			// (creating an entity and putting it in the player's hand creates 3 actions)
			Assert.AreEqual(6, game13.PowerHistory.Delta.Count);
			Assert.AreEqual(1 + 3, game14.PowerHistory.DeltaSince(game13).Count);
			Assert.AreEqual(1 + 3, game15.PowerHistory.DeltaSince(game13).Count);

			// Change a base game and its branched game at the same time with different ordering
			game9.Player1.Give("Bloodfen Raptor");
			game9.Step = Step.FINAL_GAMEOVER;

			game12.Step = Step.FINAL_GAMEOVER;
			game12.Player1.Give("Bloodfen Raptor");

			// Tag order should be ignored, Game9 and Game12 are equivalent
			Assert.True(game9.PowerHistory.EquivalentTo(game12.PowerHistory, Ordered: false));

			// But with precise tag order, they are different
			Assert.False(game9.PowerHistory.EquivalentTo(game12.PowerHistory, Ordered: true));

			// Branch from a game at two different points and test for equivalence
			game8.Player1.Give("River Crocolisk");

			var game16 = game8.CloneState() as Game;
			game16.Player1.Give("Wisp");

			game8.Player1.Give("Wisp");

			var game17 = game8.CloneState() as Game;

			game16.Player1.Give("Dr. Boom");
			game17.Player1.Give("Dr. Boom");

			Assert.True(game17.PowerHistory.EquivalentTo(game16.PowerHistory, Ordered: true));

			// Hand order should be ignored
			var game18 = game1.CloneState() as Game;
			var game19 = game1.CloneState() as Game;

			// NOTE: This doesn't ignore the entity IDs, so if we care about that, we must disable it separately
			// Here we ensure that the entities have the same IDs even though they are placed in hand in a different order
			var wisp18 = new Minion("Wisp");
			var fin18 = new Minion("Murloc Tinyfin");
			var fin19 = new Minion("Murloc Tinyfin");
			var wisp19 = new Minion("Wisp");

			game18.Player1.Hand.MoveTo(fin18, 2);
			game18.Player1.Hand.MoveTo(wisp18, 2);

			game19.Player1.Hand.MoveTo(fin19, 2);
			game19.Player1.Hand.MoveTo(wisp19, 3);

			Assert.AreEqual(2, wisp18[GameTag.ZONE_POSITION]);
			Assert.AreEqual(3, fin18[GameTag.ZONE_POSITION]);

			Assert.AreEqual(3, wisp19[GameTag.ZONE_POSITION]);
			Assert.AreEqual(2, fin19[GameTag.ZONE_POSITION]);

			Assert.True(game18.PowerHistory.EquivalentTo(game19.PowerHistory));

			// But the games are different if hand order is not ignored
			Assert.False(game18.PowerHistory.EquivalentTo(game19.PowerHistory, IgnoreHandPosition: false));
		}
	}
}