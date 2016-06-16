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

	public class ZoneEntities : IEnumerable<Entity>
	{
		public Game Game { get; }
		public Zone Zone { get; }
		public IZones Controller { get; }

		public ZoneEntities(Game game, IZones controller, Zone zone) {
			Game = game;
			Zone = zone;
			Controller = controller;
		}

		private bool _cacheDirty = true;
		private int _cachedCount;
		private IEnumerable<Entity> _cachedEntities;

		public IEnumerable<Entity> Entities {
			get {
				if (_cacheDirty)
					Refresh();
				return _cachedEntities;
			}
		}

		public int Count {
			get {
				return _cachedCount;
			}
		}

		// TODO: Optimize this into arrays
		public Entity this[int zone_position] {
			get {
				return _cachedEntities.FirstOrDefault(e => e.Controller == Controller && e[GameTag.ZONE] == (int)Zone && e[GameTag.ZONE_POSITION] == zone_position);
			}
		}

		protected Entity MoveToImpl(Entity Entity, bool SetZonePosition = true, bool Reorder = true) {
			Zone previous = (Zone)Entity[GameTag.ZONE];
			Entity[GameTag.ZONE] = (int)Zone;
			if (SetZonePosition)
				Entity[GameTag.ZONE_POSITION] = Count;
			if (previous != Zone.INVALID)
				Controller.Zones[previous].Update();
			Update();
			return Entity;
		}

		public virtual Entity MoveTo(Entity Entity) {
			return MoveToImpl(Entity: Entity);
		}

		private void Refresh() {
			_cachedEntities = Game.Entities.Where(e => e.Controller == Controller && e[GameTag.ZONE] == (int)Zone);
			_cachedCount = _cachedEntities.Count();
			_cacheDirty = false;
		}

		protected void UpdateImpl(bool Reorder = true) {
			if (Reorder) {
				Refresh();
				int p = 1;
				foreach (var ze in _cachedEntities)
					ze[GameTag.ZONE_POSITION] = p++;
			} else {
				_cacheDirty = true;
			}
		}

		public virtual void Update() {
			UpdateImpl(Reorder: true);
		}

		public IEnumerator<Entity> GetEnumerator() {
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

	public class GameZoneEntities : ZoneEntities
	{
		public GameZoneEntities(Game game, IZones controller, Zone zone) : base(game, controller, zone) { }

		public override Entity MoveTo(Entity e) {
			return MoveToImpl(Entity: e, SetZonePosition: false);
		}

		public override void Update() {
			// Don't re-order zone positions
			UpdateImpl(Reorder: false);
		}
	}

	public class PlayerGraveyardZoneEntities : ZoneEntities
	{
		public PlayerGraveyardZoneEntities(Game game, IZones controller, Zone zone) : base(game, controller, zone) { }

		public override Entity MoveTo(Entity e) {
			// Set zone position to zero when adding entities to GRAVEYARD zone
			e[GameTag.ZONE_POSITION] = 0;
			return MoveToImpl(Entity: e, SetZonePosition: false);
		}

		public override void Update() {
			// Don't re-order zone positions
			UpdateImpl(Reorder: false);
		}
	}
}
