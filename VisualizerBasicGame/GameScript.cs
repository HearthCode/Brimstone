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

			p1.Deck.Add(new List<Card> {
				"Bloodfen Raptor",
				"Wisp",
			});
			p1.Deck.Add("Knife Juggler");
			p1.Deck.Add(new List<Card> {
				"Murloc Tinyfin",
				"Wisp",
			});
			var chromaggus = new Minion("Chromaggus");
			p1.Deck.Add(chromaggus);

			p1.Deck.Fill();
			p2.Deck.Fill();

			return game;
		}

		public void PlayGame(Game Game) {
			var p1 = Game.Player1;
			var p2 = Game.Player2;

			// Do mulligan
			p1.Choice.Keep(p1.Choice.Choices.Where(x => x[GameTag.COST] <= 2));
			p2.Choice.Keep(p2.Choice.Choices.Where(x => x[GameTag.COST] <= 4));

			// Fill the board with Flame Jugglers
			for (int i = 0; i < MaxMinions - 2; i++) {
				var fj = p1.Give("Flame Juggler");
				fj.Play();
			}

			Game.BeginTurn();

			for (int i = 0; i < MaxMinions - 2; i++) {
				var fj = p2.Give("Flame Juggler");
				fj.Play();
			}

			// Throw in a couple of Boom Bots
			p2.Give("Boom Bot").Play();
			p2.Give("Boom Bot").Play();

			Game.BeginTurn();

			p1.Give("Boom Bot").Play();
			Minion boombot = p1.Give("Boom Bot").Play() as Minion;

			boombot.Hit(1);

			// Bombs away!
			p1.Give("Acolyte of Pain").Play();
			p1.Give("Whirlwind").Play();
		}
	}
}
