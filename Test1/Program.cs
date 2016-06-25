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

			var game = new Game(HeroClass.Druid, HeroClass.Druid, PowerHistory: true);
			var p1 = game.Player1;
			var p2 = game.Player2;

			p1.Deck.Add(new List<Card> {
				Cards.FromName("Bloodfen Raptor"),
				Cards.FromName("Wisp"),
			});
			p1.Deck.Add(Cards.FromName("Knife Juggler"));
			p1.Deck.Add(new List<Card> {
				Cards.FromName("Murloc Tinyfin"),
				Cards.FromName("Wisp"),
			});

			var chromaggus = new Minion(game, p1, Cards.FromName("Chromaggus"));
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
				if (e.Action is RandomChoice) {
					if (game.CurrentPlayer.Opponent.Board.Count > 0) {
						Console.WriteLine("REPLACING RANDOM CHOICE ACTION: " + e.Action);
						//queue.ReplaceAction(new LazyEntity() { Entity = (Minion)game.Opponent.Board[1] });
					}
				}
			};

			p1.Deck.Fill();
			p2.Deck.Fill();

			game.Start();

			// Put a Piloted Shredder and Flame Juggler in each player's hand
			p1.Give(Cards.FromName("Piloted Shredder"));
			p1.Give(Cards.FromName("Flame Juggler"));
			p2.Give(Cards.FromName("Piloted Shredder"));
			p2.Give(Cards.FromName("Flame Juggler"));

			Console.WriteLine(game);

			// Fill the board with Flame Jugglers
			for (int i = 0; i < MaxMinions - 2; i++) {
				var fj = p1.Give(Cards.FromName("Flame Juggler"));
				fj.Play();
			}

			game.BeginTurn();

			for (int i = 0; i < MaxMinions - 2; i++) {
				var fj = p2.Give(Cards.FromName("Flame Juggler"));
				fj.Play();
			}
			// Throw in a couple of Boom Bots
			p2.Give(Cards.FromName("Boom Bot")).Play();
			p2.Give(Cards.FromName("Boom Bot")).Play();

			game.BeginTurn();

			p1.Give(Cards.FromName("Boom Bot")).Play();
			p1.Give(Cards.FromName("Boom Bot")).Play();

			// Bombs away!
			//p1.Give(Cards.FindByName("Whirlwind")).Play();


			// Boom Bot cloning test
			Console.WriteLine("Entities to clone: " + game.Entities.Count);

			var boardStates = new Dictionary<string, int>();

			var cOut = Console.Out;
			Console.SetOut(TextWriter.Null);

			Stopwatch sw = new Stopwatch();
			sw.Start();
			var firstboombotId = game.Player1.Board.First(t => t.Card.Id == "GVG_110t").Id;
			for (int i = 0; i < 100000; i++) {
				Game cloned = (Game) game.CloneState();
				((Minion) cloned.Entities[firstboombotId]).Hit(1);
				/*
				var key = cloned.ToString();
				if (!boardStates.ContainsKey(key))
					boardStates.Add(key, 1);
				else
					boardStates[key]++;
					*/
			}
			Console.SetOut(cOut);
			Console.WriteLine("Fired off 100,000 Boom Bots in " + sw.ElapsedMilliseconds + " ms");
			Console.WriteLine("{0} board states found", boardStates.Count);
			
			// Check that cloning works
			
			// Normal game has 68 entities: Game + 2 players + 2 heroes + 2 hero powers + 30 cards each + coin = 68
			
			while (game.Entities.Count < 68) {
				p1.Give(Cards.FromName("Flame Juggler"));
			}

			var game2 = game.CloneState();
			game.PowerHistory.Log.Clear();

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
