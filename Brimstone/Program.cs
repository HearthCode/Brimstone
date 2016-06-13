using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;

namespace Brimstone
{
	static class RNG
	{
		private static Random random = new Random();

		public static int Between(int min, int max) {
			return random.Next(min, max + 1);
		}
	}

	class Card {
		public virtual string Id { get; set; }
		public virtual string Name { get; set; }
		public virtual Dictionary<GameTag, int> Tags { get; set; }
		public virtual Behaviour Behaviour { get; set; }

		public int? this[GameTag t] {
			get {
				// Use TryGetValue for safety
				return Tags[t];
			}
		}
	}

	abstract class QueueAction {
		public Game Game { get; set; }
		public List<ActionGraph> Args { get; } = new List<ActionGraph>();
		public abstract ActionResult Run(List<ActionResult> args);

		public override string ToString() {
			return "[ACTION: " + this.GetType().Name + "]";
		}
	}

	struct ActionResult {
		private bool hasValue;
		private bool hasBoolValue;
		private bool hasIntValue;
		private bool hasEntityValue;

		private bool boolValue;
		private int intValue;
		private Entity entityValue;

		public bool HasResult { get { return hasValue; } }

		public static implicit operator ActionResult(int x) {
			return new ActionResult { hasValue = true, hasIntValue = true, intValue = x };
		}
		public static implicit operator ActionResult(bool x) {
			return new ActionResult { hasValue = true, hasBoolValue = true, boolValue = x };
		}
		public static implicit operator ActionResult(Entity x) {
			return new ActionResult { hasValue = true, hasEntityValue = true, entityValue = x };
		}
		public static implicit operator int(ActionResult a) {
			return a.intValue;
		}
		public static implicit operator bool(ActionResult a) {
			return a.boolValue;
		}
		public static implicit operator Entity(ActionResult a) {
			return a.entityValue;
		}
		public static bool operator ==(ActionResult x, ActionResult y) {
			if (ReferenceEquals(x, y))
				return true;
			// Also deals with one-sided null comparisons since it will use struct value type defaults
			return (x.boolValue == y.boolValue && x.intValue == y.intValue && x.entityValue == y.entityValue);
		}
		public static bool operator !=(ActionResult x, ActionResult y) {
			return !(x == y);
		}

		public override bool Equals(object o) {
			try {
				return (bool)(this == (ActionResult)o);
			}
			catch {
				return false;
			}
		}

		public override int GetHashCode() {
			return ToString().GetHashCode();
		}

		public static ActionResult None = new ActionResult();

		public override string ToString() {
			if (!hasValue)
				return "<none>";
			else if (hasIntValue)
				return "<int: " + intValue + ">";
			else if (hasBoolValue)
				return "<bool: " + boolValue.ToString() + ">";
			else if (hasEntityValue)
				return "<Entity: " + entityValue + ">";
			else
				return "<unknown>";
		}
	}

	class ActionGraph
	{
		private List<QueueAction> graph = new List<QueueAction>();

		public ActionGraph(QueueAction q) {
			graph.Add(q);
		}

		// Convert single QueueAction to ActionGraph
		public static implicit operator ActionGraph(QueueAction q) {
			return new ActionGraph(q);
		}

		public ActionGraph Then(ActionGraph act) {
			graph.AddRange(act.graph);
			return this;
		}

		// Convert ints to actions
		public static implicit operator ActionGraph(int x) {
			return new FixedNumber { Num = x };
		}

		// Add the graph to the game's action queue
		public void Queue(Game game) {
			foreach (var action in graph) {
				foreach (var arg in action.Args)
					arg.Queue(game);
				game.ActionQueue.Enqueue(action);
				action.Game = game;
			}
		}
	}

	class FixedNumber : QueueAction
	{
		public int Num { get; set; }

		public override ActionResult Run(List<ActionResult> args) {
			return Num;
		}
	}

	class RandomOpponentMinion : QueueAction {
		public override ActionResult Run(List<ActionResult> args) {
			if (Game.Opponent.ZonePlay.Count == 0)
				return ActionResult.None;
			var m = new Random().Next(Game.Opponent.ZonePlay.Count);
			return Game.Opponent.ZonePlay[m];
		}
	}

	class RandomAmount : QueueAction
	{
		public override ActionResult Run(List<ActionResult> args) {
			return RNG.Between(args[0], args[1]);
		}
	}

	class Damage : QueueAction
	{
		private const int TARGET = 0;
		private const int DAMAGE = 1;

		public override ActionResult Run(List<ActionResult> args) {
			if (args[TARGET] != null)
				((Minion) args[TARGET]).Damage(args[DAMAGE]);
			return ActionResult.None;
		}
	}

	class Behaviour {
		// Defaulting to null for unimplemented cards or actions
		public ActionGraph Play;
		public ActionGraph Death;
	}

	partial class CardBehaviour {
		// Factory functions for DSL syntax
		public static ActionGraph RandomOpponentMinion { get { return new RandomOpponentMinion(); } }
		public static ActionGraph RandomAmount(ActionGraph min, ActionGraph max) { return new RandomAmount { Args = { min, max } }; }
		public static ActionGraph Damage(ActionGraph target, ActionGraph amount) { return new Damage { Args = { target, amount } }; }
	}

