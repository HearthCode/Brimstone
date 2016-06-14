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
		public static CardDefs Cards = new CardDefs();
		public const int MaxMinions = 7;

		public static void PlayGame(MainWindow window) {
			var game = new Game();
			game.Player1 = new Player { Game = game, Card = Cards["Player"] };
			game.Player2 = new Player { Game = game, Card = Cards["Player"] };
			var p1 = game.Player1;
			var p2 = game.Player2;
			game.CurrentPlayer = p1;
			game.Opponent = p2;

			// Put a Piloted Shredder and Flame Juggler in each player's hand
			p1.Give(Cards.ByName("Piloted Shredder"));
			p1.Give(Cards.ByName("Flame Juggler"));
			p2.Give(Cards.ByName("Piloted Shredder"));
			p2.Give(Cards.ByName("Flame Juggler"));

			Console.WriteLine(game);

			// Fill the board with Flame Jugglers
			for (int i = 0; i < MaxMinions - 2; i++) {
				var fj = p1.Give(Cards.ByName("Flame Juggler"));
				fj.Play();
				game.ResolveQueue();
			}

			game.CurrentPlayer = p2;
			game.Opponent = p1;

			for (int i = 0; i < MaxMinions - 2; i++) {
				var fj = p2.Give(Cards.ByName("Flame Juggler"));
				fj.Play();
				game.ResolveQueue();
			}

			string ph = string.Empty;
			foreach (var p in game.PowerHistory)
				ph += p.ToString() + "\n";
			window.tbPowerHistory.Text = ph;
		}
	}
}
