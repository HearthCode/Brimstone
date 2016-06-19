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
			Game game = new Game(HeroClass.Druid, HeroClass.Druid, PowerHistory: true);
			game.Start();

			var p1 = game.CurrentPlayer;
			var p2 = game.CurrentPlayer.Opponent;

			// Add items to zones
			for (int i = 0; i < 5; i++)
				p1.Give(Cards.FromName("Wisp")).Play();
			for (int i = 0; i < 5; i++)
				p2.Give(Cards.FromName("Wisp")).Play();
			for (int i = 0; i < 7; i++)
				p2.Give(Cards.FromName("Wisp"));
			for (int i = 0; i < 7; i++)
				p2.Give(Cards.FromName("Wisp"));

			Assert.AreEqual(29, game.Entities.Count);

			// Act

			// Clone with copy-on-write
			Game clone = (Game)game.CloneState();

			// Assert

			// Games must be different
			Assert.AreNotSame(game, clone);

			// Players must be different
			Assert.AreNotSame(game.Player1, clone.Player1);
			Assert.AreNotSame(game.Player2, clone.Player2);

			// Player names and hero classes must match
			Assert.AreEqual(game.Player1.FriendlyName, clone.Player1.FriendlyName);
			Assert.AreEqual(game.Player2.FriendlyName, clone.Player2.FriendlyName);
			Assert.AreEqual(game.Player1.HeroClass, clone.Player1.HeroClass);
			Assert.AreEqual(game.Player2.HeroClass, clone.Player2.HeroClass);

			// Player assignments must match
			Assert.AreEqual(game.Player1.Id, clone.Player1.Id);
			Assert.AreEqual(game.Player2.Id, clone.Player2.Id);
			Assert.AreEqual(game.CurrentPlayer.Id, clone.CurrentPlayer.Id);
			Assert.AreEqual(game.CurrentPlayer.Opponent.Id, clone.CurrentPlayer.Opponent.Id);
			Assert.AreSame(clone.Players[0], clone.Player1);
			Assert.AreSame(clone.Players[1], clone.Player2);

			// EntitySequences must have correct owners
			Assert.AreNotSame(game.Entities, clone.Entities);
			Assert.AreEqual(game.Entities.Game, game);
			Assert.AreEqual(clone.Entities.Game, clone);

			// All entity IDs must match
			Assert.AreEqual(game.Entities.NextEntityId, clone.Entities.NextEntityId);
			Assert.AreEqual(game.Entities.Count, clone.Entities.Count);
			foreach (var id in game.Entities.Keys)
				Assert.IsTrue(clone.Entities.ContainsKey(id));

			// All entities must be different proxies
			foreach (var e in game.Entities)
				Assert.AreNotSame(e, clone.Entities[e.Id]);

			// All entities must have correct game and controllers
			foreach (var e in clone.Entities) {
				Assert.AreEqual(game.Entities[e.Id].Controller.Id, e.Controller.Id);
				Assert.AreSame(clone, e.Game);
				Assert.AreSame(clone.Entities[e.Id].Controller, e.Controller);
			}

			// PowerHistory must be empty
			Assert.AreEqual(0, clone.PowerHistory.Log.Count);

			// Queue and stack must be copied
			Assert.AreNotSame(game.ActionQueue, clone.ActionQueue);
			Assert.AreNotSame(game.ActionQueue.Queue, clone.ActionQueue.Queue);
			Assert.AreNotSame(game.ActionQueue.ResultStack, clone.ActionQueue.ResultStack);
			Assert.AreEqual(game.ActionQueue.Queue.Count, clone.ActionQueue.Queue.Count);
			Assert.AreEqual(game.ActionQueue.ResultStack.Count, clone.ActionQueue.ResultStack.Count);

			// All proxies must point to original entities
			foreach (Entity e in game.Entities)
				Assert.AreSame(e.BaseEntityData, ((Entity)clone.Entities[e.Id]).BaseEntityData);

			// All reference counts must be 2
			foreach (Entity e in game.Entities)
				Assert.AreEqual(2, e.ReferenceCount);
			foreach (Entity e in clone.Entities)
				Assert.AreEqual(2, e.ReferenceCount);

			// All zone managers must be re-created
			foreach (var z in game.Zones)
				if (z != null)
					Assert.AreNotSame(z, clone.Zones[z.Zone]);

			foreach (var p in game.Players)
				foreach (var z in p.Zones)
					if (z != null)
						Assert.AreNotSame(z, ((Player)clone.Entities[p.Id]).Zones[z.Zone]);

			// All new zones must have re-assigned game and controllers
			foreach (var z in clone.Zones) {
				if (z == null)
					continue;
				Assert.AreSame(clone, z.Game);
				Assert.AreSame(clone, z.Controller);
			}
			foreach (var p in clone.Players)
				foreach (var z in p.Zones) {
					if (z == null)
						continue;
					Assert.AreSame(clone, z.Game);
					Assert.AreSame(p, z.Controller);
				}

			// All zones must match with new proxies
			foreach (var p in game.Players)
				foreach (var z in p.Zones)
					if (z != null)
						for (int zp = 1; zp <= z.Count; zp++) {
							if (z[zp] != null) {
								Assert.AreEqual(z[zp].Id, ((Player)clone.Entities[p.Id]).Zones[z.Zone][zp].Id);
								Assert.AreNotSame(z[zp], ((Player)clone.Entities[p.Id]).Zones[z.Zone][zp]);
							}
						}
		}
	}
}