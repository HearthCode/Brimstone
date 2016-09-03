using System.Collections.Generic;
using System.Linq;
using Brimstone;
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
			p1.Deck.Fill();
			p2.Deck.Fill();

			return game;
		}

		public void PlayGame(Game Game) {
			Game.Player1.Choice.Keep(x => x.Cost <= 2);
			Game.Player2.Choice.Keep(x => x.Cost <= 2);

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
