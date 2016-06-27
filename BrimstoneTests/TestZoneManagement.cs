using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Brimstone;

namespace BrimstoneTests
{
	[TestFixture]
	public class TestZoneManagement
	{
		[Test]
		public void TestZoneAddRemove() {
			// Arrange
			var game = new Game(HeroClass.Druid, HeroClass.Druid);
			var p1 = game.Player1;
			var p2 = game.Player2;

			// Act
			List<IEntity> items = new List<IEntity>(5);

			Assert.IsTrue(p1.Board.IsEmpty);

			// Add items to zones
			for (int i = 0; i < 5; i++)
				items.Add(p1.Board.Add(new Minion(game, p1, Cards.FromName("River Crocolisk"))));

			// Assert
			Assert.IsFalse(p1.Board.IsEmpty);
			Assert.AreEqual(5, p1.Board.Count);

			// Check zones, zone positions and references are correct
			for (int i = 1; i <= 5; i++) {
				Assert.AreEqual((int)Zone.PLAY, p1.Board[i][GameTag.ZONE]);
				Assert.AreEqual(i, p1.Board[i][GameTag.ZONE_POSITION]);
				Assert.AreSame(items[i - 1], p1.Board[i]);
			}

			// Act

			// Remove first item
			p1.Board.Remove(items[0]);

			// Assert
			Assert.AreEqual(4, p1.Board.Count);

			// Check zone positions and references are correct
			for (int i = 1; i <= 4; i++) {
				Assert.AreEqual(i, p1.Board[i][GameTag.ZONE_POSITION]);
				Assert.AreSame(items[i], p1.Board[i]);
			}

			// Check removed entity has correct zone data
			Assert.AreEqual((int)Zone.INVALID, items[0][GameTag.ZONE]);
			Assert.AreEqual(0, items[0][GameTag.ZONE_POSITION]);
		}

		[Test]
		public void TestZoneInsert() {
			// Arrange
			var game = new Game(HeroClass.Druid, HeroClass.Druid);
			var p1 = game.Player1;
			var p2 = game.Player2;

			// Act
			List<IEntity> items = new List<IEntity>(5);

			// Add items to zones
			for (int i = 0; i < 5; i++)
				items.Add(p1.Board.Add(new Minion(game, p1, Cards.FromName("River Crocolisk"))));

			// Add a new item at the middle, beginning and end and test
			var posList = new List<int> { 2, 1, 8 };
			foreach (var pos in posList) {
				var oldCount = p1.Board.Count;
				var inserted = p1.Board.Add(new Minion(game, p1, Cards.FromName("Wisp")), pos);
				items.Insert(pos - 1, inserted);
				// Assert
				Assert.AreEqual(oldCount + 1, p1.Board.Count);
				for (int i = 0; i < oldCount + 1; i++)
					Assert.AreEqual(i + 1, items[i][GameTag.ZONE_POSITION]);
			}
		}

		[Test]
		public void TestZoneMove() {
			// Arrange
			var game = new Game(HeroClass.Druid, HeroClass.Druid);
			var p1 = game.Player1;
			var p2 = game.Player2;

			// Act
			List<IEntity> items = new List<IEntity>(5);

			// Add items to zones
			for (int i = 0; i < 5; i++)
				items.Add(p1.Board.Add(new Minion(game, p1, Cards.FromName("River Crocolisk"))));

			// Move an item from the middle, beginning and end and test
			var posList = new List<int> { 3, 1, 3 };
			foreach (var pos in posList) {
				var oldPlay = p1.Board.Count;
				var oldHand = p1.Hand.Count;
				p1.Hand.MoveTo(p1.Board[pos]);

				// Assert
				Assert.AreEqual(oldPlay - 1, p1.Board.Count);
				Assert.AreEqual(oldHand + 1, p1.Hand.Count);
				for (int i = 1; i <= oldPlay - 1; i++)
					Assert.AreEqual(i, p1.Board[i][GameTag.ZONE_POSITION]);
				for (int i = 1; i <= oldHand + 1; i++) {
					Assert.AreEqual((int)Zone.HAND, p1.Hand[i][GameTag.ZONE]);
					Assert.AreEqual(i, p1.Hand[i][GameTag.ZONE_POSITION]);
				}
			}
		}

