using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Brimstone;

namespace BrimstoneVisualizer
{
	public partial class App
	{
		public static void PlayGame() {
			var p1 = Game.Player1;
			var p2 = Game.Player2;

			// Fill the board with Flame Jugglers
			for (int i = 0; i < MaxMinions - 2; i++) {
				var fj = p1.Give(Cards.FromName("Flame Juggler"));
				fj.Play();
			}

			Game.BeginTurn();

			for (int i = 0; i < MaxMinions - 2; i++) {
				var fj = p2.Give(Cards.FromName("Flame Juggler"));
				fj.Play();
			}

			// Throw in a couple of Boom Bots
			p2.Give(Cards.FromName("Boom Bot")).Play();
			p2.Give(Cards.FromName("Boom Bot")).Play();

			Game.BeginTurn();

			p1.Give(Cards.FromName("Boom Bot")).Play();
			p1.Give(Cards.FromName("Boom Bot")).Play();

			// Bombs away!
			p1.Give(Cards.FromName("Whirlwind")).Play();
		}
	}
}
