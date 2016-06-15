using System.Linq;
using NUnit.Framework;
using Brimstone;

namespace BrimstoneTests
{
	[TestFixture]
	public class TestClone
	{
		[Test]
		public void TestCopyOnWriteClone() {
			// Arrange

			// Create game with players
			Player p1 = new Player() { FriendlyName = "Player 1" };
			Player p2 = new Player() { FriendlyName = "Player 2" };
			Game game = new Game(Player1: p1, Player2: p2, PowerHistory: true);
			game.Start();

			// Give the game and each player at least one tag
			game[GameTag.STEP] = (int)Step.MAIN_ACTION;
			p1[GameTag.HEALTH] = 30;
			p2[GameTag.HEALTH] = 30;

			// Add items to zones
			for (int i = 0; i < 5; i++)
				p1.Give(Cards.FindByName("Flame Juggler")).Play();
			for (int i = 0; i < 5; i++)
				p2.Give(Cards.FindByName("Flame Juggler")).Play();
			for (int i = 0; i < 7; i++)
				p2.Give(Cards.FindByName("Flame Juggler"));
			for (int i = 0; i < 7; i++)
				p2.Give(Cards.FindByName("Flame Juggler"));

			Assert.IsTrue(game.Entities.Count == 27);

			// Act

			// Clone with copy-on-write
			Game clone = (Game)game.CloneState();

			// Assert

			// Games must be different
			Assert.IsTrue(!ReferenceEquals(game, clone));

			// Players must be different
			Assert.IsTrue(!ReferenceEquals(game.Player1, clone.Player1));
			Assert.IsTrue(!ReferenceEquals(game.Player2, clone.Player2));

			// Player names must match
			Assert.IsTrue(game.Player1.FriendlyName == clone.Player1.FriendlyName);
			Assert.IsTrue(game.Player2.FriendlyName == clone.Player2.FriendlyName);

			// Player assignments must match
			Assert.IsTrue(game.Player1.Id == clone.Player1.Id);
			Assert.IsTrue(game.Player2.Id == clone.Player2.Id);
			Assert.IsTrue(game.CurrentPlayer.Id == clone.CurrentPlayer.Id);
			Assert.IsTrue(game.Opponent.Id == clone.Opponent.Id);
			Assert.IsTrue(ReferenceEquals(clone.Players[0], clone.Player1));
			Assert.IsTrue(ReferenceEquals(clone.Players[1], clone.Player2));

			// EntitySequences must have correct owners
			Assert.IsTrue(!ReferenceEquals(game.Entities, clone.Entities));
			Assert.IsTrue(game.Entities.Game == game);
			Assert.IsTrue(clone.Entities.Game == clone);

			// All entity IDs must match
			Assert.IsTrue(game.Entities.NextEntityId == clone.Entities.NextEntityId);
			Assert.IsTrue(game.Entities.Count == clone.Entities.Count);
			foreach (var id in game.Entities.Keys)
				Assert.IsTrue(clone.Entities.ContainsKey(id));

			// All entities must be different proxies
			foreach (var e in game.Entities)
				Assert.IsTrue(!ReferenceEquals(e, clone.Entities[e.Id]));

			// All entities must have correct game and controllers
			foreach (var e in clone.Entities) {
				Assert.IsTrue(e.Controller.Id == game.Entities[e.Id].Controller.Id);
				Assert.IsTrue(ReferenceEquals(e.Game, clone));
				Assert.IsTrue(ReferenceEquals(e.Controller, clone.Entities[e.Id].Controller));
			}

			// PowerHistory must be empty
			Assert.IsTrue(clone.PowerHistory.Log.Count == 0);

			// Queue and stack must be copied
			Assert.IsTrue(!ReferenceEquals(game.ActionQueue, clone.ActionQueue));
			Assert.IsTrue(!ReferenceEquals(game.ActionQueue.Queue, clone.ActionQueue.Queue));
			Assert.IsTrue(!ReferenceEquals(game.ActionQueue.ResultStack, clone.ActionQueue.ResultStack));
			Assert.IsTrue(game.ActionQueue.Queue.Count == clone.ActionQueue.Queue.Count);
			Assert.IsTrue(game.ActionQueue.ResultStack.Count == clone.ActionQueue.ResultStack.Count);

			// All proxies must point to original entities
			foreach (var e in game.Entities)
				Assert.IsTrue(ReferenceEquals(e.BaseEntityData, clone.Entities[e.Id].BaseEntityData));

			// All reference counts must be 2
			foreach (var e in game.Entities)
				Assert.IsTrue(e.ReferenceCount == 2);
			foreach (var e in clone.Entities)
				Assert.IsTrue(e.ReferenceCount == 2);

			// All zones must match with new proxies
			foreach (var p in game.Players)
				foreach (var z in p.Zones)
					if (z != null)
						for (int zp = 1; zp <= z.Count; zp++) {
							Assert.IsTrue(z[zp].Id == ((Player)clone.Entities[p.Id]).Zones[z.Zone][zp].Id);
							Assert.IsTrue(!ReferenceEquals(z[zp], ((Player)clone.Entities[p.Id]).Zones[z.Zone][zp]));
						}
		}
	}
}