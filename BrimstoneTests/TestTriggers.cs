using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Brimstone;

namespace BrimstoneTests
{
	[TestFixture]
	class TestTriggers
	{
		[Test]
		public void TestTriggerAttachment() {
			var game = new Game(HeroClass.Hunter, HeroClass.Warlock, PowerHistory: true);
			var p1 = game.Player1;
			var p2 = game.Player2;
			IPlayable acolyte = p1.Deck.Add(new Minion("Acolyte of Pain"));
			p1.Deck.Fill();
			p2.Deck.Fill();
			game.Start(1);

			p1.Choice.Keep(x => x.Cost <= 2);
			p2.Choice.Keep(x => x.Cost <= 2);
			var cardsInHand = p1.Hand.Count;

			// Acolyte is in deck, should not trigger
			p1.Give("Whirlwind").Play();
			Assert.That(cardsInHand == p1.Hand.Count);

			// Acolyte is in hand, should not trigger
			acolyte.Zone = p1.Hand;
			cardsInHand++;
			p1.Give("Whirlwind").Play();
			Assert.That(cardsInHand == p1.Hand.Count);

			// Acolyte is in hand, trigger should be checked but not fire
			p1.Give("War Golem").Play();
			p1.Give("Whirlwind").Play();
			Assert.That(cardsInHand == p1.Hand.Count);

			// Acolyte is on board, trigger should be checked and fire
			acolyte.Play();
			cardsInHand--;
			p1.Give("Whirlwind").Play();
			cardsInHand++;
			Assert.That(cardsInHand == p1.Hand.Count);

			// Acolyte is in graveyard, should not trigger
			acolyte.Zone = p1.Graveyard;
			p1.Give("Whirlwind").Play();
			Assert.That(cardsInHand == p1.Hand.Count);
		}
	}
}
