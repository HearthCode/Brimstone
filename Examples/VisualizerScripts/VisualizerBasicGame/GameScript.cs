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

using Brimstone;
using Brimstone.Entities;
using BrimstoneVisualizer;

// To make a script for Brimstone Visualizer is really easy!
// Just define a type BrimstoneGameScript.BrimstoneGame which implements IBrimstoneGame
// Add references to Brimstone and BrimstoneVisualizer to your project
// Implement SetupGame() and PlayGame() as shown below - and that's it!

namespace BrimstoneGameScript
{
	public class BrimstoneGame : IBrimstoneGame
	{
		public const int MaxMinions = 7;
		private IPlayable acolyte;

		public Game SetupGame() {
			var game = new Game(HeroClass.Hunter, HeroClass.Warlock, PowerHistory: true);
			var p1 = game.Player1;
			var p2 = game.Player2;
			acolyte = p1.Deck.Add(new Minion("Acolyte of Pain"));
			for (int i = 0; i < 4; i++) {
				p1.Deck.Add("Wisp");
				p2.Deck.Add("Wisp");
			}
			p1.DisableFatigue = true;
			p2.DisableFatigue = true;
			//p1.Deck.Fill();
			//p2.Deck.Fill();

			return game;
		}

		public void PlayGame(Game Game) {
			Game.Player1.Choice.Keep(x => x.Cost <= 2);
			Game.Player2.Choice.Keep(x => x.Cost <= 2);

			for (int i = 0; i < 20; i++)
				Game.EndTurn();

			if (Game.CurrentPlayer != Game.Player1)
				Game.EndTurn();

			var cardsInHand = Game.CurrentPlayer.Hand.Count;
			// Acolyte is in deck, should not trigger
			Game.CurrentPlayer.Give("Whirlwind").Play();
			System.Diagnostics.Debug.Assert(cardsInHand == Game.CurrentPlayer.Hand.Count);

			// Acolyte is in hand, should not trigger
			acolyte.Zone = Game.CurrentPlayer.Hand;
			cardsInHand++;
			Game.CurrentPlayer.Give("Whirlwind").Play();
			System.Diagnostics.Debug.Assert(cardsInHand == Game.CurrentPlayer.Hand.Count);

			// Acolyte is in hand, trigger should be checked but not fire
			Game.CurrentPlayer.Give("War Golem").Play();
			Game.CurrentPlayer.Give("Whirlwind").Play();
			System.Diagnostics.Debug.Assert(cardsInHand == Game.CurrentPlayer.Hand.Count);

			// Acolyte is on board, trigger should be checked and fire
			acolyte.Play();
			cardsInHand--;
			Game.CurrentPlayer.Give("Whirlwind").Play();
			System.Diagnostics.Debug.Assert(cardsInHand + 1 == Game.CurrentPlayer.Hand.Count);

			// Acolyte is in graveyard, should not trigger
			acolyte.Zone = Game.CurrentPlayer.Graveyard;
			Game.CurrentPlayer.Give("Whirlwind").Play();
			System.Diagnostics.Debug.Assert(cardsInHand + 1 == Game.CurrentPlayer.Hand.Count);
		}
	}
}
