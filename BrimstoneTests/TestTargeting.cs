using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using NUnit.Framework;
using Brimstone;

namespace BrimstoneTests
{
	[TestFixture]
	public class TestTargeting
	{
		[Test]
		public void TestFireballTargeting([Values(0,1)] int playerToAct) {
			// Arrange
			var game = new Game(HeroClass.Priest, HeroClass.Priest);
			game.Start(SkipMulligan: true);

			var p1 = game.CurrentPlayer;
			var p2 = game.CurrentPlayer.Opponent;		
			var pAct = game.Players[playerToAct];

			// Add a spell and some targets
			var spell = pAct.Give("Fireball");

			var in1 = p1.Give("Goldshire Footman").Play();
			var in2 = p1.Give("Goldshire Footman").Play();
			var in3 = p2.Give("Goldshire Footman").Play();

			var out1 = p1.Give("Faerie Dragon").Play();
			var out2 = p2.Give("Faerie Dragon").Play();

			// Act
			bool requiresTarget = spell.Card.RequiresTarget;
			List<IEntity> targets = ((ICanTarget)spell).ValidTargets;

			// Assert

			// Fireball must require a target
			Assert.AreEqual(true, requiresTarget);

			// All heroes must be valid targets
			CollectionAssert.Contains(targets, p1.Hero);
			CollectionAssert.Contains(targets, p2.Hero);

			// All Goldshire Footmans must be valid targets
			CollectionAssert.Contains(targets, in1);
			CollectionAssert.Contains(targets, in2);
			CollectionAssert.Contains(targets, in3);

			// No Faerie Dragon must be a valid target
			CollectionAssert.DoesNotContain(targets, out1);
			CollectionAssert.DoesNotContain(targets, out2);

			// No other entities must be in the valid targets list
			Assert.AreEqual(targets.Count, 5);
		}

		[Test]
		public void TestHoundmasterTargeting() {
			// Arrange
			var game = new Game(HeroClass.Priest, HeroClass.Priest);
			game.Start(SkipMulligan: true);

			var p1 = game.CurrentPlayer;
			var p2 = game.CurrentPlayer.Opponent;		

			// Add Houndmaster to hands and add some targets
			var houndmaster = p1.Give("Houndmaster");

			var in1 = p1.Give("Bloodfen Raptor").Play();
			var in2 = p1.Give("Bloodfen Raptor").Play();

			var out1 = p1.Give("Goldshire Footman").Play();
			var out2 = p2.Give("Goldshire Footman").Play();
			var out3 = p2.Give("Bloodfen Raptor").Play();

			// Act
			bool requiresTarget = houndmaster.Card.RequiresTarget;
			List<IEntity> targets = ((ICanTarget)houndmaster).ValidTargets;

			// Assert

			// Houndmaster must not require a target
			Assert.AreEqual(false, requiresTarget);

			// All friendly Bloodfen Raptors must be valid targets
			CollectionAssert.Contains(targets, in1);
			CollectionAssert.Contains(targets, in2);

			// Other minions must not be valid targets
			CollectionAssert.DoesNotContain(targets, out1);
			CollectionAssert.DoesNotContain(targets, out2);
			CollectionAssert.DoesNotContain(targets, out3);

			// No other entities must be in the valid targets list
			Assert.AreEqual(2, targets.Count);
		}

		[Test]
		public void TestGormokActiveTargeting() {
			// Arrange
			var game = new Game(HeroClass.Priest, HeroClass.Priest);
			game.Start(SkipMulligan: true);

			var p1 = game.CurrentPlayer;
			var p2 = game.CurrentPlayer.Opponent;		

			// Add Gormok to hand and add some targets
			var gormok = p1.Give("Gormok the Impaler");

			for (int i = 0; i < 4; i++)
				p1.Give("Wisp").Play();
			for (int i = 0; i < 6; i++)
				p2.Give("Wisp").Play();

			// Act
			bool requiresTarget = gormok.Card.RequiresTarget;
			List<IEntity> targets = ((ICanTarget)gormok).ValidTargets;

			// Assert

			// Gormok must not require a target
			Assert.AreEqual(false, requiresTarget);

			// The valid targets must be exactly the whole board and heroes
			var validTargets = p1.Board.Concat(p2.Board).Concat(new List<IEntity> { p1.Hero, p2.Hero });
			CollectionAssert.AreEquivalent(validTargets, targets);
		}

