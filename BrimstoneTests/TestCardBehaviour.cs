using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using NUnit.Framework;
using Brimstone;

namespace BrimstoneTests
{
	[TestFixture]
	class TestCardBehaviour
	{
		[Test]
		public void TestAcolyteOfPain() {
			var game = new Game(HeroClass.Priest, HeroClass.Priest);
			game.Start(SkipMulligan: true);

			var p1 = game.CurrentPlayer;
			var p2 = game.CurrentPlayer.Opponent;

			for (int i = 0; i < 30; ++i) { p1.Deck.Add("Wisp"); p2.Deck.Add("Wisp"); }

			var a1 = p1.Give("Acolyte of Pain").Play();
			var a2 = p1.Give("Acolyte of Pain").Play();

			foreach(IPlayable card in p1.Hand) {
				card.Zone = p1.Graveyard;
			}
			foreach (IPlayable card in p2.Hand) {
				card.Zone = p2.Graveyard;
			}
			Assert.AreEqual(0, p1.Hand.Count);
			Assert.AreEqual(0, p2.Hand.Count);

			var w1 = p1.Give("Whirlwind").Play();
			Assert.AreEqual(2, p1.Hand.Count);
			Assert.AreEqual(0, p2.Hand.Count);

			var w2 = p1.Give("Whirlwind").Play();
			Assert.AreEqual(4, p1.Hand.Count);
			Assert.AreEqual(0, p2.Hand.Count);

			var w3 = p1.Give("Whirlwind").Play();
			Assert.AreEqual(6, p1.Hand.Count);
			Assert.AreEqual(0, p2.Hand.Count);
			Assert.That(a1.Zone.Type == Zone.GRAVEYARD);
			Assert.That(a2.Zone.Type == Zone.GRAVEYARD);

			var w4 = p1.Give("Whirlwind").Play();
			Assert.AreEqual(6, p1.Hand.Count);
			Assert.AreEqual(0, p2.Hand.Count);
		}

		[Test]
		public void TestExplosiveSheep() {
			var game = new Game(HeroClass.Priest, HeroClass.Priest);
			game.Start(SkipMulligan: true);

			var p1 = game.CurrentPlayer;
			var p2 = game.CurrentPlayer.Opponent;

			for (int i = 0; i < 30; ++i) { p1.Deck.Add("Wisp"); p2.Deck.Add("Wisp"); }

			var a1 = p1.Give("Acolyte of Pain").Play();
			var e1 = p2.Give("Explosive Sheep").Play();
			var e2 = p2.Give("Explosive Sheep").Play();
			var e3 = p2.Give("Explosive Sheep").Play();

			foreach (IPlayable card in p1.Hand) {
				card.Zone = p1.Graveyard;
			}
			foreach (IPlayable card in p2.Hand) {
				card.Zone = p2.Graveyard;
			}
			Assert.AreEqual(0, p1.Hand.Count);
			Assert.AreEqual(0, p2.Hand.Count);

			//Death Creation Step 1: e2 dies.
			//Death Phase 1: 2 damage dealt to all minions, one card drawn.
			//Death Creation Step 2: e1 dies, e3 dies.
			//Death Phase 2: (2 damage dealt to acolyte, one card drawn)*2. (AoE damage can hit mortally wounded minions. In addition, the DCS doesn't run mid Death Phase.)
			//Death Creation Step 3: Finally acolyte dies.

			var w1 = p1.Give("Fireball").Play((ICharacter)e2);
			Assert.AreEqual(3, p1.Hand.Count);
			Assert.AreEqual(0, p2.Hand.Count);
			Assert.That(a1.Zone.Type == Zone.GRAVEYARD);
			Assert.That(e1.Zone.Type == Zone.GRAVEYARD);
			Assert.That(e2.Zone.Type == Zone.GRAVEYARD);
			Assert.That(e3.Zone.Type == Zone.GRAVEYARD);
		}

		[Test]
		public void TestInjuredBlademaster() {
			var game = new Game(HeroClass.Priest, HeroClass.Priest);
			game.Start(SkipMulligan: true);

			var p1 = game.CurrentPlayer;
			var p2 = game.CurrentPlayer.Opponent;

			var i1 = (ICharacter)p1.Give("Injured Blademaster").Play();

			Assert.AreEqual(4, i1.Damage);
			Assert.AreEqual(7, i1.Health);
		}

