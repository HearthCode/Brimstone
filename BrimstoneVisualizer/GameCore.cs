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

			Game.Player1 = new Player(Game) { FriendlyName = "Player 1" };
			Game.Player2 = new Player(Game) { FriendlyName = "Player 2" };
			var p1 = Game.Player1;
			var p2 = Game.Player2;
			Game.CurrentPlayer = p1;
			Game.Opponent = p2;

			PlayGame();
		}
	}
}
