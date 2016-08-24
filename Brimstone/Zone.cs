using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Brimstone
{
	public interface IZoneOwner : IEntity
	{
		ZoneGroup Zones { get; }
	}

	public class ZoneGroup : IEnumerable<ZoneEntities>
	{
		public Game Game { get; }
		public IZoneOwner Owner { get; }

		private ZoneEntities[] _zones = new ZoneEntities[(int)Zone._COUNT];

		public ZoneGroup(Game game, IZoneOwner controller) {
			Game = game;
			Owner = controller;
		}

		public ZoneEntities this[Zone z] {
			get {
				if (_zones[(int)z] == null)
					_zones[(int)z] = new ZoneEntities(Game, Owner, z);
				return _zones[(int)z];
			}
			// For decks
			set {
				_zones[(int)z] = value;
			}
		}

		public IEnumerator<ZoneEntities> GetEnumerator() {
			return ((IEnumerable<ZoneEntities>)_zones).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return ((IEnumerable<ZoneEntities>)_zones).GetEnumerator();
		}
	}

	public class ZoneEntities : IEnumerable<IEntity>
	{
		public Game Game { get; }
		public Zone Zone { get; }
		public IZoneOwner Controller { get; }

		private IEnumerable<IEntity> _cachedEntities;
		private List<IEntity> _cachedEntitiesAsList;

		private IEnumerable<IEntity> Entities {
			get {
				if (!Settings.ZoneCaching) {
					Init();
					var v = _cachedEntities;
					_cachedEntities = null;
					return v;
				}
				if (_cachedEntitiesAsList != null)
					return _cachedEntitiesAsList;
				if (_cachedEntities == null)
					Init();
				return _cachedEntities;
			}
		}

		protected void Init() {
			// Make sure that _cachedEntities[0] has ZONE_POSITION = 1 etc.
			_cachedEntities = Game.Entities
				.Where(e => e.Controller == Controller && e[GameTag.ZONE] == (int)Zone && e[GameTag.ZONE_POSITION] > 0);
			_cachedEntitiesAsList = null;
		}

		private List<IEntity> asList {
			get {
				if (_cachedEntitiesAsList == null || !Settings.ZoneCaching)
					_cachedEntitiesAsList = Entities.OrderBy(e => e[GameTag.ZONE_POSITION]).ToList();
				return _cachedEntitiesAsList;
			}
		}

		private void updateZonePositions() {
			int p = 1;
			foreach (var ze in _cachedEntitiesAsList)
				ze[GameTag.ZONE_POSITION] = p++;
		}

		public ZoneEntities(Game game, IZoneOwner controller, Zone zone) {
			Game = game;
			Zone = zone;
			Controller = controller;
		}

		public int Count {
			get {
				return asList.Count;
			}
		}

		public bool IsEmpty {
			get {
				return (Count == 0);
			}
		}

		public IEntity this[int zone_position] {
			get {
				return asList[zone_position - 1];
			}
		}

		// Slice a zone
		// If no arguments are supplied, return the entire zone
		// If only one argument is supplied, return the first X elements (if X is positive) or last X elements (if X is negative)
		// If two arguments are supplied, X to Y returns elements X to Y inclusive
		// If two arguments are supplied, -X to -Y returns elemnts from Xth last to Yth last inclusive (eg. -4, -2 returns the 4th, 3rd and 2nd to last elements)
		// If two arguments are supplied, X to -Y returns elements from Xth first to Yth last inclusive (eg. 2, -3 returns the 2nd first to 3rd last element)
		public IEnumerable<IEntity> Slice(int? zpStart = null, int? zpEnd = null) {
			int eCount = Count;
			int start = 0, count = 0;

			// First or last X elements
			if (zpStart != null && zpEnd == null) {
				start = (zpStart > 0? 0 : eCount + (int)zpStart);
				count = Math.Abs((int)zpStart);
			}

			// Range
			if (zpStart != null && zpEnd != null) {
				start = (zpStart > 0 ? (int)zpStart - 1 : eCount + (int)zpStart);
				count = ((int)zpEnd - (int)zpStart) + 1; // works when both numbers are same sign

				if (zpStart > 0 && zpEnd < 0)
					count = (eCount + (int)zpEnd - (int)zpStart) + 2;
			}

			// All
			if (zpStart == null && zpEnd == null) {
				start = 0;
				count = eCount;
			}

			return asList.Skip(start).Take(count);
		}

		public IEntity Add(IEntity Entity, int ZonePosition = -1, bool InPlace = false) {
			// Update ownership
			if (Entity.Game == null) {
				Game.Add(Entity, Controller);
			}

			if (ZonePosition == -1)
				if (Entity is Minion)
					ZonePosition = Count + 1;
				else if (Entity is Spell)
					if (Zone == Zone.PLAY)
						ZonePosition = 0;
					else
						ZonePosition = Count + 1;
			if (Zone == Zone.SETASIDE || Zone == Zone.GRAVEYARD || (Zone == Zone.PLAY && Controller is Game))
				ZonePosition = 0;
			if (ZonePosition != 0) {
				asList.Insert(ZonePosition - 1, Entity);
			}
			else
				Entity[GameTag.ZONE_POSITION] = 0;
			Entity[GameTag.ZONE] = (int)Zone;

			if (ZonePosition != 0)
				updateZonePositions();

			return Entity;
		}

		public IEntity Remove(IEntity Entity, bool ClearZone = true, bool InPlace = false) {
			bool removed = asList.Remove(Entity);
			if (removed) {
				updateZonePositions();
				if (ClearZone) {
					Entity[GameTag.ZONE_POSITION] = 0;
					Entity[GameTag.ZONE] = (int)Zone.INVALID;
				}
				return Entity;
			}
			return null;
		}

		public IEntity MoveTo(IEntity Entity, int ZonePosition = -1, bool InPlace = false) {
			Zone previous = (Zone)Entity[GameTag.ZONE];
			if (previous != Zone.INVALID)
				Controller.Zones[previous].Remove(Entity, ClearZone: false);
			Add(Entity, ZonePosition);
			return Entity;
		}

		public IEnumerator<IEntity> GetEnumerator() {
			return Entities.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}

		public override string ToString() {
			string s = string.Empty;
			foreach (var e in this)
				s += e + "\n";
			return s;
		}
	}
}
