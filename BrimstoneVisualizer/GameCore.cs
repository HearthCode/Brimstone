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
		public static Game Game = new Game(PowerHistory: true);
		public static Thread GameThread;
		public const int MaxMinions = 7;

		public static void StartGame() {
			GameThread = new Thread(GameWorkerThread);
			GameThread.Start();
		}

		public static void GameWorkerThread() {
			// Block every time we queue or perform action
			Game.ActionQueue.OnQueued += (s, e) => {
				QueueRead.WaitOne();
			};
			Game.ActionQueue.OnAction += (s, e) => {
				QueueRead.WaitOne();
			};

			var p1 = new Player { FriendlyName = "Player 1" };
			var p2 = new Player { FriendlyName = "Player 2" };
			Game.SetPlayers(p1, p2);

			p1.Deck.Add(new List<Card> {
				Cards.FindByName("Bloodfen Raptor"),
				Cards.FindByName("Wisp"),
			});
			p1.Deck.Add(Cards.FindByName("Knife Juggler"));
			p1.Deck.Add(new List<Card> {
				Cards.FindByName("Murloc Tinyfin"),
				Cards.FindByName("Wisp"),
			});
			var chromaggus = new Minion(Game, p1, Cards.FindByName("Chromaggus"));
			p1.Deck.Add(chromaggus);

			Game.Start();

			PlayGame();
		}
	}
}
