using System;
using System.Collections.Generic;

namespace Brimstone
{
	public interface IEntity : ICloneable
	{
		int Id { get; set; }
		// Allow owner game to be changed for state cloning
		Game Game { get; set; }
		Card Card { get; }
		Dictionary<GameTag, int?> Tags { get; }

		int? this[GameTag t] { get; set; }
	}

	public interface IPlayable : IEntity
	{
		IPlayable Play();
	}

	public interface IMinion : IPlayable
	{
		int Health { get; set; }
		void Damage(int amount);
	}

	public abstract class BaseEntity : IEntity
	{
		public int Id { get; set; }
		public Game Game { get; set; }
		public Card Card { get; }
		public Dictionary<GameTag, int?> Tags { get; } = new Dictionary<GameTag, int?>((int)GameTag._COUNT);

		public abstract int? this[GameTag t] { get; set; }
		public BaseEntity(Game game, Card card, Dictionary<GameTag, int?> tags = null) {
			Card = card;
			Game = game;
			if (tags != null)
				Tags = tags;

			// Game is null if we are starting a new game
			Id = (game == null ? 1 : game.NextEntityId++);
		}

		// Cloning copy constructor
		public BaseEntity(BaseEntity cloneFrom) {
			Card = cloneFrom.Card;
			Game = cloneFrom.Game;
			Id = cloneFrom.Id;
			Tags = new Dictionary<GameTag, int?>(cloneFrom.Tags);
		}

		public abstract object Clone();
	}

	public class Entity : BaseEntity
	{
		public Entity(Entity cloneFrom) : base(cloneFrom) { }
		public Entity(Game game, Card card, Dictionary<GameTag, int?> tags = null) : base(game, card, tags) {
			// game is null if we are cloning or starting a new game
			if (game != null)
				Game.PowerHistory.Add(new CreateEntity(this));
		}

		public override int? this[GameTag t] {
			get {
				// Use TryGetValue for safety
				return Tags[t];
			}
			set {
				Tags[t] = value;
				Game.PowerHistory.Add(new TagChange(this) { Key = t, Value = value });
			}
		}

		public override object Clone() {
			return new Entity(this);
		}
	}
}