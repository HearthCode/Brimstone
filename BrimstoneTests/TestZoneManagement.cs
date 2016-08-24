using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Brimstone;

namespace BrimstoneTests
{
	[TestFixture]
	public class TestZoneManagement {
		[Test]
		public void TestZoneAddRemove([Values(true, false)] bool zoneCaching) {
			// Arrange
			Settings.ZoneCaching = zoneCaching;

			var game = new Game(HeroClass.Druid, HeroClass.Druid);
			var p1 = game.Player1;
			var p2 = game.Player2;

			// Act
			List<IEntity> items = new List<IEntity>(5);

			Assert.IsTrue(p1.Board.IsEmpty);

			// Add items to zones
			for (int i = 0; i < 5; i++)
				items.Add(p1.Board.Add(new Minion("River Crocolisk")));

			// Assert
			Assert.IsFalse(p1.Board.IsEmpty);
			Assert.AreEqual(5, p1.Board.Count);

			// Check zones, zone positions and references are correct
			for (int i = 1; i <= 5; i++) {
				Assert.AreEqual(Zone.PLAY, p1.Board[i].Zone);
				Assert.AreEqual(i, p1.Board[i].ZonePosition);
				Assert.AreSame(items[i - 1], p1.Board[i]);
			}

			// Act

			// Remove first item
			p1.Board.Remove(items[0]);

			// Assert
			Assert.AreEqual(4, p1.Board.Count);

			// Check zone positions and references are correct
			for (int i = 1; i <= 4; i++) {
				Assert.AreEqual(i, p1.Board[i].ZonePosition);
				Assert.AreSame(items[i], p1.Board[i]);
			}

			// Check removed entity has correct zone data
			Assert.AreEqual(Zone.INVALID, items[0].Zone);
			Assert.AreEqual(0, items[0].ZonePosition);
		}

		[Test]
		public void TestZoneInsert([Values(true, false)] bool zoneCaching) {
			// Arrange
			Settings.ZoneCaching = zoneCaching;

			var game = new Game(HeroClass.Druid, HeroClass.Druid);
			var p1 = game.Player1;
			var p2 = game.Player2;

			// Act
			List<IEntity> items = new List<IEntity>(5);

			// Add items to zones
			for (int i = 0; i < 5; i++)
				items.Add(p1.Board.Add(new Minion("River Crocolisk")));

			// Add a new item at the middle, beginning and end and test
			var posList = new List<int> { 2, 1, 8 };
			foreach (var pos in posList) {
				var oldCount = p1.Board.Count;
				var inserted = p1.Board.Add(new Minion("Wisp"), pos);
				items.Insert(pos - 1, inserted);
				// Assert
				Assert.AreEqual(oldCount + 1, p1.Board.Count);
				for (int i = 0; i < oldCount + 1; i++)
					Assert.AreEqual(i + 1, items[i].ZonePosition);
			}
		}

		[Test]
		public void TestZoneMove([Values(true, false)] bool zoneCaching) {
			// Arrange
			Settings.ZoneCaching = zoneCaching;

			var game = new Game(HeroClass.Druid, HeroClass.Druid);
			var p1 = game.Player1;
			var p2 = game.Player2;

			// Act
			List<IEntity> items = new List<IEntity>(5);

			// Add items to zones
			for (int i = 0; i < 5; i++)
				items.Add(p1.Board.Add(new Minion("River Crocolisk")));

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
					Assert.AreEqual(i, p1.Board[i].ZonePosition);
				for (int i = 1; i <= oldHand + 1; i++) {
					Assert.AreEqual(Zone.HAND, p1.Hand[i].Zone);
					Assert.AreEqual(i, p1.Hand[i].ZonePosition);
				}
			}
		}

		[Test]
		public void TestSameZoneMove([Values(true, false)] bool zoneCaching) {
			// Arrange
			Settings.ZoneCaching = zoneCaching;

			var game = new Game(HeroClass.Druid, HeroClass.Druid);
			var p1 = game.Player1;

			// Act
			List<IEntity> items = new List<IEntity>(5);

			// Add items to zones
			for (int i = 0; i < 5; i++)
				items.Add(p1.Board.Add(new Minion("River Crocolisk")));

			// Move an item from the start to the middle
			var oldBoardCount = p1.Board.Count;
			var entity = p1.Board[1];

			entity.ZoneMove(p1.Board, 3);

			// Assert
			Assert.AreEqual(oldBoardCount, p1.Board.Count);
			for (int i = 1; i <= oldBoardCount; i++)
				Assert.AreEqual(i, p1.Board[i].ZonePosition);

			Assert.AreEqual(3, entity.ZonePosition);

			// Move it back again using the other mechanic
			entity.ZonePosition = 1;

			// Assert
			Assert.AreEqual(oldBoardCount, p1.Board.Count);
			for (int i = 1; i <= oldBoardCount; i++)
				Assert.AreEqual(i, p1.Board[i].ZonePosition);

			Assert.AreEqual(1, entity.ZonePosition);
		}

		[Test]
		public void TestZoneInPlaceSwap([Values(true, false)] bool zoneCaching) {
			// Arrange
			Settings.ZoneCaching = zoneCaching;

			var game = new Game(HeroClass.Druid, HeroClass.Druid);
			var p1 = game.Player1;

			List<IEntity> items = new List<IEntity>(5);

			// Add items to zones
			for (int i = 0; i < 5; i++)
				items.Add(p1.Hand.Add(new Minion("River Crocolisk")));

			p1.Deck.Fill();

			// Act

			// Swap an item between hand and deck
			int inHandPos = 3;
			int inDeckPos = 16;

			var inHand = p1.Hand[inHandPos];
			var inDeck = p1.Deck[inDeckPos];

			inHand.ZoneSwap(inDeck);

			// Assert
			Assert.AreEqual(5, p1.Hand.Count);
			Assert.AreEqual(30, p1.Deck.Count);

			Assert.AreEqual(inDeckPos, inHand.ZonePosition);
			Assert.AreEqual(inHandPos, inDeck.ZonePosition);

			Assert.AreSame(p1.Hand[inHandPos], inDeck);
			Assert.AreSame(p1.Deck[inDeckPos], inHand);
		}

