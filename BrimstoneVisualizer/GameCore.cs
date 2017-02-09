/*
	Copyright 2016, 2017 Katy Coe

	This file is part of Brimstone.

	Brimstone is free software: you can redistribute it and/or modify
	it under the terms of the GNU Lesser General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	Brimstone is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU Lesser General Public License for more details.

	You should have received a copy of the GNU Lesser General Public License
	along with Brimstone.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.Threading;
using System.Windows;
using Brimstone;
using Brimstone.Entities;

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
			Game.ActionQueue.OnActionStarting += (s, e) => {
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
