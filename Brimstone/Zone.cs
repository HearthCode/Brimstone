using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Brimstone
{
	public interface IZones
	{
		ZoneGroup Zones { get; }
	}

	public class ZoneGroup : IEnumerable<ZoneEntities>
	{
		private ZoneEntities[] _zones = new ZoneEntities[(int)Zone._COUNT];

		public ZoneEntities this[Zone z] {
			get {
				return _zones[(int)z];
			}
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
		public IZones Controller { get; }

		private List<IEntity> _cachedEntities;
		private int _cachedCount;

		private List<IEntity> Entities {
			get {
				if (_cachedEntities == null)
					Init();
				return _cachedEntities;
			}
		}

		protected void Init() {
			// Make sure that _cachedEntities[0] has ZONE_POSITION = 1 etc.
			_cachedEntities = Game.Entities
				.Where(e => e.Controller == Controller && e[GameTag.ZONE] == (int)Zone && e[GameTag.ZONE_POSITION] > 0)
				.OrderBy(e => e[GameTag.ZONE_POSITION])
				.ToList();
			_cachedCount = _cachedEntities.Count();
		}

		protected void UpdateZonePositions() {
			int p = 1;
			foreach (var ze in _cachedEntities)
				ze[GameTag.ZONE_POSITION] = p++;
		}

		public ZoneEntities(Game game, IZones controller, Zone zone) {
			Game = game;
			Zone = zone;
			Controller = controller;
		}

		public int Count {
			get {
				if (_cachedEntities == null)
					Init();
				return _cachedCount;
			}
		}

		public bool IsEmpty {
			get {
				return (Count == 0);
			}
		}

		public IEntity this[int zone_position] {
			get {
				if (_cachedEntities == null)
					Init();
				return _cachedEntities[zone_position - 1];
			}
		}

		public List<IEntity> Slice(int zpStart, int zpEnd) {
			return Entities.Skip(zpStart - 1).Take((zpEnd - zpStart) + 1).ToList();
		}

		public IEntity Add(IEntity Entity, int ZonePosition = -1) {
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
				Entities.Insert(ZonePosition - 1, Entity);
				_cachedCount++;
			}
			else
				Entity[GameTag.ZONE_POSITION] = 0;
			Entity[GameTag.ZONE] = (int)Zone;

			if (ZonePosition != 0)
				UpdateZonePositions();
			return Entity;
		}

		public IEntity Remove(IEntity Entity, bool ClearZone = true) {
			bool removed = Entities.Remove(Entity);
			if (removed) {
				_cachedCount--;
				UpdateZonePositions();
				if (ClearZone) {
					Entity[GameTag.ZONE_POSITION] = 0;
					Entity[GameTag.ZONE] = (int)Zone.INVALID;
				}
				return Entity;
			}
			return null;
		}

		public IEntity MoveTo(IEntity Entity, int ZonePosition = -1) {
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
