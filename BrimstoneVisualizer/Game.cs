using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Brimstone;

namespace BrimstoneVisualizer
{
	public partial class App : Application
	{
		public const int MaxMinions = 7;

		public static void PlayGame(MainWindow window) {
			var game = new Game(PowerHistory: true);
			game.Player1 = new Player(game);
			game.Player2 = new Player(game);
			var p1 = game.Player1;
			var p2 = game.Player2;
			game.CurrentPlayer = p1;
			game.Opponent = p2;

			// Put a Piloted Shredder and Flame Juggler in each player's hand
			p1.Give(Cards.FindByName("Piloted Shredder"));
			p1.Give(Cards.FindByName("Flame Juggler"));
			p2.Give(Cards.FindByName("Piloted Shredder"));
			p2.Give(Cards.FindByName("Flame Juggler"));

			Console.WriteLine(game);

			// Fill the board with Flame Jugglers
			for (int i = 0; i < MaxMinions; i++) {
				var fj = p1.Give(Cards.FindByName("Flame Juggler"));
				fj.Play();
			}

			game.CurrentPlayer = p2;
			game.Opponent = p1;

			for (int i = 0; i < MaxMinions; i++) {
				var fj = p2.Give(Cards.FindByName("Flame Juggler"));
				fj.Play();
			}

			string ph = string.Empty;
			foreach (var p in game.PowerHistory)
				ph += p.ToString() + "\n";
			window.tbPowerHistory.Text = ph;
		}
	}
}
