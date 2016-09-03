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

			foreach(IPlayable card in p1.Hand.ToArray()) {
				card.Zone = p1.Graveyard;
			}
			foreach (IPlayable card in p2.Hand.ToArray()) {
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

			foreach (IPlayable card in p1.Hand.ToArray()) {
				card.Zone = p1.Graveyard;
			}
			foreach (IPlayable card in p2.Hand.ToArray()) {
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
			Assert.That(a1.Zone == a1.Controller.Graveyard);
			Assert.That(e1.Zone == e1.Controller.Graveyard);
			Assert.That(e2.Zone == e2.Controller.Graveyard);
			Assert.That(e3.Zone == e3.Controller.Graveyard);
		}

		[Test]
		public void TestInjuredBlademaster() {
			var game = new Game(HeroClass.Priest, HeroClass.Priest);
			game.Start(SkipMulligan: true);

			var p1 = game.CurrentPlayer;
			var p2 = game.CurrentPlayer.Opponent;

			var i1 = (ICharacter)p1.Give("Injured Blademaster").Play();

			Assert.AreEqual(4, i1.Damage);
			Assert.AreEqual(3, i1.Health);
			Assert.AreEqual(7, i1.StartingHealth);
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
			Assert.AreEqual(5, c1.StartingHealth);

			var v1 = p1.Give("Voodoo Doctor").Play(c1);
			Assert.AreEqual(1, c1.Damage);
			Assert.AreEqual(5, c1.StartingHealth);

			var v2 = p1.Give("Voodoo Doctor").Play(c1);
			Assert.AreEqual(0, c1.Damage); // This will fail if you don't automatically clamp the assignment of tags to [0, 2^31-1]
			Assert.AreEqual(5, c1.StartingHealth);
		}

		/*[Test]
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
		}*/
		// TODO: Uncomment this test when Mad Bomber is fixed

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
			game.EndTurn();
			Assert.AreEqual(0, p2.Hero.Damage + c1.Damage);
			game.EndTurn();
			Assert.AreEqual(2, p2.Hero.Damage + c1.Damage);
			game.EndTurn();
			Assert.AreEqual(2, p2.Hero.Damage + c1.Damage);
			game.EndTurn();
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

		[Test]
		public void TestBaronGeddon() {
			//TODO: When Baron Geddon is fixed, fix this test too

			var game = new Game(HeroClass.Priest, HeroClass.Priest);
			game.Start(SkipMulligan: true);

			var p1 = game.CurrentPlayer;
			var p2 = game.CurrentPlayer.Opponent;

			var c1 = (ICharacter)p1.Give("Chillwind Yeti").Play();
			var c2 = (ICharacter)p2.Give("Chillwind Yeti").Play();

			var b1 = (ICharacter)p1.Give("EX1_249").Play();
			Assert.AreEqual(0, p1.Hero.Damage);
			Assert.AreEqual(0, p2.Hero.Damage);
			Assert.AreEqual(0, c1.Damage);
			Assert.AreEqual(0, c2.Damage);
			Assert.AreEqual(0, b1.Damage);

			game.EndTurn();
			Assert.AreEqual(2, p1.Hero.Damage);
			Assert.AreEqual(2, p2.Hero.Damage);
			Assert.AreEqual(2, c1.Damage);
			Assert.AreEqual(2, c2.Damage);
			Assert.AreEqual(2, b1.Damage);

			game.EndTurn();
			Assert.AreEqual(2, p1.Hero.Damage);
			Assert.AreEqual(2, p2.Hero.Damage);
			Assert.AreEqual(2, c1.Damage);
			Assert.AreEqual(2, c2.Damage);
			Assert.AreEqual(2, b1.Damage);

			game.EndTurn();
			Assert.AreEqual(4, p1.Hero.Damage);
			Assert.AreEqual(4, p2.Hero.Damage);
			Assert.AreEqual(4, c1.Damage);
			Assert.AreEqual(4, c2.Damage);
			Assert.AreEqual(4, b1.Damage);

			game.EndTurn();
			Assert.AreEqual(4, p1.Hero.Damage);
			Assert.AreEqual(4, p2.Hero.Damage);
			Assert.AreEqual(4, c1.Damage);
			Assert.AreEqual(4, c2.Damage);
			Assert.AreEqual(4, b1.Damage);

			game.EndTurn();
			Assert.AreEqual(6, p1.Hero.Damage);
			Assert.AreEqual(6, p2.Hero.Damage);
			Assert.That(c1.Zone == c1.Controller.Graveyard);
			Assert.That(c2.Zone == c2.Controller.Graveyard);
			Assert.That(b1.Zone == b1.Controller.Graveyard);
		}

		[Test]
		public void TestWildPyromancer() {
			var game = new Game(HeroClass.Priest, HeroClass.Priest);
			game.Start(SkipMulligan: true);

			var p1 = game.CurrentPlayer;
			var p2 = game.CurrentPlayer.Opponent;

			var c1 = (ICharacter)p1.Give("Chillwind Yeti").Play();
			var c2 = (ICharacter)p2.Give("Chillwind Yeti").Play();

			var w1 = (ICharacter)p1.Give("Wild Pyromancer").Play();
			Assert.AreEqual(0, p1.Hero.Damage);
			Assert.AreEqual(0, p2.Hero.Damage);
			Assert.AreEqual(0, c1.Damage);
			Assert.AreEqual(0, c2.Damage);
			Assert.AreEqual(0, w1.Damage);

			p1.Give("Darkbomb").Play(p2.Hero);
			Assert.AreEqual(0, p1.Hero.Damage);
			Assert.AreEqual(3, p2.Hero.Damage);
			Assert.AreEqual(1, c1.Damage);
			Assert.AreEqual(1, c2.Damage);
			Assert.AreEqual(1, w1.Damage);

			var wisp = (ICharacter)p1.Give("Wisp").Play();
			Assert.AreEqual(0, p1.Hero.Damage);
			Assert.AreEqual(3, p2.Hero.Damage);
			Assert.AreEqual(1, c1.Damage);
			Assert.AreEqual(1, c2.Damage);
			Assert.AreEqual(1, w1.Damage);
			Assert.AreEqual(0, wisp.Damage);

			p1.Give("Darkbomb").Play(p2.Hero);
			Assert.AreEqual(0, p1.Hero.Damage);
			Assert.AreEqual(6, p2.Hero.Damage);
			Assert.AreEqual(2, c1.Damage);
			Assert.AreEqual(2, c2.Damage);
			Assert.That(w1.Zone == w1.Controller.Graveyard);
			Assert.That(wisp.Zone == wisp.Controller.Graveyard);

			p1.Give("Darkbomb").Play(p2.Hero);
			Assert.AreEqual(0, p1.Hero.Damage);
			Assert.AreEqual(9, p2.Hero.Damage);
			Assert.AreEqual(2, c1.Damage);
			Assert.AreEqual(2, c2.Damage);
			Assert.That(w1.Zone == w1.Controller.Graveyard);
			Assert.That(wisp.Zone == wisp.Controller.Graveyard);
		}

		[Test]
		public void TestGadgetzanAuctioneer() {
			var game = new Game(HeroClass.Rogue, HeroClass.Warlock);
			game.Player1.Deck.Fill();
			game.Player2.Deck.Fill();
			game.Start(SkipMulligan: false);

			game.Player1.Choice.Discard(new List<IEntity>());
			game.Player2.Choice.Discard(new List<IEntity>());

			var p1 = game.CurrentPlayer;
			var p2 = game.CurrentPlayer.Opponent;

			var auctioneer = new Minion("Gadgetzan Auctioneer").GiveTo(p1).Play();
			var p1CardsInHand = p1.Hand.Count;
			var p2CardsInHand = p2.Hand.Count;
			new Spell("Darkbomb").GiveTo(p1).Play(p2.Hero);
			Assert.AreEqual(p1CardsInHand + 1, p1.Hand.Count);
			Assert.AreEqual(p2CardsInHand, p2.Hand.Count);
			game.EndTurn();

			new Spell("Darkbomb").GiveTo(p2).Play(p1.Hero);
			Assert.AreEqual(p1CardsInHand + 1, p1.Hand.Count);
			Assert.AreEqual(p2CardsInHand + 1, p2.Hand.Count);
			game.EndTurn();

			new Minion("Bloodfen Raptor").GiveTo(p1).Play();
			Assert.AreEqual(p1CardsInHand + 2, p1.Hand.Count);
			Assert.AreEqual(p2CardsInHand + 1, p2.Hand.Count);
		}
	}
}
