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