		[Test]
		public void TestZoneIndexWrite([Values(true, false)] bool zoneCaching) {
			// Arrange
			Settings.ZoneCaching = zoneCaching;

			var game = new Game(HeroClass.Druid, HeroClass.Druid);
			var p1 = game.Player1;

			// Act
			List<IEntity> items = new List<IEntity>(5);

			// Add items to zones
			int boardSize = 5;
			int handSize = 3;

			for (int i = 0; i < boardSize; i++)
				items.Add(p1.Board.Add(new Minion("River Crocolisk")));
			for (int i = 0; i < handSize; i++)
				items.Add(p1.Hand.Add(new Minion("River Crocolisk")));

			// Test same zone move
			var entity = p1.Board[1];
			p1.Board[3] = entity;

			Assert.AreEqual(boardSize, p1.Board.Count);
			for (int i = 1; i <= boardSize; i++)
				Assert.AreEqual(i, p1.Board[i].ZonePosition);

			Assert.AreEqual(3, entity.ZonePosition);

			p1.Board[1] = entity;

			Assert.AreEqual(boardSize, p1.Board.Count);
			for (int i = 1; i <= boardSize; i++)
				Assert.AreEqual(i, p1.Board[i].ZonePosition);

			Assert.AreEqual(1, entity.ZonePosition);

			// Test different zone move
			// -1 = move to end of zone
			p1.Hand[-1] = entity;

			Assert.AreEqual(handSize + 1, p1.Hand.Count);
			for (int i = 1; i <= handSize + 1; i++)
				Assert.AreEqual(i, p1.Hand[i].ZonePosition);
			Assert.AreEqual(boardSize - 1, p1.Board.Count);
			for (int i = 1; i <= boardSize - 1; i++)
				Assert.AreEqual(i, p1.Board[i].ZonePosition);

			Assert.AreEqual(handSize + 1, entity.ZonePosition);
		}

		[Test]
		public void TestZonePositionZero([Values(true, false)] bool zoneCaching) {
			// Arrange
			Settings.ZoneCaching = zoneCaching;

			var game = new Game(HeroClass.Druid, HeroClass.Druid);
			var p1 = game.Player1;
			var p2 = game.Player2;

			// Act
			// Add items to zones
			for (int i = 0; i < 5; i++)
				p1.Board.Add(new Minion("River Crocolisk"));

			var item = p1.Board[1];

			// Send one to the graveyard
			p1.Graveyard.MoveTo(item);

			// Assert
			// Position 0 items shouldn't be counted
			Assert.AreEqual(0, p1.Graveyard.Count);
			Assert.AreEqual(Zone.GRAVEYARD, item.Zone);
			Assert.AreEqual(0, item.ZonePosition);
		}

		[Test]
		public void TestZoneSlice([Values(true, false)] bool zoneCaching) {
			// Arrange
			Settings.ZoneCaching = zoneCaching;

			var game = new Game(HeroClass.Druid, HeroClass.Druid);
			var p1 = game.Player1;
			var p2 = game.Player2;

			// Act
			for (int i = 0; i < 5; i++)
				p1.Deck.Add("Wisp");

			// Assert
			List<IEntity> e;

			e = p1.Deck.Slice(2).ToList();
			Assert.AreEqual(2, e.Count);
			for (int i = 0; i < e.Count; i++)
				Assert.AreEqual(i + 1, e[i].ZonePosition);

			e = p1.Deck.Slice(-2).ToList();
			Assert.AreEqual(2, e.Count);
			for (int i = 0; i < e.Count; i++)
				Assert.AreEqual(i + 4, e[i].ZonePosition);

			e = p1.Deck.Slice(-3, -1).ToList();
			Assert.AreEqual(3, e.Count);
			for (int i = 0; i < e.Count; i++)
				Assert.AreEqual(i + 3, e[i].ZonePosition);

			e = p1.Deck.Slice(2, 4).ToList();
			Assert.AreEqual(3, e.Count);
			for (int i = 0; i < e.Count; i++)
				Assert.AreEqual(i + 2, e[i].ZonePosition);

			e = p1.Deck.Slice(3, -2).ToList();
			Assert.AreEqual(2, e.Count);
			for (int i = 0; i < e.Count; i++)
				Assert.AreEqual(i + 3, e[i].ZonePosition);
		}

		[Test]
		public void TestDeck([Values(true, false)] bool zoneCaching) {
			// Arrange
			Settings.ZoneCaching = zoneCaching;

			var game = new Game(HeroClass.Druid, HeroClass.Druid);
			var p1 = game.Player1;
			var p2 = game.Player2;

			// Act
			p1.Deck.Add(new List<Card> {
				"Bloodfen Raptor",
				"Wisp",
			});
			p1.Deck.Add("River Crocolisk");
			p1.Deck.Add(new List<Card> {
				"Murloc Tinyfin",
				"Wisp",
			});
			var chromaggus = new Minion("Chromaggus");
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
				Assert.AreEqual(i, p1.Deck[i].ZonePosition);
		}
	}
}