		[Test]
		public void TestVoodooDoctor() {
			var game = new Game(HeroClass.Priest, HeroClass.Priest);
			game.Start(SkipMulligan: true);

			var p1 = game.CurrentPlayer;
			var p2 = game.CurrentPlayer.Opponent;

			var c1 = (ICharacter)p1.Give("Chillwind Yeti").Play();
			var d1 = p1.Give("Darkbomb").Play(c1);

			Assert.AreEqual(3, c1.Damage);
			Assert.AreEqual(5, c1.Health);

			var v1 = p1.Give("Voodoo Doctor").Play(c1);
			Assert.AreEqual(1, c1.Damage);
			Assert.AreEqual(5, c1.Health);

			var v2 = p1.Give("Voodoo Doctor").Play(c1);
			Assert.AreEqual(0, c1.Damage); // This will fail if you don't automatically clamp the assignment of tags to [0, 2^31-1]
			Assert.AreEqual(5, c1.Health);
		}

		[Test]
		public void TestMadBomber() {
			var game = new Game(HeroClass.Priest, HeroClass.Priest);
			game.Start(SkipMulligan: true);

			var p1 = game.CurrentPlayer;
			var p2 = game.CurrentPlayer.Opponent;

			var w1 = (ICharacter)p1.Give("Wisp").Play();
			var b1 = p1.Give("Mad Bomber").Play();

			if (w1.Zone.Type == Zone.GRAVEYARD) {
				Assert.AreEqual(2, p1.Hero.Damage + p2.Hero.Damage); //If Mad Bomber's Battlecry can select a mortally wounded minion, this will sometimes fail
			}
			else {
				Assert.AreEqual(3, p1.Hero.Damage + p2.Hero.Damage);
			}
		}

		[Test]
		public void TestDemolisher() {
			var game = new Game(HeroClass.Priest, HeroClass.Priest);
			game.Start(SkipMulligan: true);

			var p1 = game.CurrentPlayer;
			var p2 = game.CurrentPlayer.Opponent;

			for (int i = 0; i < 30; ++i) { p1.Deck.Add("Wisp"); p2.Deck.Add("Wisp"); }

			var c1 = (ICharacter)p2.Give("Chillwind Yeti").Play();
			var d1 = p1.Give("Demolisher").Play();

			Assert.AreEqual(0, p2.Hero.Damage + c1.Damage);
			game.NextTurn();
			Assert.AreEqual(0, p2.Hero.Damage + c1.Damage);
			game.NextTurn();
			Assert.AreEqual(2, p2.Hero.Damage + c1.Damage);
			game.NextTurn();
			Assert.AreEqual(2, p2.Hero.Damage + c1.Damage);
			game.NextTurn();
			Assert.AreEqual(4, p2.Hero.Damage + c1.Damage);
		}

		[Test]
		public void TestLeperGnome() {
			var game = new Game(HeroClass.Priest, HeroClass.Priest);
			game.Start(SkipMulligan: true);

			var p1 = game.CurrentPlayer;
			var p2 = game.CurrentPlayer.Opponent;

			var l1 = (ICharacter)p1.Give("Leper Gnome").Play();
			var l2 = (ICharacter)p2.Give("Leper Gnome").Play();

			Assert.AreEqual(0, p1.Hero.Damage);
			Assert.AreEqual(0, p2.Hero.Damage);
			var m1 = p1.Give("Moonfire").Play(l1);
			Assert.AreEqual(0, p1.Hero.Damage);
			Assert.AreEqual(2, p2.Hero.Damage);
			var m2 = p1.Give("Moonfire").Play(l2);
			Assert.AreEqual(2, p1.Hero.Damage);
			Assert.AreEqual(2, p2.Hero.Damage);

			var l3 = (ICharacter)p1.Give("Leper Gnome").Play();
			var l4 = (ICharacter)p2.Give("Leper Gnome").Play();

			var w1 = p1.Give("Whirlwind").Play();
			Assert.AreEqual(4, p1.Hero.Damage);
			Assert.AreEqual(4, p2.Hero.Damage);
		}
	}
}
