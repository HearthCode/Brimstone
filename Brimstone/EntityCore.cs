using System;
using System.Collections;
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

	public class ReferenceCount
	{
		public ReferenceCount() {
			Count = 1;
		}

		public int Count { get; set; }
	}

	public class Entity : IEntity, ICopyOnWrite
	{
		private BaseEntityData _entity;
		private ReferenceCount _referenceCount;

		public Game Game { get; set; }

		public Entity(Entity cloneFrom) {
			_entity = cloneFrom._entity;
			_referenceCount = cloneFrom._referenceCount;
			Game = cloneFrom.Game;
		}

		public Entity(Game game, Card card, Dictionary<GameTag, int?> tags = null) {
			_entity = new BaseEntityData(game, card, tags);
			_referenceCount = new ReferenceCount();
			Game = game;
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
			return new Entity(this);
		}

		public void CopyOnWrite() {
			if (_referenceCount.Count > 1) {
				_entity = (BaseEntityData)_entity.Clone();
				_referenceCount.Count--;
				_referenceCount = new ReferenceCount();
			}
		}
	}

	public class EntitySequence : IEnumerable<Entity>, ICloneable
	{
		public Game Game { get; }
		public int NextEntityId = 1;

		public SortedDictionary<int, Entity> Entities { get; } = new SortedDictionary<int, Entity>();

		public Entity this[int id] {
			get {
				return Entities[id];
			}
		}

		public EntitySequence(Game game) {
			Game = game;
		}

		public EntitySequence(EntitySequence es) {
			Game = es.Game;
			NextEntityId = es.NextEntityId;
			foreach (var entity in es) {
				Entities.Add(entity.Id, (Entity) entity.Clone());
			}
		}

		public int Add(Entity entity) {
			entity.Game = Game;
			entity.Id = NextEntityId++;
			Entities[entity.Id] = entity;
			Game.PowerHistory.Add(new CreateEntity(entity));
			return entity.Id;
		}

		public IEnumerator<Entity> GetEnumerator() {
			return Entities.Values.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}

		public object Clone() {
			return new EntitySequence(this);
		}
	}
}