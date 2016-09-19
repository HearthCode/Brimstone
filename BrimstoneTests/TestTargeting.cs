using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Brimstone;
using Brimstone.Entities;

namespace BrimstoneTests
{
	[TestFixture]
	public class TestTargeting
	{
		[Test]
		public void TestSpellTargeting([Values(1, 2)] int playerToStart, [Values(1, 2)] int playerToAct) {
			// Arrange
			var game = new Game(HeroClass.Priest, HeroClass.Priest);
			game.Start(FirstPlayer: playerToStart, SkipMulligan: true);

			var p1 = game.CurrentPlayer;
			var p2 = game.CurrentPlayer.Opponent;

			var pAct = game.Players[playerToAct - 1];

			// Add a spell and some targets
			var spell = new Spell("Fireball").GiveTo(pAct);

			var in1 = p1.Give("Goldshire Footman").Play();
			var in2 = p1.Give("Goldshire Footman").Play();
			var in3 = p2.Give("Goldshire Footman").Play();
			var in4 = p1.Give("Stranglethorn Tiger").Play();

			var out1 = p1.Give("Faerie Dragon").Play();
			var out2 = p2.Give("Faerie Dragon").Play();
			var out3 = p2.Give("Stranglethorn Tiger").Play();

			// Act
			var targets = spell.ValidTargets;

			// Assert

			// Fireball must require a target
			Assert.True(spell.Card.RequiresTarget);

			// All heroes must be valid targets
			CollectionAssert.Contains(targets, p1.Hero);
			CollectionAssert.Contains(targets, p2.Hero);

			// All Goldshire Footmans must be valid targets
			CollectionAssert.Contains(targets, in1);
			CollectionAssert.Contains(targets, in2);
			CollectionAssert.Contains(targets, in3);

			// Our Stanglethorn Tiger must be a valid target
			if (pAct == p1)
				CollectionAssert.Contains(targets, in4);
			else
				CollectionAssert.DoesNotContain(targets, in4);

			// No Faerie Dragon must be a valid target
			CollectionAssert.DoesNotContain(targets, out1);
			CollectionAssert.DoesNotContain(targets, out2);

			// Opponent Stranglethorn Tiger must not be a valid target
			if (pAct == p1)
				CollectionAssert.DoesNotContain(targets, out3);
			else
				CollectionAssert.Contains(targets, out3);

			// No other entities must be in the valid targets list
			Assert.AreEqual(6, targets.Count());
		}

		[Test]
		public void TestFriendlyTargetWithRaceTargeting() {
			// Arrange
			var game = new Game(HeroClass.Priest, HeroClass.Priest);
			game.Start(SkipMulligan: true);

			var p1 = game.CurrentPlayer;
			var p2 = game.CurrentPlayer.Opponent;

			// Add Houndmaster to hands and add some targets
			var houndmaster = new Minion("Houndmaster").GiveTo(p1);

			var in1 = p1.Give("Bloodfen Raptor").Play();
			var in2 = p1.Give("Bloodfen Raptor").Play();

			var out1 = p1.Give("Goldshire Footman").Play();
			var out2 = p2.Give("Goldshire Footman").Play();
			var out3 = p2.Give("Bloodfen Raptor").Play();

			// Act
			var targets = houndmaster.ValidTargets;

			// Assert

			// Houndmaster must not require a target
			Assert.False(houndmaster.Card.RequiresTarget);

			// Must require a target if one is available
			Assert.True(houndmaster.Card.RequiresTargetIfAvailable);

			// All friendly Bloodfen Raptors must be valid targets
			CollectionAssert.Contains(targets, in1);
			CollectionAssert.Contains(targets, in2);

			// Other minions must not be valid targets
			CollectionAssert.DoesNotContain(targets, out1);
			CollectionAssert.DoesNotContain(targets, out2);
			CollectionAssert.DoesNotContain(targets, out3);

			// No other entities must be in the valid targets list
			Assert.AreEqual(2, targets.Count());
		}

