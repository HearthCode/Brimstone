using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Brimstone
{
	// TODO: Handle unknown entities

	public partial interface IEntity : IEnumerable<KeyValuePair<GameTag, int>>, ICloneable {
		int Id { get; set; }
		// Allow owner game and controller to be changed for state cloning
		Game Game { get; set; }
		BaseEntityData BaseEntityData { get; }
		Card Card { get; }
		IZoneController ZoneController { get; }
		IZone Zone { get; set; }
		int ZonePosition { get; set; }
		void ZoneMove(Zone Zone, int ZonePosition = -1);
		void ZoneMove(IZone Zone, int ZonePosition = -1);
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

		internal BaseEntityData(Card card, Dictionary<GameTag, int> tags = null) {
			Card = card;
			if (tags != null)
				Tags = tags;
			else
				Tags = new Dictionary<GameTag, int>((int)GameTag._COUNT);
		}

		// Cloning copy constructor
		internal BaseEntityData(BaseEntityData cloneFrom) {
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

	internal class ReferenceCount : IReferenceCount
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

	internal class ReferenceCountInterlocked : IReferenceCount
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

		public long ReferenceCount => _referenceCount.Count;
		public BaseEntityData BaseEntityData => _entity;
		public virtual Game Game { get; set; }
		public IZoneController ZoneController => (IZoneController)Controller ?? Game;

		protected internal Entity(Entity cloneFrom) {
			_fuzzyHash = cloneFrom._fuzzyHash;
			_referenceCount = cloneFrom._referenceCount;
			if (Settings.CopyOnWrite) {
				_entity = cloneFrom._entity;
				_referenceCount.Increment();
			} else {
				_entity = (BaseEntityData)cloneFrom._entity.Clone();
			}
		}

		protected internal Entity(Card card, Dictionary<GameTag, int> tags = null) {
			_entity = new BaseEntityData(card, tags);
			_referenceCount = (Settings.ParallelClone ? (IReferenceCount) new ReferenceCountInterlocked() : new ReferenceCount());
		}

		public int this[GameTag t] {
			get {
				if (t == GameTag.ENTITY_ID)
					return _entity.Id;
				return _entity[t];
			}
			set {
				if (value < 0) value = 0;
				// Ignore unchanged data
				var oldValue = _entity[t];
				if (value == oldValue)
					return;
				if (t == GameTag.ENTITY_ID) {
					Changing(t, oldValue, value);
					_entity.Id = value;
				} else {
					Changing(t, oldValue, value);
					_entity[t] = value;
				}
				Game?.EntityChanged(this, t, oldValue, value);
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

		IEntity IEntity.CloneState() {
			return Clone() as IEntity;
		}

		private void Changing(GameTag tag, int oldValue, int newValue, bool cow = true) {
			Game.EntityChanging(this, tag, oldValue, newValue, _fuzzyHash);
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
			allTags[GameTag.ENTITY_ID] = _entity.Id;
			return allTags;
		}

		public static IEntity FromCard(Card Card, Dictionary<GameTag, int> Tags = null, IZone StartingZone = null)
		{
			IEntity e = null;
			switch (Card.Type)
			{
				case CardType.MINION:
					if (StartingZone == null)
						e = new Minion(Card, Tags);
					else
						e = new Minion(Card, Tags) {Zone = StartingZone};
					break;

				case CardType.SPELL:
					if (StartingZone == null)
						e = new Spell(Card, Tags);
					else
						e = new Spell(Card, Tags) {Zone = StartingZone};
					break;
				
				// TODO: Weapons etc.
			}
			return e;
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
				bool inPlay = Zone == ZoneController.Board;
				uint hash = 2166136261;
				// The card's asset ID uniquely identifies the set of immutable starting tags for the card
				hash = (hash * prime) ^ (uint)(_entity.Card.AssetId >> 8);
				hash = (hash * prime) ^ (uint)(_entity.Card.AssetId & 0xff);
				foreach (var kv in _entity.Tags)
					if ((kv.Key != GameTag.ZONE_POSITION || inPlay)
						&& kv.Key != GameTag.LAST_AFFECTED_BY && kv.Key != GameTag.LAST_CARD_PLAYED && kv.Key != GameTag.NUM_OPTIONS_PLAYED_THIS_TURN) {
						hash = (hash * prime) ^ ((uint)kv.Key >> 8);
						hash = (hash * prime) ^ ((uint)kv.Key & 0xff);
						hash = (hash * prime) ^ (uint)(kv.Value >> 8);
						hash = (hash * prime) ^ (uint)(kv.Value & 0xff);
					}
				_fuzzyHash = (int)hash;
				return _fuzzyHash;
			}
		}

		public override string ToString() {
			string s = "[" + string.Format("{0:x8}", FuzzyHash) + "] " + Card.Name + " - ";
			s += new Tag(GameTag.ENTITY_ID, _entity.Id) + ", ";
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
}
