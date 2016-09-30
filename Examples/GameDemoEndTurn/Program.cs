using System;
using Brimstone;
using Brimstone.Entities;

namespace GameDemoEndTurn
{
	class Program
	{
		static void Main(string[] args) {
			// The simplest possible game - end turn until fatigue kills someone

			// 1. Create the game
			var game = new Game(HeroClass.Druid, HeroClass.Druid);

			// 2. Fill the decks with random cards
			game.Player1.Deck.Fill();
			game.Player2.Deck.Fill();

			// 3. Start the game. A random player gets the coin and an extra card
			game.Start();

			// 4. Mulligan (keep all cards)
			game.Player1.Choice.Keep(x => true);
			game.Player2.Choice.Keep(x => true);

			// 5. End turn until someone dies
			while (game.State != GameState.COMPLETE)
				game.EndTurn();

			// 6. Find out who won
			if (game.Player1.PlayState == PlayState.WON)
				Console.WriteLine("Player 1 won!");
			else {
				Console.WriteLine("Player 2 won!");
			}

			// 7. See how many turns it took
			Console.WriteLine("Game took {0} turns", game.Turn);
		}
	}
}
