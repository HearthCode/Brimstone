using System;
using System.Collections.Generic;
using Brimstone.Cards;

namespace Brimstone.Utils
{
}

namespace Brimstone.Cards
{
	interface ICard
	{
		string Id { get; }
		string Name { get; }
	}

	interface IMinion : ICard
	{
		Game Game { get; set; }
		int Health { get; set; }
		void OnPlay();
		void OnDeath();
	}

	class GVG_096 : IMinion
	{
		public Game Game { get; set; }
		public string Id { get; } = "GVG_096";
		public string Name { get; } = "Piloted Shredder";
		public int Health { get; set; } = 3;

		public void OnPlay() {

		}
		public void OnDeath() {

		}
	}

	class AT_094 : IMinion
	{
		public Game Game { get; set; }
		public string Id { get; } = "AT_094";
		public string Name { get; } = "Flame Juggler";
		public int Health { get; set; } = 3;
		public void OnPlay() {
			Game.RandomOpponentMinion().Damage(1);
		}
		public void OnDeath() {

		}
	}
}

namespace Brimstone
{
	enum GameTag
	{
		ZONE,
		ZONE_POSITION,
		ENTITY_ID,
	}

	class ActiveMinion
	{
		public IMinion Card { get; set; }
		public Dictionary<GameTag, int> Tags { get; } = new Dictionary<GameTag, int>();

		public void Play() {
			Console.WriteLine("Player {0} is playing {1}", Card.Game.CurrentPlayer, Card.Name);
			Card.Game.CurrentPlayer.ZoneHand.Remove(this);
			Card.Game.CurrentPlayer.ZonePlay.Add(this);
			Card.OnPlay();
		}

		public void Damage(int amount) {
			Card.Health -= amount;
		}

		public override string ToString() {
			return Card.Name + " (Health=" + Card.Health + ")";
		}
	}

	class Player
	{
		public Game Game { get; set; }
		public int Id { get; set; }
		public int Health { get; } = 30;
		public List<ActiveMinion> ZoneHand { get; } = new List<ActiveMinion>();
		public List<ActiveMinion> ZonePlay { get; } = new List<ActiveMinion>();
		
		public ActiveMinion Give(IMinion card) {
			card.Game = Game;
			var minion = new ActiveMinion { Card = card };
			ZoneHand.Add(minion);
			return minion;
		}

		public override string ToString() {
			return Id.ToString();
		}
	}

	class Game
	{
		private Random rng = new Random();
		public Player Player1 { get; set; }
		public Player Player2 { get; set; }
		public Player CurrentPlayer { get; set; }
		public Player Opponent { get; set; }

		public override string ToString() {
			string s = "Board state: ";
			var players = new List<Player> { Player1, Player2 };
			foreach (var player in players) {
				s += "Player " + player.Id + " - ";
				s += "HAND: ";
				foreach (var entity in player.ZoneHand) {
					s += entity.ToString() + ", ";
				}
				s += "PLAY: ";
				foreach (var entity in player.ZonePlay) {
					s += entity.ToString() + ", ";
				}
			}
			return s;
		}

		public ActiveMinion RandomOpponentMinion() {
			var m = rng.Next(Opponent.ZonePlay.Count);
			return Opponent.ZonePlay[m];
		}
	}

	class Brimstone
	{
		public const int MaxMinions = 7;

		static void Main(string[] args) {
			Console.WriteLine("Hello Hearthstone!");

			var game = new Game();
			game.Player1 = new Player { Game = game, Id = 1 };
			game.Player2 = new Player { Game = game, Id = 2 };
			var p1 = game.Player1;
			var p2 = game.Player2;
			game.CurrentPlayer = p1;
			game.Opponent = p2;

			// Put a Piloted Shredder and Flame Juggler in each player's hand
			p1.Give(new GVG_096());
			var p1_fj = p1.Give(new AT_094());
			p2.Give(new GVG_096());
			var p2_fj = p2.Give(new AT_094());

			// Fill the board with Flame Jugglers leaving one slot each
			for (int i = 0; i < MaxMinions - 1; i++) {
				p1.ZonePlay.Add(new ActiveMinion { Card = new AT_094() });
				p2.ZonePlay.Add(new ActiveMinion { Card = new AT_094() });
			}

			// Play player 1's flame juggler
			Console.WriteLine(game);
			p1_fj.Play();
			Console.WriteLine(game);
		}
	}
}