	partial class CardBehaviour
	{
		// Flame Juggler
		public static Behaviour AT_094 = new Behaviour {
			Play = Damage(RandomOpponentMinion, 1)
		};

		// Boom Bot
		public static Behaviour GVG_110t = new Behaviour {
			Death = Damage(RandomOpponentMinion, RandomAmount(1, 4))
		};
	}

	// Let's pretend this crap is XML or whatever
	class GVG_096 : Card
	{
		public override string Id { get; set; } = "GVG_096";
		public override string Name { get; set; } = "Piloted Shredder";
		public override Dictionary<GameTag, int> Tags { get; set; } = new Dictionary<GameTag, int> {
			{ GameTag.CARDTYPE, (int) CardType.MINION },
			{ GameTag.HEALTH, 3 }
		};
	}

	class AT_094 : Card
	{
		public override string Id { get; set; } = "AT_094";
		public override string Name { get; set; } = "Flame Juggler";
		public override Dictionary<GameTag, int> Tags { get; set; } = new Dictionary<GameTag, int> {
			{ GameTag.CARDTYPE, (int) CardType.MINION },
			{ GameTag.HEALTH, 3 }
		};
	}

	class GVG_110t : Card
	{
		public override string Id { get; set; } = "GVG_110t";
		public override string Name { get; set; } = "Boom Bot";
		public override Dictionary<GameTag, int> Tags { get; set; } = new Dictionary<GameTag, int> {
			{ GameTag.CARDTYPE, (int) CardType.MINION },
			{ GameTag.HEALTH, 1 }
		};
	}

	enum GameTag
	{
		ZONE,
		ZONE_POSITION,
		ENTITY_ID,
		DAMAGE,
		HEALTH,
		CARDTYPE,
		_COUNT
	}

	enum Zone
	{
		PLAY = 1,
		HAND = 3,
		GRAVEYARD = 4,
		_COUNT = 3
	}

	enum CardType
	{
		GAME = 1,
		PLAYER = 2,
		HERO = 3,
		MINION = 4,
		SPELL = 5,
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

	interface IEntity
	{
		IEntity Play();
	}

	class Entity : IEntity, ICloneable
	{
		public Game Game { get; set; } = null;
		public Card Card { get; set; }
		public Dictionary<GameTag, int?> Tags { get; protected set; } = new Dictionary<GameTag, int?>((int)GameTag._COUNT);

		public int? this[GameTag t] {
			get {
				// Use TryGetValue for safety
				return Tags[t];
			}
			set {
				Tags[t] = value;
				Game.PowerHistory.Add(new TagChange { Entity = this, Key = t, Value = value });
			}
		}

		public Entity() {
			// set all tags to zero to avoid having to check if keys exist
			// worsens memory footprint to improve performance
			//var tags = Enum.GetValues(typeof(GameTag));

			//foreach (var tagValue in tags)
				//Tags.Add((GameTag)tagValue, null);
		}

		public virtual IEntity Play() { return this; }

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

	class Minion : Entity
	{
		public int Health { get; set; }

		public override IEntity Play() {
			Health = (int)Card[GameTag.HEALTH];
			Console.WriteLine("Player {0} is playing {1}", Game.CurrentPlayer, Card.Name);
			Game.CurrentPlayer.ZoneHand.Remove(this);
			Game.CurrentPlayer.ZonePlay.Add(this);
			this[GameTag.ZONE] = (int)Zone.PLAY;
			this[GameTag.ZONE_POSITION] = Game.CurrentPlayer.ZonePlay.Count;
			Game.Enqueue(Card.Behaviour.Play);
			return this;
		}

		public void Damage(int amount) {
			Console.WriteLine("{0} gets hit for {1} points of damage!", this, amount);
			Health -= amount;
			this[GameTag.DAMAGE] = Card[GameTag.HEALTH] - Health;
			CheckForDeath();
		}

