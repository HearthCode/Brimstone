using System.Threading;
using System.Windows;
using Brimstone;

namespace BrimstoneVisualizer
{
	public partial class App : Application
	{
		public static AutoResetEvent QueueRead = new AutoResetEvent(false);
		public static AutoResetEvent GameStarted = new AutoResetEvent(false);
		public static Game Game;
		public static Thread GameThread;
		public static IBrimstoneGame Script;

		public static void StartGame() {
			GameThread = new Thread(GameWorkerThread);
			GameThread.Start();
		}

		public static void GameWorkerThread() {
			Game = Script.SetupGame();
			Game.Start(SkipMulligan: false);
			GameStarted.Set();

			// Block every time we queue or perform action
			Game.ActionQueue.OnQueued += (s, e) => {
					QueueRead.WaitOne();
			};
			Game.ActionQueue.OnAction += (s, e) => {
				if (Game.State != GameState.COMPLETE)
					QueueRead.WaitOne();
			};

			Script.PlayGame(Game);
		}

		public static void EndGame() {
			if (GameThread != null) {
				GameThread.Abort();
			}
			Game = null;
			GameThread = null;
		}
	}
}
