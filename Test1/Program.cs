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

			/*
						var game = new Game(HeroClass.Druid, HeroClass.Druid, PowerHistory: true);
						var p1 = game.Player1;
						var p2 = game.Player2;

						p1.Deck.Add(new List<Card> {
							"Bloodfen Raptor",
							"Wisp",
						});
						p1.Deck.Add("Knife Juggler");
						p1.Deck.Add(new List<Card> {
							"Murloc Tinyfin",
							"Wisp",
						});

						var chromaggus = new Minion(game, p1, "Chromaggus");
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
						game.ActiveTriggers.When(CardBehaviour.Damage(CardBehaviour.AllMinions), CardBehaviour.Give(CardBehaviour.CurrentPlayer, "Wisp"));

						//p1.Deck.Fill();
						//p2.Deck.Fill();

						game.Start();

						// Put a Piloted Shredder and Flame Juggler in each player's hand
						p1.Give("Piloted Shredder");
						p1.Give("Flame Juggler");
						p2.Give("Piloted Shredder");
						p2.Give("Flame Juggler");

						Console.WriteLine(game);

						// Fill the board with Flame Jugglers
						for (int i = 0; i < MaxMinions - 2; i++) {
							var fj = p1.Give("Flame Juggler");
							fj.Play();
						}

						game.BeginTurn();

						for (int i = 0; i < MaxMinions - 2; i++) {
							var fj = p2.Give("Flame Juggler");
							fj.Play();
						}
						// Throw in a couple of Boom Bots
						p2.Give("Boom Bot").Play();
						p2.Give("Boom Bot").Play();

						game.BeginTurn();

						p1.Give("Boom Bot").Play();
						p1.Give("Boom Bot").Play();

						//p1.Give("Acolyte of Pain").Play();

						// Bombs away!
						//p1.Give("Whirlwind").Play();

						// Normal game has 68 entities: Game + 2 players + 2 heroes + 2 hero powers + 30 cards each + coin = 68

						while (game.Entities.Count < 68) {
							p1.Give("Flame Juggler");
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

			for (int i = 0; i < MaxMinions - 2; i++)
				game.CurrentPlayer.Give("Bloodfen Raptor").Play();
			for (int i = 0; i < 2; i++)
				game.CurrentPlayer.Give("Boom Bot").Play();
			game.BeginTurn();
			for (int i = 0; i < MaxMinions - 2; i++)
				game.CurrentPlayer.Give("Bloodfen Raptor").Play();
			for (int i = 0; i < 2; i++)
				game.CurrentPlayer.Give("Boom Bot").Play();

			int cloneCount = 0;
			int keptClonesCount = 0;
			var fuzzyUniqueGames = new HashSet<Game>(new FuzzyGameComparer());

			game.ActionQueue.OnActionStarting += (o, e) => {
				ActionQueue queue = o as ActionQueue;
				if (e.Action is RandomChoice) {
					foreach (var entity in e.Args[RandomChoice.ENTITIES]) {
						Game cloned = (Game)e.Game.CloneState();
						cloneCount++;
						cloned.ActionQueue.InsertPaused(e.Source, new LazyEntity { EntityId = entity.Id });
						cloned.ActionQueue.ProcessAll();
						if (!cloned.EquivalentTo(e.Game)) {
							keptClonesCount++;
							fuzzyUniqueGames.Add(cloned);
						}
						e.Cancel = true;
					}
				}
				if (e.Action is RandomAmount) {
					for (int i = e.Args[RandomAmount.MIN]; i <= e.Args[RandomAmount.MAX]; i++) {
						Game cloned = (Game)e.Game.CloneState();
						cloneCount++;
						cloned.ActionQueue.InsertPaused(e.Source, new FixedNumber { Num = i });
						cloned.ActionQueue.ProcessAll();
						if (!cloned.EquivalentTo(e.Game)) {
							keptClonesCount++;
							fuzzyUniqueGames.Add(cloned);
						}
						e.Cancel = true;
					}
				}
			};

			var sw = new Stopwatch();
			var BoomBot = game.CurrentPlayer.Board.First(t => t.Card.Id == "GVG_110t") as Minion;
			sw.Start();
			BoomBot.Hit(1);
			Console.SetOut(cOut);
			Console.WriteLine("{0} branches in {1}ms", cloneCount, sw.ElapsedMilliseconds);
			Console.WriteLine("{0} intermediate clones pruned ({1} unique branches kept)", cloneCount - keptClonesCount, keptClonesCount);
			Console.WriteLine("{0} fuzzy unique game states found", fuzzyUniqueGames.Count);
		}
	}
}
