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

			// Build tree
			var game1 = new Game(HeroClass.Druid, HeroClass.Warrior, PowerHistory: true);
			game1.Player1.Give(Cards.FromName("Wisp"));
			var game2 = game1.CloneState() as Game;
			var game3 = game1.CloneState() as Game;
			game2.Player2.Give(Cards.FromName("Wisp"));
			var game4 = game2.CloneState() as Game;
			var game5 = game2.CloneState() as Game;
			var game6 = game2.CloneState() as Game;
			game4.Player1.Give(Cards.FromName("Deathwing"));
			var game7 = game4.CloneState() as Game;
			var game8 = game4.CloneState() as Game;
			var game9 = game4.CloneState() as Game;
			game7.Player1.Give(Cards.FromName("Boom Bot"));
			game8.Player1.Give(Cards.FromName("Murloc Tinyfin"));
			game9.Player1.Give(Cards.FromName("Knife Juggler"));
			var game10 = game9.CloneState() as Game;
			var game11 = game9.CloneState() as Game;
			var game12 = game9.CloneState() as Game;

			// same as game 8
			var game13 = game4.CloneState() as Game;
			game13.Player1.Give(Cards.FromName("Murloc Tinyfin"));

			var d1 = game4.PowerHistory.DeltaSince(game2);
			var d2 = game6.PowerHistory.DeltaSince(game1);
			var d3 = game2.PowerHistory.DeltaSince(game2);
			var d4 = game1.PowerHistory.DeltaSince(game2);
			var d5 = game12.PowerHistory.DeltaSince(game1);

			Console.WriteLine(game8.PowerHistory.EquivalentTo(game13.PowerHistory));
			Console.WriteLine(game8.PowerHistory.EquivalentTo(game12.PowerHistory));







			/*
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
						/*
						// TODO: Add helper functions for these
						game.ActionQueue.OnActionStarting += (o, e) => {
							ActionQueue queue = o as ActionQueue;
							if (e.Action is Damage) {
								Console.WriteLine("DAMAGE INTERCEPTED: " + e.Action);
								queue.ReplaceArg(2);
							}
						};

						game.ActionQueue.OnQueueing += (o, e) => {
							ActionQueue queue = o as ActionQueue;
							if (e.Action is RandomChoice) {
								if (game.CurrentPlayer.Opponent.Board.Count > 0) {
									Console.WriteLine("REPLACING RANDOM CHOICE ACTION: " + e.Action);
									queue.EnqueuePaused(e.Source, (Entity)game.CurrentPlayer.Opponent.Board[1]);
									e.Cancel = true;
								}
							}
						};

						game.ActiveTriggers.When(CardBehaviour.Damage(CardBehaviour.AllMinions), (Action<IEntity>)(g => {
							Console.WriteLine("A MINION IS ABOUT TO BE DAMAGED!");
						}));
						game.ActiveTriggers.When(CardBehaviour.Damage(CardBehaviour.AllMinions), CardBehaviour.Give(CardBehaviour.CurrentPlayer, Cards.FromName("Wisp")));

						//p1.Deck.Fill();
						//p2.Deck.Fill();

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

						//p1.Give(Cards.FromName("Acolyte of Pain")).Play();

						// Bombs away!
						//p1.Give(Cards.FromName("Whirlwind")).Play();

						// Normal game has 68 entities: Game + 2 players + 2 heroes + 2 hero powers + 30 cards each + coin = 68

						while (game.Entities.Count < 68) {
							p1.Give(Cards.FromName("Flame Juggler"));
						}
						Console.WriteLine("Entities to clone: " + game.Entities.Count);

						var cOut = Console.Out;
						var boardStates = new Dictionary<string, int>();
						var sw = new Stopwatch();

						// Boom Bot cloning test
						Console.WriteLine("Pre-Hit cloning test");
						Console.SetOut(TextWriter.Null);
						sw.Start();

						var BoomBotId = game.Player1.Board.First(t => t.Card.Id == "GVG_110t").Id;
						for (int i = 0; i < 100000; i++) {
							Game cloned = (Game)game.CloneState();
							((Minion)cloned.Entities[BoomBotId]).Hit(1);
						}

						Console.SetOut(cOut);
						Console.WriteLine("Fired off 100,000 Boom Bots in " + sw.ElapsedMilliseconds + " ms");


						Console.WriteLine("Mid-action cloning test");
						Console.SetOut(TextWriter.Null);
						sw = new Stopwatch();
						sw.Start();

						// Capture after Boom Bot has died but before Deathrattle executes
						var BoomBot = game.Player1.Board.First(t => t.Card.Id == "GVG_110t") as Minion;
						game.ActionQueue.OnAction += (o, e) => {
							ActionQueue queue = o as ActionQueue;
							if (e.Action is Death && e.Source == BoomBot) {
								for (int i = 0; i < 100000; i++) {
									Game cloned = (Game)game.CloneState();
									cloned.ActionQueue.ProcessAll();
								}
							}
						};
						BoomBot.Hit(1);

						Console.SetOut(cOut);
						Console.WriteLine("Fired off 100,000 Boom Bots in " + sw.ElapsedMilliseconds + " ms");
						//Console.WriteLine("{0} board states found", boardStates.Count);


						// Measure clonimg time
						Stopwatch s = new Stopwatch();
						s.Start();
						for (int i = 0; i < 100000; i++)
							game.CloneState();
						Console.WriteLine(s.ElapsedMilliseconds + "ms for 100,000 clones");

						*/

			// Make a game that fills the board with Boom Bots, kills one and branches every possible state
			// (including duplicate states)

			var cOut = Console.Out;

			Console.SetOut(TextWriter.Null);

			var game = new Game(HeroClass.Druid, HeroClass.Druid, PowerHistory: true);
			game.Player1.Deck.Fill();
			game.Player2.Deck.Fill();
			game.Start();

			for (int i = 0; i < MaxMinions - 3; i++)
				game.CurrentPlayer.Give(Cards.FromName("Bloodfen Raptor")).Play();
			for (int i = 0; i < 3; i++)
				game.CurrentPlayer.Give(Cards.FromName("Boom Bot")).Play();
			game.BeginTurn();
			for (int i = 0; i < MaxMinions - 3; i++)
				game.CurrentPlayer.Give(Cards.FromName("Bloodfen Raptor")).Play();
			for (int i = 0; i < 3; i++)
				game.CurrentPlayer.Give(Cards.FromName("Boom Bot")).Play();

			int clones = 0;
			int cloneDepth = 0;

			game.ActionQueue.OnActionStarting += (o, e) => {
				ActionQueue queue = o as ActionQueue;
				if (e.Action is RandomChoice) {
					foreach (var entity in e.Args[RandomChoice.ENTITIES]) {
						clones++;
						cloneDepth++;
						Game cloned = (Game)e.Game.CloneState();
						cloned.ActionQueue.InsertPaused(e.Source, new LazyEntity { EntityId = entity.Id });
						cloned.ActionQueue.ProcessAll();
						cloneDepth--;
						e.Cancel = true;
					}
				}
				if (e.Action is RandomAmount) {
					for (int i = e.Args[RandomAmount.MIN]; i <= e.Args[RandomAmount.MAX]; i++) {
						clones++;
						cloneDepth++;
						Game cloned = (Game)e.Game.CloneState();
						cloned.ActionQueue.InsertPaused(e.Source, new FixedNumber { Num = i });
						cloned.ActionQueue.ProcessAll();
						cloneDepth--;
						e.Cancel = true;
					}
				}
			};

			var sw = new Stopwatch();
			var BoomBot = game.CurrentPlayer.Board.First(t => t.Card.Id == "GVG_110t") as Minion;
			sw.Start();
			BoomBot.Hit(1);
			Console.SetOut(cOut);
			Console.WriteLine("{0} branches in {1}ms", clones, sw.ElapsedMilliseconds);
		}
	}
}
