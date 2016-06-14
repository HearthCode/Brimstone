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

			var game = new Game(PowerHistory: true);
			game.Player1 = new Player(game) { FriendlyName = "Player 1" };
			game.Player2 = new Player(game) { FriendlyName = "Player 2" };
			var p1 = game.Player1;
			var p2 = game.Player2;
			game.CurrentPlayer = p1;
			game.Opponent = p2;

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
					if (game.Opponent.ZonePlay.Count > 0) {
						Console.WriteLine("REPLACING RANDOM CHOICE ACTION: " + e.Action);
						queue.ReplaceAction(new LazyEntity() { Entity = (Minion)game.Opponent.ZonePlay[0] });
					}
				}
			};

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

			// Set off the chain of eventsy
			var boardStates = new Dictionary<string, int>();

			var cOut = Console.Out;
			Console.SetOut(TextWriter.Null);

			for (int i = 0; i < 100000; i++) {
				var clonedGame = game.Clone() as Game;
				var firstboombot = clonedGame.Player1.ZonePlay.First(t => t.Card.Id == "GVG_110t");
				firstboombot.Damage(1);

				var key = clonedGame.ToString();
				if (!boardStates.ContainsKey(key))
					boardStates.Add(key, 1);
				else
					boardStates[key]++;
			}
			Console.SetOut(cOut);
			Console.WriteLine("{0} board states found", boardStates.Count);


			// Check that cloning works

			// Normal game has 68 entities: Game + 2 players + 2 heroes + 2 hero powers + 30 cards each + coin = 68
			while (game.Entities.Count() < 68) {
				p1.Give(Cards.FindByName("Flame Juggler"));
			}

			var game2 = game.Clone();
			game.PowerHistory.Log.Clear();

			Console.WriteLine(game);
			Console.WriteLine(game2);

			string gs1 = game.ToString();
			string gs2 = game2.ToString();

			System.Diagnostics.Debug.Assert(gs1.Equals(gs2));

			game.Player1.ZoneHand[0][GameTag.ZONE_POSITION] = 12345;

			gs1 = game.ToString();
			gs2 = game2.ToString();

			System.Diagnostics.Debug.Assert(!gs1.Equals(gs2));

			Console.WriteLine("Entities to clone: " + (game.Player1.ZoneHand.Count + game.Player1.ZonePlay.Count + game.Player2.ZoneHand.Count + game.Player2.ZonePlay.Count + 3));
			// Measure clonimg time
			Stopwatch s = new Stopwatch();
			s.Start();
			for (int i = 0; i < 100000; i++)
				game.Clone();
			Console.WriteLine(s.ElapsedMilliseconds + "ms for 100,000 clones");
		}
	}
}
