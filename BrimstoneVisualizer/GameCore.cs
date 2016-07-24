using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Brimstone;

namespace BrimstoneVisualizer
{
	public partial class App : Application
	{
		public static AutoResetEvent QueueRead = new AutoResetEvent(false);
		public static Game Game;
		public static Thread GameThread;
		public const int MaxMinions = 7;

		public static void StartGame() {
			GameThread = new Thread(GameWorkerThread);
			GameThread.Start();
		}

		public static void GameWorkerThread() {
			Game = new Game(HeroClass.Hunter, HeroClass.Warlock, PowerHistory: true);

			// Block every time we queue or perform action
			Game.ActionQueue.OnQueued += (s, e) => {
				QueueRead.WaitOne();
			};
			Game.ActionQueue.OnAction += (s, e) => {
				QueueRead.WaitOne();
			};

			var p1 = Game.Player1;
			var p2 = Game.Player2;

			p1.Deck.Add(new List<Card> {
				"Bloodfen Raptor",
				"Wisp",
			});
			p1.Deck.Add("Knife Juggler");
			p1.Deck.Add(new List<Card> {
				"Murloc Tinyfin",
				"Wisp",
			});
			var chromaggus = new Minion("Chromaggus");
			p1.Deck.Add(chromaggus);

			p1.Deck.Fill();
			p2.Deck.Fill();

			Game.Start();

			PlayGame();
		}
	}
}
