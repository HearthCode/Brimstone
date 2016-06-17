using System.Collections.Generic;
using NUnit.Framework;
using Brimstone;

namespace BrimstoneTests
{
	[TestFixture]
	public class TestZoneManager
	{
		[Test]
		public void TestZones() {
			// TODO
		}

		[Test]
		public void TestDeck() {
			// Arrange
			var p1 = new Player { FriendlyName = "Player 1" };
			var p2 = new Player { FriendlyName = "Player 2" };
			var game = new Game(Player1: p1, Player2: p2);

			// Act
			p1.Deck.Add(new List<Card> {
				Cards.FindByName("Bloodfen Raptor"),
				Cards.FindByName("Wisp"),
			});
			p1.Deck.Add(Cards.FindByName("Knife Juggler"));
			p1.Deck.Add(new List<Card> {
				Cards.FindByName("Murloc Tinyfin"),
				Cards.FindByName("Wisp"),
			});
			var chromaggus = new Minion(game, p1, Cards.FindByName("Chromaggus"));
			p1.Deck.Add(chromaggus);

			// Assert
			Assert.IsTrue(p1.Deck.Count == 6);

			Assert.IsTrue(p1.Deck[1].Card.Name == "Bloodfen Raptor");
			Assert.IsTrue(p1.Deck[2].Card.Name == "Wisp");
			Assert.IsTrue(p1.Deck[3].Card.Name == "Knife Juggler");
			Assert.IsTrue(p1.Deck[4].Card.Name == "Murloc Tinyfin");
			Assert.IsTrue(p1.Deck[5].Card.Name == "Wisp");
			Assert.IsTrue(p1.Deck[6].Card.Name == "Chromaggus");
			Assert.IsTrue(ReferenceEquals(p1.Deck[6], chromaggus));

			for (int i = 1; i < 6; i++)
				Assert.IsTrue(p1.Deck[i][GameTag.ZONE_POSITION] == i);
		}
	}
}