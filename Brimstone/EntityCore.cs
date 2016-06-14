using System;
using System.Collections.Generic;

namespace Brimstone
{
	public interface IEntity : ICloneable
	{
		Game Game { get; set; }
		Card Card { get; set; }
		Dictionary<GameTag, int?> Tags { get; set; }

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
		public Game Game { get; set; } = null;
		public Card Card { get; set; }
		public Dictionary<GameTag, int?> Tags { get; set; } = new Dictionary<GameTag, int?>((int)GameTag._COUNT);

		public abstract int? this[GameTag t] { get; set; }

		public virtual object Clone() {
			BaseEntity clone = OnClone();
			clone.Card = Card;
			clone.Tags = new Dictionary<GameTag, int?>(Tags);
			return clone;
		}

		protected abstract BaseEntity OnClone();
	}

	public class Entity : BaseEntity
	{
		public override int? this[GameTag t] {
			get {
				// Use TryGetValue for safety
				return Tags[t];
			}
			set {
				Tags[t] = value;
				Game.PowerHistory.Add(new TagChange { Entity = this, Key = t, Value = value });
			}
		}

		protected override BaseEntity OnClone() {
			return new Entity();
		}
	}
}