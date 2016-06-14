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

	public interface ICopyOnWrite
	{
		void CopyOnWrite();
	}

	public interface IPlayable : IEntity
	{
		IPlayable Play();
	}

	public interface IMinion : IPlayable
	{
		void Damage(int amount);
	}

	public class BaseEntityData : ICloneable
	{
		public int Id { get; set; }
		public Card Card { get; }
		public Dictionary<GameTag, int?> Tags { get; }

		public int? this[GameTag t] {
			get {
				// Use TryGetValue for safety
				return Tags[t];
			}
			set {
				Tags[t] = value;
			}
		}

		public BaseEntityData(Game game, Card card, Dictionary<GameTag, int?> tags = null) {
			Card = card;
			if (tags != null)
				Tags = tags;
			else
				Tags = new Dictionary<GameTag, int?>((int)GameTag._COUNT);
		}

		// Cloning copy constructor
		public BaseEntityData(BaseEntityData cloneFrom) {
			Card = cloneFrom.Card;
			Id = cloneFrom.Id;
			Tags = new Dictionary<GameTag, int?>(cloneFrom.Tags);
		}

		public virtual object Clone() {
			return new BaseEntityData(this);
		}
	}

	public class BaseEntity : IEntity, ICopyOnWrite
	{
		private BaseEntityData _entity;
		private List<BaseEntity> _references;

		public Game Game { get; set; }

		public BaseEntity(BaseEntity cloneFrom) {
			_entity = cloneFrom._entity;
			_references = cloneFrom._references;
			_references.Add(this);
			Game = cloneFrom.Game;
		}

		public BaseEntity(Game game, Card card, Dictionary<GameTag, int?> tags = null) {
			_entity = new BaseEntityData(game, card, tags);
			_references = new List<BaseEntity> { this };

			Game = game;
			// Game is null if we are starting a new game
			Id = (game == null ? 1 : game.NextEntityId++);

			if (Game != null)
				Game.PowerHistory.Add(new CreateEntity(this));
		}

		public int? this[GameTag t] {
			get {
				return Tags[t];
			}

			set {
				CopyOnWrite();
				_entity[t] = value;
				Game.PowerHistory.Add(new TagChange(this) { Key = t, Value = value });
			}
		}

		public Card Card {
			get {
				return _entity.Card;
			}
		}

		public int Id {
			get {
				return _entity.Id;
			}

			set {
				CopyOnWrite();
				_entity.Id = value;
			}
		}

		public Dictionary<GameTag, int?> Tags {
			get {
				return _entity.Tags;
			}
		}

		public virtual object Clone() {
			return new BaseEntity(this);
		}

		public void CopyOnWrite() {
			if (_references.Count > 1) {
				_entity = (BaseEntityData)_entity.Clone();
				_references.Remove(this);
				_references = new List<BaseEntity> { this };
			}
		}
	}
}