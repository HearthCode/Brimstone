using System;
using System.Collections;
using System.Collections.Generic;

namespace Brimstone
{
	public class EntityController : IEnumerable<IEntity>, ICloneable
	{
		public Game Game { get; }
		public int NextEntityId = 1;
		private Dictionary<int, IEntity> Entities = new Dictionary<int, IEntity>();

		public IEntity this[int id]
		{
			get { return Entities[id]; }
		}

		public int Count
		{
			get { return Entities.Count; }
		}

		public ICollection<int> Keys
		{
			get { return Entities.Keys; }
		}

		public bool ContainsKey(int key)
		{
			return Entities.ContainsKey(key);
		}

		public EntityController(Game game)
		{
			Game = game;
			Game.Controller = game;

			// Fuzzy hashing
			Changed = false;
			Add(game);
		}

		public EntityController(EntityController es)
		{
			_gameHash = es._gameHash;
			Changed = es.Changed;

			NextEntityId = es.NextEntityId;
			foreach (var entity in es)
			{
				Entities.Add(entity.Id, (IEntity) entity.Clone());
			}
			// Change ownership
			Game = FindGame();
			foreach (var entity in Entities)
				entity.Value.Game = Game;
			foreach (var entity in Entities)
				entity.Value.Controller = (IZoneController) Entities[es.Entities[entity.Key].Controller.Id];

			// Do this last so that changing Controller doesn't trigger EntityChanging
			Game.Entities = this;
		}

		public IEntity Add(IEntity entity)
		{
			entity.Game = Game;
			entity.Id = NextEntityId++;
			Entities[entity.Id] = entity;
			EntityChanging(entity.Id, 0);
			if (Game.PowerHistory != null)
				Game.PowerHistory.Add(new CreateEntity(entity));
			Game.ActiveTriggers.Add(entity);
			return entity;
		}

		public Game FindGame()
		{
			// Game is always entity ID 1
			return (Game) Entities[1];
		}

		public Player FindPlayer(int p)
		{
			// Player is always p+1
			return (Player) Entities[p + 1];
		}

		// Calculate a fuzzy hash for the whole game state
		// WARNING: The hash algorithm MUST be designed in such a way that the order
		// in which the entities are hashed doesn't matter
		private int _gameHash = 0;
		private bool _changed = false;

		public bool Changed
		{
			get { return _changed; }
			set
			{
				int dummy;
				if (!value)
					dummy = FuzzyGameHash;
				else
					_changed = true;
			}
		}

		public void EntityChanging(int id, int previousHash)
		{
			if (Settings.GameHashCaching)
				_changed = true;
		}

		public void EntityChanged(int id, GameTag tag, int value)
		{
			if (Game.PowerHistory != null)
				Game.PowerHistory.Add(new TagChange(id, tag, value));
		}

		public int FuzzyGameHash
		{
			get
			{
				// TODO: Take order-of-play semantics into account
				if (!Settings.GameHashCaching || _changed)
				{
					_gameHash = 0;
					// Hash board states (play zones) for both players in order, hash rest of game entities in any order
					foreach (var entity in Entities.Values)
						if (entity.Zone.Type != Zone.PLAY || entity.ZonePosition == 0)
							_gameHash += entity.FuzzyHash;
						else
							_gameHash += (entity.Controller.Id*8 + entity.ZonePosition)*entity.FuzzyHash;
					_changed = false;
				}
				return _gameHash;
			}
		}

		public IEnumerator<IEntity> GetEnumerator()
		{
			return Entities.Values.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public object Clone()
		{
			return new EntityController(this);
		}
	}
}