		[Test]
		public void TestZonePositionZero() {
			// Arrange
			var game = new Game(HeroClass.Druid, HeroClass.Druid);
			var p1 = game.Player1;
			var p2 = game.Player2;

			// Act
			// Add items to zones
			for (int i = 0; i < 5; i++)
				p1.Board.Add(new Minion(game, p1, Cards.FromName("River Crocolisk")));

			var item = p1.Board[1];

			// Send one to the graveyard
			p1.Graveyard.MoveTo(item);

			// Assert
			// Position 0 items shouldn't be counted
			Assert.AreEqual(0, p1.Graveyard.Count);
			Assert.AreEqual((int)Zone.GRAVEYARD, item[GameTag.ZONE]);
			Assert.AreEqual(0, item[GameTag.ZONE_POSITION]);
		}

		[Test]
		public void TestZoneSlice() {
			// Arrange
			var game = new Game(HeroClass.Druid, HeroClass.Druid);
			var p1 = game.Player1;
			var p2 = game.Player2;

			// Act
			for (int i = 0; i < 5; i++)
				p1.Deck.Add(Cards.FromName("Wisp"));

			// Assert
			List<IEntity> e;

			e = p1.Deck.Slice(2).ToList();
			Assert.AreEqual(2, e.Count);
			for (int i = 0; i < e.Count; i++)
				Assert.AreEqual(i + 1, e[i][GameTag.ZONE_POSITION]);

			e = p1.Deck.Slice(-2).ToList();
			Assert.AreEqual(2, e.Count);
			for (int i = 0; i < e.Count; i++)
				Assert.AreEqual(i + 4, e[i][GameTag.ZONE_POSITION]);

			e = p1.Deck.Slice(-3, -1).ToList();
			Assert.AreEqual(3, e.Count);
			for (int i = 0; i < e.Count; i++)
				Assert.AreEqual(i + 3, e[i][GameTag.ZONE_POSITION]);

			e = p1.Deck.Slice(2, 4).ToList();
			Assert.AreEqual(3, e.Count);
			for (int i = 0; i < e.Count; i++)
				Assert.AreEqual(i + 2, e[i][GameTag.ZONE_POSITION]);

			e = p1.Deck.Slice(3, -2).ToList();
			Assert.AreEqual(2, e.Count);
			for (int i = 0; i < e.Count; i++)
				Assert.AreEqual(i + 3, e[i][GameTag.ZONE_POSITION]);
		}

		[Test]
		public void TestDeck() {
			// Arrange
			var game = new Game(HeroClass.Druid, HeroClass.Druid);
			var p1 = game.Player1;
			var p2 = game.Player2;

			// Act
			p1.Deck.Add(new List<Card> {
				Cards.FromName("Bloodfen Raptor"),
				Cards.FromName("Wisp"),
			});
			p1.Deck.Add(Cards.FromName("River Crocolisk"));
			p1.Deck.Add(new List<Card> {
				Cards.FromName("Murloc Tinyfin"),
				Cards.FromName("Wisp"),
			});
			var chromaggus = new Minion(game, p1, Cards.FromName("Chromaggus"));
			p1.Deck.Add(chromaggus);

			// Assert
			Assert.AreEqual(6, p1.Deck.Count);

			Assert.AreEqual(p1.Deck[1].Card.Name, "Bloodfen Raptor");
			Assert.AreEqual(p1.Deck[2].Card.Name, "Wisp");
			Assert.AreEqual(p1.Deck[3].Card.Name, "River Crocolisk");
			Assert.AreEqual(p1.Deck[4].Card.Name, "Murloc Tinyfin");
			Assert.AreEqual(p1.Deck[5].Card.Name, "Wisp");
			Assert.AreEqual(p1.Deck[6].Card.Name, "Chromaggus");
			Assert.AreSame(chromaggus, p1.Deck[6]);

			for (int i = 1; i < 6; i++)
				Assert.AreEqual(i, p1.Deck[i][GameTag.ZONE_POSITION]);
		}
	}
}