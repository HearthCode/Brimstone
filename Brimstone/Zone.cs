using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Brimstone
{
	public class ZoneEntities : IEnumerable<Entity>
	{
		public Game Game { get; }
		public Zone Zone { get; }
		public Entity Controller { get; }

		public ZoneEntities(Game game, Entity controller, Zone zone) {
			Game = game;
			Zone = zone;
			Controller = controller;
		}

		public IEnumerable<Entity> Entities {
			get {
				return Game.Entities.Where(e => e.Controller == Controller && e.Tags[GameTag.ZONE] == (int)Zone);
			}
		}

		public int Count {
			get {
				return Entities.Count();
			}
		}

		public Entity this[int zone_position] {
			get {
				return Game.Entities.FirstOrDefault(e => e.Controller == Controller && e.Tags[GameTag.ZONE] == (int)Zone && e.Tags[GameTag.ZONE_POSITION] == zone_position);
			}
		}

		public Entity Add(Entity e) {
			e[GameTag.ZONE] = (int)Zone;
			e[GameTag.ZONE_POSITION] = Count;
			return e;
		}

		public Entity Remove(Entity e) {
			e[GameTag.ZONE_POSITION] = 0;
			e[GameTag.ZONE] = (int)Zone.INVALID;
			int p = 1;
			foreach (var ze in Entities)
				ze[GameTag.ZONE_POSITION] = p++;
			return e;
		}

		public IEnumerator<Entity> GetEnumerator() {
			return Entities.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}
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
}