		[Test]
		public void TestGormokInactiveTargeting() {
			// Arrange
			var game = new Game(HeroClass.Priest, HeroClass.Priest);
			game.Start(SkipMulligan: true);

			var p1 = game.CurrentPlayer;
			var p2 = game.CurrentPlayer.Opponent;		

			// Add Gormok to hand and add some targets
			var gormok = p1.Give("Gormok the Impaler");

			for (int i = 0; i < 3; i++)
				p1.Give("Wisp").Play();
			for (int i = 0; i < 6; i++)
				p2.Give("Wisp").Play();

			// Act
			List<IEntity> targets = ((ICanTarget)gormok).ValidTargets;

			// Assert

			// Gormok must not have any valid targets
			Assert.AreEqual(0, targets.Count);
		}

		[Test]
		public void TestBlackwingActiveTargeting() {
			// Arrange
			var game = new Game(HeroClass.Priest, HeroClass.Priest);
			game.Start(SkipMulligan: true);

			var p1 = game.CurrentPlayer;
			var p2 = game.CurrentPlayer.Opponent;		

			// Add Blackwing to hand and add some targets
			var blackwing = p1.Give("Blackwing Corruptor");

			for (int i = 0; i < 4; i++)
				p1.Give("Wisp").Play();
			for (int i = 0; i < 6; i++)
				p2.Give("Wisp").Play();

			// Activate Blackwing
			p1.Give("Azure Drake");

			// Act
			bool requiresTarget = blackwing.Card.RequiresTarget;
			List<IEntity> targets = ((ICanTarget)blackwing).ValidTargets;

			// Assert

			// Blackwing must not require a target
			Assert.AreEqual(false, requiresTarget);

			// The valid targets must be exactly the whole board and heroes
			var validTargets = p1.Board.Concat(p2.Board).Concat(new List<IEntity> { p1.Hero, p2.Hero });
			CollectionAssert.AreEquivalent(validTargets, targets);
		}

		[Test]
		public void TestBlackwingInactiveTargeting() {
			// Arrange
			var game = new Game(HeroClass.Priest, HeroClass.Priest);
			game.Start(SkipMulligan: true);

			var p1 = game.CurrentPlayer;
			var p2 = game.CurrentPlayer.Opponent;		

			// Add Gormok to hand and add some targets
			var blackwing = p1.Give("Blackwing Corruptor");

			for (int i = 0; i < 4; i++)
				p1.Give("Wisp").Play();
			for (int i = 0; i < 6; i++)
				p2.Give("Wisp").Play();

			// Act
			List<IEntity> targets = ((ICanTarget)blackwing).ValidTargets;

			// Assert

			// Blackwing must not have any valid targets
			Assert.AreEqual(0, targets.Count);
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
			var attacker = p1.Give("Worgen Infiltrator").Play();
			p1.Give("Worgen Infiltrator").Play();

			// Add enemy minions
			var validTargets = new List<IEntity>();
			for (int i = 0; i < 3; i++)
				validTargets.Add(p2.Give("Wisp").Play());
			p2.Give("Worgen Infiltrator").Play();
			validTargets.Add(p2.Hero);

			// Act
			List<IEntity> targets = ((ICanTarget)attacker).ValidTargets;

			// Assert

			// Must be able to attack all opponent characters
			CollectionAssert.AreEquivalent(validTargets, targets);
		}

		[Test]
		public void TestAttackTargetingOnlyHero() {
			// Arrange
			var game = new Game(HeroClass.Priest, HeroClass.Priest);
			game.Start(SkipMulligan: true);

			var p1 = game.CurrentPlayer;
			var p2 = game.CurrentPlayer.Opponent;

			// Add friendly minions
			var attacker = p1.Give("Raging Worgen").Play();

			// Act
			List<IEntity> targets = ((ICanTarget)attacker).ValidTargets;

			// Assert

			// Must be able to attack only enemy hero
			CollectionAssert.AreEquivalent(new List<IEntity> { p2.Hero }, targets);
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
			var attacker = p1.Give("Worgen Infiltrator").Play();

			// Add enemy minions, including two taunts and one stealthy taunt
			var validTargets = new List<IEntity>();
			validTargets.Add(p2.Give("Goldshire Footman").Play());

			for (int i = 0; i < 3; i++)
				p2.Give("Wisp").Play();
			p2.Give("Worgen Infiltrator").Play();

			validTargets.Add(p2.Give("Sludge Belcher").Play());

			var stealthTaunt = (Minion)p2.Give("Deathlord").Play();
			stealthTaunt.HasStealth = true;

			// Act
			List<IEntity> targets = ((ICanTarget)attacker).ValidTargets;

			// Assert

			// Must be able to attack all non-stealthy opponent taunts
			CollectionAssert.AreEquivalent(validTargets, targets);
		}
	}
}