		[Test]
		public void TestMinimumMinionsTargeting() {
			// Arrange
			var game = new Game(HeroClass.Priest, HeroClass.Priest);
			game.Start(SkipMulligan: true);

			var p1 = game.CurrentPlayer;
			var p2 = game.CurrentPlayer.Opponent;

			// Add Gormok to hand and add some targets
			var gormok = new Minion("Gormok the Impaler").GiveTo(p1);

			for (int i = 0; i < 4; i++)
				p1.Give("Wisp").Play();
			for (int i = 0; i < 6; i++)
				p2.Give("Wisp").Play();

			// Assert

			// Gormok must not require a target
			Assert.False(gormok.Card.RequiresTarget);

			// The valid targets must be exactly the whole board and heroes
			CollectionAssert.AreEquivalent(game.Characters, gormok.ValidTargets);

			// Remove a minion
			p1.Board[4].Zone = p1.Graveyard;

			// Test again
			Assert.AreEqual(0, gormok.ValidTargets.Count());
		}

		[Test]
		public void TestDragonInHandTargeting() {
			// Arrange
			var game = new Game(HeroClass.Priest, HeroClass.Priest);
			game.Start(SkipMulligan: true);

			var p1 = game.CurrentPlayer;
			var p2 = game.CurrentPlayer.Opponent;

			// Add Blackwing to hand and add some targets
			var blackwing = new Minion("Blackwing Corruptor").GiveTo(p1);

			for (int i = 0; i < 4; i++)
				p1.Give("Wisp").Play();
			for (int i = 0; i < 6; i++)
				p2.Give("Wisp").Play();

			// Assert

			// Blackwing must not require a target
			Assert.False(blackwing.Card.RequiresTarget);

			// No targets right now because requirements not met
			Assert.AreEqual(0, blackwing.ValidTargets.Count());

			// Act

			// Activate Blackwing (fulfill targeting requirements)
			p1.Give("Azure Drake");

			// Assert

			// The valid targets must be exactly the whole board and heroes
			CollectionAssert.AreEquivalent(game.Characters, blackwing.ValidTargets);
		}

		[Test]
		public void TestAttackTargetingWithoutTaunt() {
			// Arrange
			var game = new Game(HeroClass.Priest, HeroClass.Priest);
			game.Start(SkipMulligan: true);

			var p1 = game.CurrentPlayer;
			var p2 = game.CurrentPlayer.Opponent;

			// Add friendly minions
			for (int i = 0; i < 3; i++)
				p1.Give("Wisp").Play();
			var attacker = new Minion("Worgen Infiltrator").GiveTo(p1).Play();
			p1.Give("Worgen Infiltrator").Play();

			// Add enemy minions
			var validTargets = new List<IEntity>();
			for (int i = 0; i < 3; i++)
				validTargets.Add(p2.Give("Wisp").Play());
			p2.Give("Worgen Infiltrator").Play();
			validTargets.Add(p2.Hero);

			// Assert

			// Must be able to attack all opponent characters
			CollectionAssert.AreEquivalent(validTargets, attacker.ValidTargets);
		}

		[Test]
		public void TestAttackTargetingOnlyHero() {
			// Arrange
			var game = new Game(HeroClass.Priest, HeroClass.Priest);
			game.Start(SkipMulligan: true);

			var p1 = game.CurrentPlayer;
			var p2 = game.CurrentPlayer.Opponent;

			// Add friendly minions
			var attacker = new Minion("Raging Worgen").GiveTo(p1).Play();

			// Assert

			// Must be able to attack only enemy hero
			CollectionAssert.AreEquivalent(new List<IEntity> { p2.Hero }, attacker.ValidTargets);
		}

		[Test]
		public void TestAttackTargetingWithTaunt() {
			// Arrange
			var game = new Game(HeroClass.Priest, HeroClass.Priest);
			game.Start(SkipMulligan: true);

			var p1 = game.CurrentPlayer;
			var p2 = game.CurrentPlayer.Opponent;

			// Add friendly minions
			for (int i = 0; i < 3; i++)
				p1.Give("Wisp").Play();
			var attacker = new Minion("Worgen Infiltrator").GiveTo(p1).Play();

			// Add enemy minions, including two taunts and one stealthy taunt
			var validTargets = new List<IEntity>();
			validTargets.Add(p2.Give("Goldshire Footman").Play());

			for (int i = 0; i < 3; i++)
				p2.Give("Wisp").Play();
			p2.Give("Worgen Infiltrator").Play();

			validTargets.Add(p2.Give("Sludge Belcher").Play());

			var stealthTaunt = new Minion("Deathlord").GiveTo(p2).Play();
			stealthTaunt.HasStealth = true;

			// Assert

			// Must be able to attack all non-stealthy opponent taunts
			CollectionAssert.AreEquivalent(validTargets, attacker.ValidTargets);
		}
	}
}
