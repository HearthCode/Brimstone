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
			var m = Game.RandomOpponentMinion();
			if (m != null)
				m.Damage(1);
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
		DAMAGE,
		_COUNT
	}

	enum Zone
	{
		PLAY = 1,
		HAND = 3,
		GRAVEYARD = 4,
		_COUNT = 3
	}

	class PowerAction
	{
		public ActiveMinion Entity { get; set; }
	}

	class TagChange : PowerAction
	{
		public GameTag Key { get; set; }
		public int? Value { get; set; }

		public override string ToString() {
			return "<" + Key.ToString() + ": " + Value + ">, ";
		}
	}

	class ActiveMinion
	{
		public IMinion Card { get; set; }
		public Dictionary<GameTag, int?> Tags { get; } = new Dictionary<GameTag, int?>((int) GameTag._COUNT);

		public int? this[GameTag t] {
			get {
				return Tags[t];
			}
			set {
				Tags[t] = value;
				Card.Game.PowerHistory.Add(new TagChange { Entity = this, Key = t, Value = value });
			}
		}

		public ActiveMinion() {
			// set all tags to zero to avoid having to check if keys exist
			// worsens memory footprint to improve performance
			var tags = Enum.GetValues(typeof(GameTag));

			foreach (var tagValue in tags)
				Tags.Add((GameTag) tagValue, null);
		}

		public void Play() {
			Console.WriteLine("Player {0} is playing {1}", Card.Game.CurrentPlayer, Card.Name);
			Card.Game.CurrentPlayer.ZoneHand.Remove(this);
			Card.Game.CurrentPlayer.ZonePlay.Add(this);
			this[GameTag.ZONE] = (int) Zone.PLAY;
			this[GameTag.ZONE_POSITION] = Card.Game.CurrentPlayer.ZonePlay.Count;
			Card.OnPlay();
		}

		public void Damage(int amount) {
			Card.Health -= amount;
			this[GameTag.DAMAGE] = 3 - Card.Health;
			Card.Game.CheckForDeath(this);
		}

		public override string ToString() {
			string s = Card.Name + " (Health=" + Card.Health + ", ";
			foreach (var tag in Tags) {
				s += tag.Value + ", ";
			}
			return s;
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
			minion[GameTag.ZONE] = (int) Zone.HAND;
			minion[GameTag.ZONE_POSITION] = ZoneHand.Count + 1;
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

		public List<PowerAction> PowerHistory = new List<PowerAction>();

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
			s += "\nPower log: ";
			foreach (var item in PowerHistory)
				s += item + "\n";
			return s;
		}

		public ActiveMinion RandomOpponentMinion() {
			if (Opponent.ZonePlay.Count == 0)
				return null;
			var m = rng.Next(Opponent.ZonePlay.Count);
			return Opponent.ZonePlay[m];
		}
		public void CheckForDeath(ActiveMinion minion) {
			if (minion.Card.Health <= 0) {
				Console.WriteLine(minion + " dies!");
				Opponent.ZonePlay.Remove(minion);
				minion.Card.OnDeath();
				minion[GameTag.ZONE] = (int)Zone.GRAVEYARD;
				minion[GameTag.ZONE_POSITION] = 0;
				minion[GameTag.DAMAGE] = 0;
			}
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
			p1.Give(new AT_094());
			p2.Give(new GVG_096());
			p2.Give(new AT_094());

			Console.WriteLine(game);

			// Fill the board with Flame Jugglers
			for (int i = 0; i < MaxMinions; i++) {
				var fj = p1.Give(new AT_094());
				fj.Play();
			}
			game.CurrentPlayer = p2;
			game.Opponent = p1;
			// Play way too many Flame Jugglers :-)
			for (int i = 0; i < 100; i++) {
				var fj = p2.Give(new AT_094());
				fj.Play();
			}
			Console.WriteLine(game);
		}
	}
}
