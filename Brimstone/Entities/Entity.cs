/*
	Copyright 2016, 2017 Katy Coe

	This file is part of Brimstone.

	Brimstone is free software: you can redistribute it and/or modify
	it under the terms of the GNU Lesser General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	Brimstone is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU Lesser General Public License for more details.

	You should have received a copy of the GNU Lesser General Public License
	along with Brimstone.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Brimstone.Entities
{
	// TODO: Handle unknown entities

	public partial interface IEntity : IEnumerable<KeyValuePair<GameTag, int>>, ICloneable
	{
		int Id { get; set; }
		// Allow owner game to be changed for state cloning
		Game Game { get; set; }
		Card Card { get; }
		IZoneController ZoneController { get; }
		IZone Zone { get; set; }
		int ZonePosition { get; set; }
		void ZoneMove(Zone Zone, int ZonePosition = -1);
		void ZoneMove(IZone Zone, int ZonePosition = -1);
		void ZoneSwap(IEntity New);
		int this[GameTag t] { get; set; }
		string ShortDescription { get; }
		int FuzzyHash { get; }
		IEntity CloneState();
	}

	public partial class Entity : IEntity
	{
		private IReferenceCount _referenceCount;
		public long ReferenceCount => _referenceCount.Count;
		private EntityData _data;

		public virtual Game Game { get; set; }
		public Card Card => _data.Card;
		public IZoneController ZoneController => (IZoneController)Controller ?? Game;

		public int Id {
			get { return _data.Id; }
			set { _data.Id = value; }
		}

		public string ShortDescription => _data.ShortDescription;

		protected internal Entity(Entity cloneFrom) {
			_fuzzyHash = cloneFrom._fuzzyHash;
			_referenceCount = cloneFrom._referenceCount;
			if (Settings.CopyOnWrite) {
				_data = cloneFrom._data;
				_referenceCount.Increment();
			}
			else {
				_data = new EntityData(cloneFrom._data);
			}
		}

		protected internal Entity(Card card, Dictionary<GameTag, int> tags = null) {
			_data = new EntityData(card, tags);
			_referenceCount = (Settings.ParallelClone ? (IReferenceCount)new ReferenceCountInterlocked() : new ReferenceCount());
		}

		public int this[GameTag t] {
			get {
				return _data[t];
			}
			set {
				if (value < 0) value = 0;
				// Ignore unchanged data
				var oldValue = _data[t];
				if (value == oldValue)
					return;
				Changing(t, oldValue, value);
				_data[t] = value;
				Game?.EntityChanged(this, t, oldValue, value);
			}
		}

		private void Changing(GameTag tag, int oldValue, int newValue, bool cow = true) {
			Game.EntityChanging(this, tag, oldValue, newValue, _fuzzyHash);
			_fuzzyHash = 0;
			if (cow) CopyOnWrite();
		}

		private void CopyOnWrite() {
			if (_referenceCount.Count > 1) {
				_data = new EntityData(_data);
				_referenceCount.Decrement();
				_referenceCount = (Settings.ParallelClone ? (IReferenceCount)new ReferenceCountInterlocked() : new ReferenceCount());
			}
		}

		public static IEntity FromCard(Card Card, Dictionary<GameTag, int> Tags = null, IZone StartingZone = null) {
			IEntity e = null;
			switch (Card.Type) {
				case CardType.MINION:
					if (StartingZone == null)
						e = new Minion(Card, Tags);
					else
						e = new Minion(Card, Tags) { Zone = StartingZone };
					break;

				case CardType.SPELL:
					if (StartingZone == null)
						e = new Spell(Card, Tags);
					else
						e = new Spell(Card, Tags) { Zone = StartingZone };
					break;

					// TODO: Weapons etc.
			}
			return e;
		}

		public void ZoneMove(Zone Zone, int ZonePosition = -1) {
			Controller.Zones[Zone].MoveTo(this, ZonePosition);
		}

		public void ZoneMove(IZone Zone, int ZonePosition = -1) {
			Zone.MoveTo(this, ZonePosition);
		}

		public void ZoneMove(int ZonePosition = -1) {
			Zone.MoveTo(this, ZonePosition);
		}

		public void ZoneSwap(IEntity entity) {
			Zone.Swap(this, entity);
		}

		// Get a NON-UNIQUE hash code for the entity (without copying the tags for speed)
		// This is used for testing fuzzy entity equality across games
		// The ENTITY_ID is left out, and the ZONE_POSITION is left out if the entity is in the player's hand
		// All underlying card tags are included to differentiate cards from each other. CONTROLLER is included
		private int _fuzzyHash;

		public int FuzzyHash {
			get {
				if (_fuzzyHash != 0 && Settings.EntityHashCaching)
					return _fuzzyHash;
				uint prime = 16777219;
				bool inPlay = Zone == ZoneController.Board;
				uint hash = 2166136261;
				// The card's asset ID uniquely identifies the set of immutable starting tags for the card
				hash = (hash * prime) ^ (uint)(_data.Card.AssetId >> 8);
				hash = (hash * prime) ^ (uint)(_data.Card.AssetId & 0xff);
				foreach (var kv in _data.Tags)
					if ((kv.Key != GameTag.ZONE_POSITION || inPlay)
						&& kv.Key != GameTag.LAST_AFFECTED_BY && kv.Key != GameTag.LAST_CARD_PLAYED &&
						kv.Key != GameTag.NUM_OPTIONS_PLAYED_THIS_TURN) {
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
			return "[" + string.Format("{0:x8}", FuzzyHash) + "] " + _data;
		}

		public IEnumerator<KeyValuePair<GameTag, int>> GetEnumerator() {
			return _data.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}

		IEntity IEntity.CloneState() {
			return Clone() as IEntity;
		}

		public virtual object Clone() {
			return new Entity(this);
		}
	}
}
