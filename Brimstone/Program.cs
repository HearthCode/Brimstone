using System;
using System.Collections.Generic;
using Brimstone.Cards;
using System.Diagnostics;
using System.Linq;
using System.IO;

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

	class GVG_110t : IMinion
	{
		public Game Game { get; set; }
		public string Id { get; } = "GVG_110t";
		public string Name { get; } = "Boom Bot";
		public int Health { get; set; } = 1;
		public void OnPlay() {
		}
		public void OnDeath() {
			var m = Game.RandomOpponentMinion();
			if (m != null)
				m.Damage(new Random().Next(1, 5));
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
		public Entity Entity { get; set; }
	}

	class TagChange : PowerAction
	{
		public GameTag Key { get; set; }
		public int? Value { get; set; }

		public override string ToString() {
			return "<" + Key.ToString() + ": " + Value + ">, ";
		}
	}

	class Entity : ICloneable
	{
		public IMinion Card { get; set; }
		public Dictionary<GameTag, int?> Tags { get; protected set; } = new Dictionary<GameTag, int?>((int)GameTag._COUNT);

		public int? this[GameTag t] {
			get {
				// Use TryGetValue for safety
				return Tags[t];
			}
			set {
				Tags[t] = value;
				Card.Game.PowerHistory.Add(new TagChange { Entity = this, Key = t, Value = value });
			}
		}

		public Entity() {
			// set all tags to zero to avoid having to check if keys exist
			// worsens memory footprint to improve performance
			//var tags = Enum.GetValues(typeof(GameTag));

			//foreach (var tagValue in tags)
				//Tags.Add((GameTag)tagValue, null);
		}

		public object Clone() {
			// Cards will never change so we can just take pointers
			var e = new Entity {
				Card = Card
			};
			// Tags should be copied (for now)
			// This works because the dictionary only uses value types!
			e.Tags = new Dictionary<GameTag, int?>(Tags);
			return e;
		}
	}

	class ActiveMinion : Entity
	{
		public int Health { get; set; }

		public ActiveMinion Play() {
			Health = Card.Health;
			Console.WriteLine("Player {0} is playing {1}", Card.Game.CurrentPlayer, Card.Name);
			Card.Game.CurrentPlayer.ZoneHand.Remove(this);
			Card.Game.CurrentPlayer.ZonePlay.Add(this);
			this[GameTag.ZONE] = (int) Zone.PLAY;
			this[GameTag.ZONE_POSITION] = Card.Game.CurrentPlayer.ZonePlay.Count;
			Card.OnPlay();
			return this;
		}

		public void Damage(int amount) {
			Console.WriteLine("{0} gets hit for {1} points of damage!", this, amount);
			Health -= amount;
			this[GameTag.DAMAGE] = Card.Health - Health;
			Card.Game.CheckForDeath(this);
		}

		public override string ToString() {
			string s = Card.Name + " (Health=" + Health + ", ";
			foreach (var tag in Tags) {
				s += tag.Key + ": " + tag.Value + ", ";
			}
			return s.Substring(0, s.Length - 2) + ")";
		}

		public new object Clone() {
			// Cards will never change so we can just take pointers
			var e = new ActiveMinion {
				Card = Card,
				Health = Health
			};
			// Tags should be copied (for now)
			// This works because the dictionary only uses value types!
			e.Tags = new Dictionary<GameTag, int?>(Tags);
			return e;
		}
	}

	class Player : Entity
	{
		public Game Game { get; set; }
		public int Id { get; set; }
		public int Health { get; private set; } = 30;
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

		public new object Clone() {
			var p = new Player {
				Game = Game,
				Id = Id,
				Health = Health
			};
			foreach (var e in ZoneHand)
				p.ZoneHand.Add((ActiveMinion) e.Clone());
			foreach (var e in ZonePlay)
				p.ZonePlay.Add((ActiveMinion) e.Clone());
			return p;
		}
	}

	class Game : Entity, ICloneable
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

		public IEnumerable<Entity> Entities() {
			yield return this;
			yield return Player1;
			yield return Player2;
			foreach (var e in Player1.ZoneHand)
				yield return e;
			foreach (var e in Player1.ZonePlay)
				yield return e;
			foreach (var e in Player2.ZoneHand)
				yield return e;
			foreach (var e in Player2.ZonePlay)
				yield return e;
		}

		public ActiveMinion RandomOpponentMinion() {
			if (Opponent.ZonePlay.Count == 0)
				return null;
			var m = rng.Next(Opponent.ZonePlay.Count);
			return Opponent.ZonePlay[m];
		}
		public void CheckForDeath(ActiveMinion minion) {
			if (minion.Health <= 0) {
				Console.WriteLine(minion + " dies!");
				Opponent.ZonePlay.Remove(minion);
				minion.Card.OnDeath();
				minion[GameTag.ZONE] = (int)Zone.GRAVEYARD;
				minion[GameTag.ZONE_POSITION] = 0;
				minion[GameTag.DAMAGE] = 0;
			}
		}

		public new Game Clone() {
			var g = new Game {
				Player1 = (Player)Player1.Clone(),
				Player2 = (Player)Player2.Clone()
			};
			// Yeah, fix this...
			g.CurrentPlayer = g.Player1;
			g.Opponent = g.Player2;
			g.Player1.Game = g;
			g.Player2.Game = g;
			foreach (var e in g.Player1.ZoneHand)
				e.Card.Game = g;
			foreach (var e in g.Player2.ZoneHand)
				e.Card.Game = g;
			foreach (var e in g.Player1.ZonePlay)
				e.Card.Game = g;
			foreach (var e in g.Player2.ZonePlay)
				e.Card.Game = g;
			return g;
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
			for (int i = 0; i < MaxMinions - 2; i++) {
				var fj = p1.Give(new AT_094());
				fj.Play();
			}

			game.CurrentPlayer = p2;
			game.Opponent = p1;

			for (int i = 0; i < MaxMinions - 2; i++) {
				var fj = p2.Give(new AT_094());
				fj.Play();
			}
			// Throw in a couple of Boom Bots
			p2.Give(new GVG_110t()).Play();
			p2.Give(new GVG_110t()).Play();

			game.CurrentPlayer = p1;
			game.Opponent = p2;

			p1.Give(new GVG_110t()).Play();
			p1.Give(new GVG_110t()).Play();

			// Set off the chain of events
			var boardStates = new Dictionary<string, int>();

			var cOut = Console.Out;
			Console.SetOut(TextWriter.Null);

			for (int i = 0; i < 100000; i++) {
				var clonedGame = game.Clone();
				var firstboombot = clonedGame.Player1.ZonePlay.First(t => t.Card.Id == "GVG_110t");
				firstboombot.Damage(1);

				var key = clonedGame.ToString();
				if (!boardStates.ContainsKey(key))
					boardStates.Add(key, 1);
				else
					boardStates[key]++;
			}
			Console.SetOut(cOut);
			Console.WriteLine("{0} board states found", boardStates.Count);


			// Check that cloning works

			var game2 = game.Clone();
			game.PowerHistory.Clear();

			Console.WriteLine(game);
			Console.WriteLine(game2);

			string gs1 = game.ToString();
			string gs2 = game2.ToString();

			System.Diagnostics.Debug.Assert(gs1.Equals(gs2));

			game.Player1.ZoneHand[0][GameTag.ZONE_POSITION] = 12345;

			gs1 = game.ToString();
			gs2 = game2.ToString();

			System.Diagnostics.Debug.Assert(!gs1.Equals(gs2));

			Console.WriteLine("Entities to clone: " + (game.Player1.ZoneHand.Count + game.Player1.ZonePlay.Count + game.Player2.ZoneHand.Count + game.Player2.ZonePlay.Count + 3));
			// Measure clonimg time
			Stopwatch s = new Stopwatch();
			s.Start();
			for (int i = 0; i < 100000; i++)
				game.Clone();
			Console.WriteLine(s.ElapsedMilliseconds + "ms for 100,000 clones");
		}
	}
}
