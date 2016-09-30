using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Brimstone.Entities;
using Brimstone.Exceptions;

namespace Brimstone
{
	public interface IZone
	{
		Game Game { get; }
		Zone Type { get; }
		IZoneController Controller { get; }
		int Count { get; }
		bool IsEmpty { get; }
		int MaxSize { get; }
		bool IsFull { get; }
		IEntity this[int ZonePosition] { get; set; }
		IEnumerable<IEntity> Slice(int? Start, int? End);
		IEntity Add(IEntity Entity, int ZonePosition = -1);
		IEntity Remove(IEntity Entity, bool ClearZone = true);
		IEntity MoveTo(IEntity Entity, int ZonePosition = -1);
		void Swap(IEntity Old, IEntity New);
		void SetDirty();
	}

	public class Zone<T> : IZone, IEnumerable<T> where T : IEntity
	{
		public Game Game { get; }
		public Zone Type { get; }
		public IZoneController Controller { get; }

		private IEnumerable<T> _cachedEntities;
		private List<T> _cachedEntitiesAsList;

		protected IEnumerable<T> Entities
		{
			get
			{
				if (!Settings.ZoneCaching)
				{
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

		protected void Init()
		{
			// Make sure that _cachedEntities[0] has ZONE_POSITION = 1 etc.
			_cachedEntities = Game.Entities
				.Where(e => e.Zone == this && e.ZonePosition > 0).Select(e => (T) e);
			_cachedEntitiesAsList = null;
		}

		protected List<T> asList
		{
			get
			{
				if (_cachedEntitiesAsList == null || !Settings.ZoneCaching)
					_cachedEntitiesAsList = Entities.OrderBy(e => e.ZonePosition).ToList();
				return _cachedEntitiesAsList;
			}
		}

		private void updateZonePositions()
		{
			int p = 1;
			foreach (var ze in _cachedEntitiesAsList)
				ze[GameTag.ZONE_POSITION] = p++;
		}

		public Zone(Game game, IZoneController controller, Zone zone)
		{
			Game = game;
			Type = zone;
			Controller = controller;
		}

		public int Count => asList.Count;

		public bool IsEmpty => (Count == 0);

		public int MaxSize
			=> (Type == Zone.PLAY ? Game.MaxMinionsOnBoard : (Type == Zone.HAND ? ((Player) Controller).MaxHandSize : 9999));

		public bool IsFull => (Count == MaxSize);

		public T this[int zone_position]
		{
			get
			{
				if (zone_position <= 0 || zone_position > Count)
					return default(T);
				return asList[zone_position - 1];
			}
			set { MoveTo(value, zone_position); }
		}

		// NOTE: For internal use only
		public void SetDirty()
		{
			_cachedEntities = null;
			_cachedEntitiesAsList = null;
		}

		// Explicit interface
		IEntity IZone.this[int ZonePosition]
		{
			get { return this[ZonePosition]; }
			set { this[ZonePosition] = (T) value; }
		}

		IEnumerable<IEntity> IZone.Slice(int? Start, int? End)
		{
			return (IEnumerable<IEntity>) Slice(Start, End);
		}

		IEntity IZone.Add(IEntity Entity, int ZonePosition)
		{
			Add(Entity, ZonePosition);
			return Entity;
		}

		IEntity IZone.Remove(IEntity Entity, bool ClearZone)
		{
			Remove(Entity, ClearZone);
			return Entity;
		}

		IEntity IZone.MoveTo(IEntity Entity, int ZonePosition)
		{
			MoveTo(Entity, ZonePosition);
			return Entity;
		}

		void IZone.Swap(IEntity Old, IEntity New)
		{
			Swap((T) Old, (T) New);
		}

		// Slice a zone
		// If no arguments are supplied, return the entire zone
		// If only one argument is supplied, return the first X elements (if X is positive) or last X elements (if X is negative)
		// If two arguments are supplied, X to Y returns elements X to Y inclusive
		// If two arguments are supplied, -X to -Y returns elemnts from Xth last to Yth last inclusive (eg. -4, -2 returns the 4th, 3rd and 2nd to last elements)
		// If two arguments are supplied, X to -Y returns elements from Xth first to Yth last inclusive (eg. 2, -3 returns the 2nd first to 3rd last element)
		public IEnumerable<T> Slice(int? zpStart = null, int? zpEnd = null)
		{
			int eCount = Count;
			int start = 0, count = 0;

			// First or last X elements
			if (zpStart != null && zpEnd == null)
			{
				start = (zpStart > 0 ? 0 : eCount + (int) zpStart);
				count = Math.Abs((int) zpStart);
			}

			// Range
			if (zpStart != null && zpEnd != null)
			{
				start = (zpStart > 0 ? (int) zpStart - 1 : eCount + (int) zpStart);
				count = ((int) zpEnd - (int) zpStart) + 1; // works when both numbers are same sign

				if (zpStart > 0 && zpEnd < 0)
					count = (eCount + (int) zpEnd - (int) zpStart) + 2;
			}

			// All
			if (zpStart == null && zpEnd == null)
			{
				start = 0;
				count = eCount;
			}

			return asList.Skip(start).Take(count);
		}

		public T Add(IEntity Entity, int ZonePosition = -1)
		{
			// Update ownership
			if (Entity.Game == null)
			{
				Game.Add(Entity, (Player) Controller);
			}

			if (Type == Zone.INVALID)
				return (T) Entity;

			if (ZonePosition == -1)
				if (Entity is Minion)
					ZonePosition = Count + 1;
				else if (Entity is Spell)
					if (Type == Zone.PLAY)
						ZonePosition = 0;
					else
						ZonePosition = Count + 1;
				else if (Entity is Hero || Entity is HeroPower)
					ZonePosition = 0;
			if (Type == Zone.SETASIDE || Type == Zone.GRAVEYARD || (Type == Zone.PLAY && Controller is Game) || Entity is Player)
				ZonePosition = 0;
			if (ZonePosition != 0)
			{
				if (Entity is T)
				{
					if (IsFull)
						throw new ZoneException("Zone size exceeded when trying to move " + Entity.ShortDescription + " to zone " + Type);
					asList.Insert(ZonePosition - 1, (T) Entity);
				}
			}
			Entity[GameTag.ZONE] = (int) Type;

			if (Type == Zone.GRAVEYARD && Entity is Minion && Entity[GameTag.ZONE] == (int) Zone.PLAY)
			{
				Entity.Controller.NumFriendlyMinionsThatDiedThisTurn++;
				Entity.Controller.NumFriendlyMinionsThatDiedThisGame++;
			}

			if (ZonePosition == 0)
				Entity[GameTag.ZONE_POSITION] = 0;
			else
				updateZonePositions();
#if _ZONE_DEBUG
			DebugLog.WriteLine("Game " + Game.GameId + ": Adding " + Entity.ShortDescription + " to " + Entity.Controller.ShortDescription + "'s " + Type + " zone at position " + ZonePosition);
#endif
			if (Entity is T)
				return (T) Entity;
			return default(T);
		}

		public T Remove(IEntity Entity, bool ClearZone = true)
		{
			if (Entity.Zone.Type != Zone.INVALID)
			{
#if _ZONE_DEBUG
				DebugLog.WriteLine("Game " + Game.GameId + ": Removing " + Entity.ShortDescription + " from " + Entity.Controller.ShortDescription + "'s " + Type + " zone at position " + Entity.ZonePosition);
#endif
				bool removed = (Entity is T ? asList.Remove((T) Entity) : true);
				if (removed)
				{
					if (Entity is T)
						updateZonePositions();
					if (ClearZone)
					{
						Entity[GameTag.ZONE_POSITION] = 0;
						Entity[GameTag.ZONE] = (int) Zone.INVALID;
					}
				}
				else
					return default(T);
			}
			return (Entity is T ? (T) Entity : default(T));
		}

		public T MoveTo(IEntity Entity, int ZonePosition = -1)
		{
			var previous = Entity.Zone;
			if (previous != null && previous.Type != Zone.INVALID)
			{
				// Same zone move
				if (previous == this)
				{
					// Same zone moves allowed only for HAND, or for PLAY where the zone position changes
					if (Type != Zone.HAND && (Type != Zone.PLAY || Entity.ZonePosition == ZonePosition))
						throw new ZoneException(Entity.ShortDescription + " attempted a same zone move in " + Type);

					// We have to take a copy of asList here in case zone caching is disabled!
					var entities = asList;
					entities.Remove((T) Entity);
					entities.Insert(ZonePosition - 1, (T) Entity);
					updateZonePositions();
					return (T) Entity;
				}
				else
				{
					// Other zone move
					previous.Remove(Entity, ClearZone: false);
				}
			}
			if (Type != Zone.INVALID)
				Add(Entity, ZonePosition);
			if (Entity is T)
				return (T) Entity;
			return default(T);
		}

		// Perform an in-replacement of one entity with another, without re-calculating zone positions
		// NOTE: The item in New will be moved first
		public void Swap(T Old, T New)
		{
			var z = New.Zone.Type;
			int p = New.ZonePosition;

			// We have to do it in this order, because Blizzard
			New[GameTag.ZONE] = (int) Old.Zone.Type;
			New[GameTag.ZONE_POSITION] = Old.ZonePosition;
			Old[GameTag.ZONE] = (int) z;
			Old[GameTag.ZONE_POSITION] = p;

			// Swap references
			Old.Controller.Zones[Old.Zone.Type].SetDirty();
			New.Controller.Zones[Old.Zone.Type].SetDirty();
			Old.Controller.Zones[New.Zone.Type].SetDirty();
			New.Controller.Zones[New.Zone.Type].SetDirty();
		}

		public IEnumerator<T> GetEnumerator()
		{
			return Entities.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public override string ToString()
		{
			string s = string.Empty;
			foreach (var e in asList)
				s += e + "\n";
			return s;
		}
	}
}
