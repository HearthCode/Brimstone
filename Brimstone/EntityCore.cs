using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Brimstone
{
	// TODO: Handle unknown entities

	public interface IEntity : IEnumerable<KeyValuePair<GameTag, int>>, ICloneable {
		int Id { get; set; }
		// Allow owner game and controller to be changed for state cloning
		Game Game { get; set; }
		IZoneOwner Controller { get; set; }
		Card Card { get; }
		ZoneEntities Zone { get; set; }
		int ZonePosition { get; set; }
		void ZoneMove(Zone Zone, int ZonePosition = -1);
		void ZoneMove(ZoneEntities Zone, int ZonePosition = -1);
		void ZoneSwap(IEntity New);
		Dictionary<GameTag, int> CopyTags();
		int this[GameTag t] { get; set; }
		string ShortDescription { get; }
		int FuzzyHash { get; }

		IEntity CloneState();
	}

	public class BaseEntityData : ICloneable {
		public int Id { get; set; }
		public Card Card { get; }
		public Dictionary<GameTag, int> Tags { get; }

		public int this[GameTag t] {
			get {
				// Use the entity tag if available, otherwise the card tag
				if (Tags.ContainsKey(t))
					return Tags[t];
				if (Card.Tags.ContainsKey(t))
					return Card[t];
				return 0;
			}
			set {
				Tags[t] = value;
			}
		}

		public BaseEntityData(Card card, Dictionary<GameTag, int> tags = null) {
			Card = card;
			if (tags != null)
				Tags = tags;
			else
				Tags = new Dictionary<GameTag, int>((int)GameTag._COUNT);
		}

		// Cloning copy constructor
		public BaseEntityData(BaseEntityData cloneFrom) {
			Card = cloneFrom.Card;
			Id = cloneFrom.Id;
			Tags = new Dictionary<GameTag, int>(cloneFrom.Tags);
		}

		public virtual object Clone() {
			return new BaseEntityData(this);
		}
	}

	public interface IReferenceCount
	{
		void Increment();
		void Decrement();
		long Count { get; }
	}

	public class ReferenceCount : IReferenceCount
	{
		private long _count;
		public ReferenceCount() {
			_count = 1;
		}

		public void Increment() {
			++_count;
		}

		public void Decrement() {
			--_count;
		}

		public long Count {
			get {
				return _count;
			}
		}
	}

	public class ReferenceCountInterlocked : IReferenceCount
	{
		private long _count;
		public ReferenceCountInterlocked() {
			_count = 1;
		}

		public void Increment() {
			Interlocked.Increment(ref _count);
		}

		public void Decrement() {
			Interlocked.Decrement(ref _count);
		}

		public long Count {
			get {
				return Interlocked.Read(ref _count);
			}
		}
	}

	public partial class Entity : IEntity {
		private BaseEntityData _entity;
		private IReferenceCount _referenceCount;

		public long ReferenceCount { get { return _referenceCount.Count; } }
		public BaseEntityData BaseEntityData { get { return _entity; } }
		public virtual Game Game { get; set; }
		// TODO: Re-do Controller code as normal tag property
		private IZoneOwner _controller;
		public IZoneOwner Controller {
			get {
				return _controller;
			}
			set {
				if (_controller == value)
					return;
				if (Game != null)
					if (Game.Entities != null)
						Changing(false);
				_controller = value;
				if (Game != null)
					if (Game.Entities != null)
						Game.Entities.EntityChanged(Id, GameTag.CONTROLLER, value.Id);
			}
		}

		public Entity(Entity cloneFrom) {
			_fuzzyHash = cloneFrom._fuzzyHash;
			_referenceCount = cloneFrom._referenceCount;
			if (Settings.CopyOnWrite) {
				_entity = cloneFrom._entity;
				_referenceCount.Increment();
			} else {
				_entity = (BaseEntityData)cloneFrom._entity.Clone();
			}
		}

		public Entity(Card card, Dictionary<GameTag, int> tags = null) {
			_entity = new BaseEntityData(card, tags);
			_referenceCount = (Settings.ParallelClone ? (IReferenceCount) new ReferenceCountInterlocked() : new ReferenceCount());
		}

		public int this[GameTag t] {
			get {
				if (t == GameTag.ENTITY_ID)
					return _entity.Id;
				if (t == GameTag.CONTROLLER)
					return Controller.Id;
				return _entity[t];
			}
			set {
				// Ignore unchanged data
				if (_entity.Tags.ContainsKey(t) && _entity[t] == value)
					return;
				else if (value == 0 && !_entity.Tags.ContainsKey(t))
					return;

				else if (t == GameTag.CONTROLLER) {
					Controller = (IZoneOwner) Game.Entities[value];
				}
				else if (t == GameTag.ENTITY_ID) {
					Changing();
					_entity.Id = value;
				} else {
					Changing();
					_entity[t] = value;
				}
				if (Game != null && t != GameTag.CONTROLLER)
					Game.Entities.EntityChanged(Id, t, value);
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
				_entity.Id = value;
			}
		}

		public virtual object Clone() {
			return new Entity(this);
		}

		public virtual IEntity CloneState() {
			return Clone() as IEntity;
		}

		private void Changing(bool cow = true) {
			// TODO: Replace with a C# event
			Game.Entities.EntityChanging(Id, _fuzzyHash);
			_fuzzyHash = 0;
			if (cow) CopyOnWrite();
		}

		private void CopyOnWrite() {
			if (_referenceCount.Count > 1) {
				_entity = (BaseEntityData)_entity.Clone();
				_referenceCount.Decrement();
				_referenceCount = (Settings.ParallelClone ? (IReferenceCount)new ReferenceCountInterlocked() : new ReferenceCount());
			}
		}

		// Returns a *copy* of all tags from both the entity and the underlying card
		public Dictionary<GameTag, int> CopyTags() {
			var allTags = new Dictionary<GameTag, int>(_entity.Card.Tags);

			// Entity tags override card tags
			foreach (var tag in _entity.Tags)
				allTags[tag.Key] = tag.Value;

			// Specially handled tags
			if (Controller != null)
				allTags[GameTag.CONTROLLER] = Controller.Id;
			allTags[GameTag.ENTITY_ID] = _entity.Id;
			return allTags;
		}

		public IEnumerator<KeyValuePair<GameTag, int>> GetEnumerator() {
			// Hopefully we're only iterating through tags in test code
			// so it doesn't matter that we are making a deep clone of the dictionary
			return CopyTags().GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}

		public string ShortDescription {
			get {
				return Card.Name + " [" + Id + "]";
			}
		}

		// Get a NON-UNIQUE hash code for the entity (without copying the tags for speed)
		// This is used for testing fuzzy entity equality across games
		// The ENTITY_ID is left out, and the ZONE_POSITION is left out if the entity is in the player's hand
		// All underlying card tags are included to differentiate cards from each other. CONTROLLER is included
		private int _fuzzyHash = 0;
		public int FuzzyHash {
			get {
				if (_fuzzyHash != 0 && Settings.EntityHashCaching)
					return _fuzzyHash;
				uint prime = 16777219;
				bool inHand = Zone == Controller.Hand;
				uint hash = 2166136261;
				// The card's asset ID uniquely identifies the set of immutable starting tags for the card
				hash = (hash * prime) ^ (uint)(_entity.Card.AssetId >> 8);
				hash = (hash * prime) ^ (uint)(_entity.Card.AssetId & 0xff);
				foreach (var kv in _entity.Tags)
					if (kv.Key != GameTag.ZONE_POSITION || !inHand) {
						hash = (hash * prime) ^ ((uint)kv.Key >> 8);
						hash = (hash * prime) ^ ((uint)kv.Key & 0xff);
						hash = (hash * prime) ^ (uint)(kv.Value >> 8);
						hash = (hash * prime) ^ (uint)(kv.Value & 0xff);
					}
				hash = (hash * prime) ^ (uint)GameTag.CONTROLLER;
				hash = (hash * prime) ^ (uint)Controller.Id;
				_fuzzyHash = (int)hash;
				return _fuzzyHash;
			}
		}

		public override string ToString() {
			string s = Card.Name + " - ";
			s += new Tag(GameTag.ENTITY_ID, _entity.Id) + ", ";
			if (Controller != null)
				s += new Tag(GameTag.CONTROLLER, Controller.Id) + ", ";
			foreach (var tag in _entity.Tags) {
				s += new Tag(tag.Key, tag.Value) + ", ";
			}
			return s.Substring(0, s.Length - 2);
		}
	}

	public class FuzzyEntityComparer : IEqualityComparer<IEntity> {
		// Used when adding to and fetching from HashSet, and testing for equality
		public bool Equals(IEntity x, IEntity y) {
			return x.FuzzyHash == y.FuzzyHash;
		}

		public int GetHashCode(IEntity obj) {
			return obj.FuzzyHash;
		}
	}

	public class EntityController : IEnumerable<IEntity>, ICloneable {
		public Game Game { get; }
		public int NextEntityId = 1;
		private Dictionary<int, IEntity> Entities = new Dictionary<int, IEntity>();

		public IEntity this[int id] {
			get {
				return Entities[id];
			}
		}

		public int Count {
			get {
				return Entities.Count;
			}
		}

		public ICollection<int> Keys {
			get {
				return Entities.Keys;
			}
		}

		public bool ContainsKey(int key) {
			return Entities.ContainsKey(key);
		}

		public EntityController(Game game) {
			Game = game;
			Game.Controller = game;

			// Fuzzy hashing
			Changed = false;
			Add(game);
		}

		public EntityController(EntityController es) {
			_gameHash = es._gameHash;
			Changed = es.Changed;

			NextEntityId = es.NextEntityId;
			foreach (var entity in es) {
				Entities.Add(entity.Id, (IEntity) entity.Clone());
			}
			// Change ownership
			Game = FindGame();
			foreach (var entity in Entities)
				entity.Value.Game = Game;
			foreach (var entity in Entities)
				entity.Value.Controller = (IZoneOwner) Entities[es.Entities[entity.Key].Controller.Id];

			// Do this last so that changing Controller doesn't trigger EntityChanging
			Game.Entities = this;
		}

		public IEntity Add(IEntity entity) {
			entity.Game = Game;
			entity.Id = NextEntityId++;
			Entities[entity.Id] = entity;
			EntityChanging(entity.Id, 0);
			if (Game.PowerHistory != null)
				Game.PowerHistory.Add(new CreateEntity(entity));
			Game.ActiveTriggers.Add(entity);
			return entity;
		}

		public Game FindGame() {
			// Game is always entity ID 1
			return (Game)Entities[1];
		}

		public Player FindPlayer(int p) {
			// Player is always p+1
			return (Player)Entities[p + 1];
		}

		// Calculate a fuzzy hash for the whole game state
		// WARNING: The hash algorithm MUST be designed in such a way that the order
		// in which the entities are hashed doesn't matter
		private int _gameHash = 0;
		private bool _changed = false;
		public bool Changed {
			get {
				return _changed;
			}
			set {
				int dummy;
				if (!value)
					dummy = FuzzyGameHash;
				else
					_changed = true;
			}
		}

		public void EntityChanging(int id, int previousHash) {
			if (Settings.GameHashCaching)
				_changed = true;
		}

		public void EntityChanged(int id, GameTag tag, int value) {
			if (Game.PowerHistory != null)
				Game.PowerHistory.Add(new TagChange(id, tag, value));
		}

		public int FuzzyGameHash {
			get {
				// TODO: Take order-of-play semantics into account
				if (!Settings.GameHashCaching || _changed) {
					_gameHash = 0;
					// Hash board states (play zones) for both players in order, hash rest of game entities in any order
					foreach (var entity in Entities.Values)
						if (entity.Zone.Type != Zone.PLAY || entity.ZonePosition == 0)
							_gameHash += entity.FuzzyHash;
						else
							_gameHash += (entity.Controller.Id * 8 + entity.ZonePosition) * entity.FuzzyHash;
					_changed = false;
				}
				return _gameHash;
			}
		}

		public IEnumerator<IEntity> GetEnumerator() {
			return Entities.Values.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}

		public object Clone() {
			return new EntityController(this);
		}
	}
}
