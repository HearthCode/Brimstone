using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using Brimstone;

namespace Test1
{
	public class Brimstone
	{
		public const int MaxMinions = 7;

		static void Main(string[] args) {
			Console.WriteLine("Hello Hearthstone!");

			// TODO: Clean up constructor order

			var p1 = new Player { FriendlyName = "Player 1" };
			var p2 = new Player { FriendlyName = "Player 2" };
			var game = new Game(Player1: p1, Player2: p2, PowerHistory: true);

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

			// TODO: Add helper functions for these
			game.ActionQueue.OnActionStarting += (o, e) => {
				ActionQueue queue = o as ActionQueue;
				if (e.Action is Damage) {
					Console.WriteLine("DAMAGE INTERCEPTED: " + e.Action);
					queue.ReplaceArg(2);
				}
			};

			game.ActionQueue.OnQueued += (o, e) => {
				ActionQueue queue = o as ActionQueue;
				if (e.Action is RandomOpponentMinion) {
					if (game.CurrentPlayer.Opponent.InPlay.Count > 0) {
						Console.WriteLine("REPLACING RANDOM CHOICE ACTION: " + e.Action);
						//queue.ReplaceAction(new LazyEntity() { Entity = (Minion)game.Opponent.Board[1] });
					}
				}
			};

			game.Start();

			// Put a Piloted Shredder and Flame Juggler in each player's hand
			p1.Give(Cards.FindByName("Piloted Shredder"));
			p1.Give(Cards.FindByName("Flame Juggler"));
			p2.Give(Cards.FindByName("Piloted Shredder"));
			p2.Give(Cards.FindByName("Flame Juggler"));

			Console.WriteLine(game);

			// Fill the board with Flame Jugglers
			for (int i = 0; i < MaxMinions - 2; i++) {
				var fj = p1.Give(Cards.FindByName("Flame Juggler"));
				fj.Play();
			}

			game.BeginTurn();

			for (int i = 0; i < MaxMinions - 2; i++) {
				var fj = p2.Give(Cards.FindByName("Flame Juggler"));
				fj.Play();
			}
			// Throw in a couple of Boom Bots
			p2.Give(Cards.FindByName("Boom Bot")).Play();
			p2.Give(Cards.FindByName("Boom Bot")).Play();

			game.BeginTurn();

			p1.Give(Cards.FindByName("Boom Bot")).Play();
			p1.Give(Cards.FindByName("Boom Bot")).Play();

			// Bombs away!
			p1.Give(Cards.FindByName("Whirlwind")).Play();


			/*
			// Set off the chain of events
			Console.WriteLine("Entities to clone: " + game.Entities.Count);

			var boardStates = new Dictionary<string, int>();

			var cOut = Console.Out;
			Console.SetOut(TextWriter.Null);

			Stopwatch sw = new Stopwatch();
			sw.Start();
			var clones = new EntityGroup<Game>(game, 100000).Entities;
			var firstboombotId = game.Player1.InPlay.First(t => t.Card.Id == "GVG_110t").Id;
			for (int i = 0; i < 100000; i++) {
				((Minion) clones[i].Entities[firstboombotId]).Damage(1);
				
				var key = clones[i].ToString();
				if (!boardStates.ContainsKey(key))
					boardStates.Add(key, 1);
				else
					boardStates[key]++;

			}
			Console.SetOut(cOut);
			Console.WriteLine("Fired off 100,000 Boom Bots in " + sw.ElapsedMilliseconds + " ms");
			Console.WriteLine("{0} board states found", boardStates.Count);
			*/
			// Check that cloning works
			
			// Normal game has 68 entities: Game + 2 players + 2 heroes + 2 hero powers + 30 cards each + coin = 68
			
			while (game.Entities.Count < 68) {
				p1.Give(Cards.FindByName("Flame Juggler"));
			}

			var game2 = game.CloneState();
			game.PowerHistory.Log.Clear();

			Console.WriteLine(game);
			Console.WriteLine(game2);

			string gs1 = game.ToString();
			string gs2 = game2.ToString();

			System.Diagnostics.Debug.Assert(gs1.Equals(gs2));

			game.Player1.Hand[1][GameTag.ZONE_POSITION] = 12345;

			gs1 = game.ToString();
			gs2 = game2.ToString();

			System.Diagnostics.Debug.Assert(!gs1.Equals(gs2));

			Console.WriteLine("Entities to clone: " + game.Entities.Count);
			// Measure clonimg time
			Stopwatch s = new Stopwatch();
			s.Start();
			for (int i = 0; i < 100000; i++)
				game.CloneState();
			Console.WriteLine(s.ElapsedMilliseconds + "ms for 100,000 clones");
			Console.ReadLine();
			
		}
	}
}