		public void CheckForDeath() {
			if (Health <= 0) {
				Console.WriteLine(this + " dies!");
				Game.Opponent.ZonePlay.Remove(this);
				Game.Enqueue(Card.Behaviour.Death);
				this[GameTag.ZONE] = (int)Zone.GRAVEYARD;
				this[GameTag.ZONE_POSITION] = 0;
				this[GameTag.DAMAGE] = 0;
			}
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
			var e = new Minion {
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
		public int Health { get; private set; } = 30;
		public List<Minion> ZoneHand { get; } = new List<Minion>();
		public List<Minion> ZonePlay { get; } = new List<Minion>();
		
		public Entity Give(Card card) {
			if (card[GameTag.CARDTYPE] == (int) CardType.MINION) {
				var minion = new Minion { Card = card, Game = Game };
				ZoneHand.Add(minion);
				minion[GameTag.ZONE] = (int)Zone.HAND;
				minion[GameTag.ZONE_POSITION] = ZoneHand.Count + 1;
				return minion;
			}
			return null;
		}

		public override string ToString() {
			return Card.Id;
		}

		public new object Clone() {
			var p = new Player {
				Card = Card,
				Health = Health
			};
			foreach (var e in ZoneHand)
				p.ZoneHand.Add(e.Clone() as Minion);
			foreach (var e in ZonePlay)
				p.ZonePlay.Add(e.Clone() as Minion);
			return p;
		}
	}

	class Game : Entity
	{
		public Player Player1 { get; set; }
		public Player Player2 { get; set; }
		public Player CurrentPlayer { get; set; }
		public Player Opponent { get; set; }

		public List<PowerAction> PowerHistory = new List<PowerAction>();
		public Queue<QueueAction> ActionQueue = new Queue<QueueAction>();
		public Stack<ActionResult> ActionResultStack = new Stack<ActionResult>();

		public override string ToString() {
			string s = "Board state: ";
			var players = new List<Player> { Player1, Player2 };
			foreach (var player in players) {
				s += "Player " + player.Card.Id + " - ";
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
		
		public void Enqueue(ActionGraph g) {
			// Don't queue unimplemented cards
			if (g != null)
				g.Queue(this);
		}

		public void ResolveQueue() {
			while (ActionQueue.Count > 0) {
				var action = ActionQueue.Dequeue();
				Console.WriteLine(action);
				var args = new List<ActionResult>();
				for (int i = 0; i < action.Args.Count; i++)
					args.Add(ActionResultStack.Pop());
				args.Reverse();
				ActionResultStack.Push(action.Run(args));
			}
		}

		public IEnumerable<Entity> Entities {
			get {
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
		}

		public new Game Clone() {
			var g = new Game {
				Player1 = (Player)Player1.Clone(),
				Player2 = (Player)Player2.Clone()
			};
			// Yeah, fix this...
			g.CurrentPlayer = g.Player1;
			g.Opponent = g.Player2;
			foreach (var entity in g.Entities) {
				entity.Game = g;
			}
			return g;
		}
	}

	class CardDefs
	{
		public Dictionary<string, Card> Cards = new Dictionary<string, Card>();

		public Card this[string cardId] {
			get {
				return Cards[cardId];
			}
		}

		public Card ByName(string cardName) {
			return Cards.First(x => x.Value.Name == cardName).Value;
		}

		public CardDefs() {
			// Build the card definitions from the 'XML' and the behaviour scripts
			// These will never be modified once created
			Cards = new Dictionary<string, Card> {
				{ "GVG_096", new GVG_096() },
				{ "AT_094", new AT_094 { Behaviour = CardBehaviour.AT_094 } },
				{ "GVG_110t", new GVG_110t { Behaviour = CardBehaviour.GVG_110t } },
				{ "Player", new Card { Id = "Player", Name = "Player" } }
			};
		}
	}

	class Brimstone
	{
		public static CardDefs Cards = new CardDefs();

		public const int MaxMinions = 7;

		static void Main(string[] args) {
			Console.WriteLine("Hello Hearthstone!");

			var game = new Game();
			game.Player1 = new Player { Game = game, Card = Cards["Player"] };
			game.Player2 = new Player { Game = game, Card = Cards["Player"] };
			var p1 = game.Player1;
			var p2 = game.Player2;
			game.CurrentPlayer = p1;
			game.Opponent = p2;

			// Put a Piloted Shredder and Flame Juggler in each player's hand
			p1.Give(Cards.ByName("Piloted Shredder"));
			p1.Give(Cards.ByName("Flame Juggler"));
			p2.Give(Cards.ByName("Piloted Shredder"));
			p2.Give(Cards.ByName("Flame Juggler"));

			Console.WriteLine(game);

			// Fill the board with Flame Jugglers
			for (int i = 0; i < MaxMinions - 2; i++) {
				var fj = p1.Give(Cards.ByName("Flame Juggler"));
				fj.Play();
				game.ResolveQueue();
			}

			game.CurrentPlayer = p2;
			game.Opponent = p1;

			for (int i = 0; i < MaxMinions - 2; i++) {
				var fj = p2.Give(Cards.ByName("Flame Juggler"));
				fj.Play();
				game.ResolveQueue();
			}
			// Throw in a couple of Boom Bots
			p2.Give(Cards.ByName("Boom Bot")).Play();
			game.ResolveQueue();
			p2.Give(Cards.ByName("Boom Bot")).Play();
			game.ResolveQueue();

			game.CurrentPlayer = p1;
			game.Opponent = p2;

			p1.Give(Cards.ByName("Boom Bot")).Play();
			game.ResolveQueue();
			p1.Give(Cards.ByName("Boom Bot")).Play();
			game.ResolveQueue();

			// Set off the chain of eventsy
			var boardStates = new Dictionary<string, int>();

			var cOut = Console.Out;
			Console.SetOut(TextWriter.Null);

			for (int i = 0; i < 100000; i++) {
				var clonedGame = game.Clone();
				var firstboombot = clonedGame.Player1.ZonePlay.First(t => t.Card.Id == "GVG_110t");
				firstboombot.Damage(1);
				clonedGame.ResolveQueue();

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
