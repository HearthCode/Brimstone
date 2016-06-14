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
			// New game?
			if (game == null) {
				Id = 1;
			}
			else {
				Id = game.NextEntityId++;
				Game = game;
				Card = card;
			}
			if (tags != null)
				Tags = tags;
		}

		public virtual object Clone() {
			return OnClone();
		}

		protected abstract BaseEntity OnClone();
	}

	public class Entity : BaseEntity
	{
		public Entity(Game game, Card card, Dictionary<GameTag, int?> tags) : base(game, card, tags) {
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

		protected override BaseEntity OnClone() {
			return new Entity(Game, Card, new Dictionary<GameTag, int?>(Tags));
		}
	}
}