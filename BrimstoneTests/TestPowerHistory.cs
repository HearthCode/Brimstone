using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Brimstone;

namespace BrimstoneTests
{
	[TestFixture]
	public class TestPowerHistory
	{
		[Test]
		public void TestEquivalence() {
			// Arrange

			// Build tree

			// Game1: Player1 <- Wisp
			var game1 = new Game(HeroClass.Druid, HeroClass.Warrior, PowerHistory: true);
			game1.Player1.Give(Cards.FromName("Wisp"));

			// Game1 ==> Game2, Game3
			var game2 = game1.CloneState() as Game;
			var game3 = game1.CloneState() as Game;

			// Game2: Player 2 <- Wisp
			game2.Player2.Give(Cards.FromName("Wisp"));

			// Game2 ==> Game4, Game5, Game6
			var game4 = game2.CloneState() as Game;
			var game5 = game2.CloneState() as Game;
			var game6 = game2.CloneState() as Game;

			// Game4: Player1 <- Deathwing
			game4.Player1.Give(Cards.FromName("Deathwing"));

			// Game4 ==> Game7, Game8, Game9
			var game7 = game4.CloneState() as Game;
			var game8 = game4.CloneState() as Game;
			var game9 = game4.CloneState() as Game;

			// Game7: Player1 <- Boom Bot
			game7.Player1.Give(Cards.FromName("Boom Bot"));

			// Game8: Player1 <- Murloc Tinyfin
			game8.Player1.Give(Cards.FromName("Murloc Tinyfin"));

			// Game9: Player1 <- Knife Juggler
			game9.Player1.Give(Cards.FromName("Knife Juggler"));

			// Game9 ==> Game10, Game11, Game12
			var game10 = game9.CloneState() as Game;
			var game11 = game9.CloneState() as Game;
			var game12 = game9.CloneState() as Game;

			// Game4 ==> Game13
			var game13 = game4.CloneState() as Game;

			// Game13: Player1 <- Murloc Tinyfin
			game13.Player1.Give(Cards.FromName("Murloc Tinyfin"));

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
			Assert.True(game10.PowerHistory.EquivalentTo(game11.PowerHistory, PreciseTagOrder: false));

			// But with precise tag order, they are different
			Assert.False(game10.PowerHistory.EquivalentTo(game11.PowerHistory, PreciseTagOrder: true));

			// Test the case where the LCA has been modified after branching
			// (every entry after the branch should be ignored)
			game13.Player1.Give(Cards.FromName("River Crocolisk"));
			game14.Step = Step.FINAL_GAMEOVER;
			game15.Step = Step.FINAL_GAMEOVER;

			// (creating an entity and putting it in the player's hand creates 3 actions)
			Assert.AreEqual(6, game13.PowerHistory.Delta.Count);
			Assert.AreEqual(1 + 3, game14.PowerHistory.DeltaSince(game13).Count);
			Assert.AreEqual(1 + 3, game15.PowerHistory.DeltaSince(game13).Count);

			// Change a base game and its branched game at the same time with different ordering
			game9.Player1.Give(Cards.FromName("Bloodfen Raptor"));
			game9.Step = Step.FINAL_GAMEOVER;

			game12.Step = Step.FINAL_GAMEOVER;
			game12.Player1.Give(Cards.FromName("Bloodfen Raptor"));

			// Tag order should be ignored, Game9 and Game12 are equivalent
			Assert.True(game9.PowerHistory.EquivalentTo(game12.PowerHistory, PreciseTagOrder: false));

			// But with precise tag order, they are different
			Assert.False(game9.PowerHistory.EquivalentTo(game12.PowerHistory, PreciseTagOrder: true));

			// Branch from a game at two different points and test for equivalence
			game8.Player1.Give(Cards.FromName("River Crocolisk"));

			var game16 = game8.CloneState() as Game;
			game16.Player1.Give(Cards.FromName("Wisp"));

			game8.Player1.Give(Cards.FromName("Wisp"));

			var game17 = game8.CloneState() as Game;

			game16.Player1.Give(Cards.FromName("Dr. Boom"));
			game17.Player1.Give(Cards.FromName("Dr. Boom"));

			Assert.True(game17.PowerHistory.EquivalentTo(game16.PowerHistory, PreciseTagOrder: true));
		}
	}
}