using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Brimstone;

namespace BrimstoneTests
{
	[TestFixture]
	public class TestClone
	{
		[Test]
		public void TestCloning([Values(true, false)] bool copyOnWrite) {
			// Arrange
			Settings.CopyOnWrite = copyOnWrite;

			// Create game with players
			Game game = new Game(HeroClass.Druid, HeroClass.Druid, PowerHistory: true);
			game.Start(SkipMulligan: true);

			var p1 = game.CurrentPlayer;
			var p2 = game.CurrentPlayer.Opponent;

			// Add items to zones
			for (int i = 0; i < 5; i++)
				p1.Give("Wisp").Play();
			for (int i = 0; i < 5; i++)
				p2.Give("Wisp").Play();
			for (int i = 0; i < 7; i++)
				p2.Give("Wisp");
			for (int i = 0; i < 7; i++)
				p2.Give("Wisp");

			Assert.AreEqual(30, game.Entities.Count);

			// Put a referenced entity on the stack
			game.Queue(game, Actions.Draw(p1));
			game.ActionQueue.ProcessOne(); // places reference to player 1 on the stack

			// Act

			// Clone with copy-on-write
			Game clone = (Game)game.CloneState();

			// Assert

			// Games must be different
			Assert.AreNotSame(game, clone);

			// Players must be different
			Assert.AreNotSame(game.Player1, clone.Player1);
			Assert.AreNotSame(game.Player2, clone.Player2);
			Assert.AreNotSame(game.Players[0], clone.Players[0]);
			Assert.AreNotSame(game.Players[1], clone.Players[1]);

			// Player names and hero classes must match
			Assert.AreEqual(game.Player1.FriendlyName, clone.Player1.FriendlyName);
			Assert.AreEqual(game.Player2.FriendlyName, clone.Player2.FriendlyName);
			Assert.AreEqual(game.Player1.HeroClass, clone.Player1.HeroClass);
			Assert.AreEqual(game.Player2.HeroClass, clone.Player2.HeroClass);

			// Player assignments must match
			Assert.AreSame(clone.Players[0], clone.Player1);
			Assert.AreSame(clone.Players[1], clone.Player2);
			Assert.AreEqual(game.Player1.Id, clone.Player1.Id);
			Assert.AreEqual(game.Player2.Id, clone.Player2.Id);
			Assert.AreEqual(game.CurrentPlayer.Id, clone.CurrentPlayer.Id);
			Assert.AreEqual(game.CurrentPlayer.Opponent.Id, clone.CurrentPlayer.Opponent.Id);

			// EntitySequences must have correct owners
			Assert.AreNotSame(game.Entities, clone.Entities);
			Assert.AreSame(game.Entities.Game, game);
			Assert.AreSame(clone.Entities.Game, clone);

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

			// PowerHistory must be linked properly
			Assert.AreEqual(game.PowerHistory.SequenceNumber, clone.PowerHistory.SequenceNumber);
			Assert.AreEqual(game.PowerHistory.SequenceNumber, clone.PowerHistory.ParentBranchEntry);
			Assert.AreEqual(game.PowerHistory.Count(), clone.PowerHistory.Count());
			Assert.AreEqual(game.PowerHistory.SequenceNumber, game.PowerHistory.Count());
			Assert.AreSame(clone, clone.PowerHistory.Game);
			Assert.AreEqual(0, clone.PowerHistory.Delta.Count);

			// Queue and stack must be copied
			Assert.AreEqual(1, game.ActionQueue.Queue.Count);
			Assert.AreEqual(1, game.ActionQueue.ResultStack.Count);

			Assert.AreNotSame(game.ActionQueue, clone.ActionQueue);
			Assert.AreNotSame(game.ActionQueue.Queue, clone.ActionQueue.Queue);
			Assert.AreNotSame(game.ActionQueue.ResultStack, clone.ActionQueue.ResultStack);
			Assert.AreEqual(game.ActionQueue.Queue.Count, clone.ActionQueue.Queue.Count);
			Assert.AreEqual(game.ActionQueue.ResultStack.Count, clone.ActionQueue.ResultStack.Count);

			while (game.ActionQueue.Queue.Count > 0) {
				var i1 = game.ActionQueue.Queue.RemoveFront();
				var i2 = clone.ActionQueue.Queue.RemoveFront();
				Assert.AreNotSame(i1, i2);
			}

			if (copyOnWrite)
				// All proxies must point to original entities
				foreach (Entity e in game.Entities)
					Assert.AreSame(e.BaseEntityData, ((Entity)clone.Entities[e.Id]).BaseEntityData);
			else
				// All proxies must point to new entities
				foreach (Entity e in game.Entities)
					Assert.AreNotSame(e.BaseEntityData, ((Entity)clone.Entities[e.Id]).BaseEntityData);

			if (copyOnWrite) {
				// All reference counts must be 2
				foreach (Entity e in game.Entities)
					Assert.AreEqual(2, e.ReferenceCount);
				foreach (Entity e in clone.Entities)
					Assert.AreEqual(2, e.ReferenceCount);
			}
			else {
				// All reference counts must be 1
				foreach (Entity e in game.Entities)
					Assert.AreEqual(1, e.ReferenceCount);
				foreach (Entity e in clone.Entities)
					Assert.AreEqual(1, e.ReferenceCount);
			}

			// All zone managers must be re-created
			foreach (var z in game.Zones)
				if (z != null)
					Assert.AreNotSame(z, clone.Zones[z.Type]);

			foreach (var p in game.Players)
				foreach (var z in p.Zones)
					if (z != null)
						Assert.AreNotSame(z, ((Player)clone.Entities[p.Id]).Zones[z.Type]);

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
								Assert.AreEqual(z[zp].Id, ((Player)clone.Entities[p.Id]).Zones[z.Type][zp].Id);
								Assert.AreNotSame(z[zp], ((Player)clone.Entities[p.Id]).Zones[z.Type][zp]);
							}
						}

			// All stack entity references must be updated
			while (game.ActionQueue.ResultStack.Count > 0) {
				var i1 = game.ActionQueue.ResultStack.Pop();
				var i2 = clone.ActionQueue.ResultStack.Pop();
				Assert.AreNotSame(i1, i2);

				List<IEntity> il1 = i1;
				List<IEntity> il2 = i2;

				if (il1 == null)
					Assert.IsNull(il2);
				else {
					Assert.AreEqual(il1.Count, il2.Count);
					for (int i = 0; i < il1.Count; i++) {
						Assert.AreNotSame(il1[i], il2[i]);
						Assert.AreEqual(il1[i].Id, il2[i].Id);
					}
				}
			}

			// TODO: Check that ActiveTriggers is cloned correctly and only once
			// TODO: Check Queue events are copied
			// TODO: Check that choices are cloned
		}

		[Test]
		public void TestCloneThreadSafety() {
			// Test that concurrent cloning updates ReferenceCount correctly
			Settings.ParallelClone = true;

			// Without Hero/FirstPlayer/CurrentPlayer set
			var game = new Game(HeroClass.Druid, HeroClass.Priest);
			game.Player1.Deck.Fill();
			game.Player2.Deck.Fill();

			var clones = game.GetClones(1000);
			foreach (Entity e in game.Entities)
				Assert.AreEqual(1001, e.ReferenceCount);

			// With everything set
			game = new Game(HeroClass.Druid, HeroClass.Priest);
			game.Player1.Deck.Fill();
			game.Player2.Deck.Fill();
			game.Start(1);

			clones = game.GetClones(1000);
			foreach (Entity e in game.Entities)
				Assert.AreEqual(1001, e.ReferenceCount);
		}
	}
}