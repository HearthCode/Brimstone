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

		public Game SetupGame() {
			var game = new Game(HeroClass.Hunter, HeroClass.Warlock, PowerHistory: true);
			var p1 = game.Player1;
			var p2 = game.Player2;
			p1.Deck.Fill();
			p2.Deck.Fill();

			return game;
		}

		public void PlayGame(Game Game) {
			Game.Player1.Choice.Keep(x => true);
			Game.Player2.Choice.Keep(x => true);

			var p = Game.CurrentPlayer;

			p.Give("Acolyte of Pain").Play();
			p.Give("Acolyte of Pain").Play();
			p.Give("Whirlwind").Play();
		}
	}
}
