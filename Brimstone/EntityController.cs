/*
	Copyright 2016, 2017 Katy Coe
	Copyright 2016 Timothy Stiles

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
using Brimstone.Entities;

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
			Add(game);
		}

		public EntityController(EntityController es)
		{
			NextEntityId = es.NextEntityId;
			foreach (var entity in es)
				Entities.Add(entity.Id, (IEntity) entity.Clone());
			// Change ownership
			Game = FindGame();
			foreach (var entity in Entities.Values)
				entity.Game = Game;
			Game.Entities = this;
		}

		public IEntity Add(IEntity entity)
		{
			entity.Game = Game;
			entity.Id = NextEntityId++;
			Entities[entity.Id] = entity;
			Game.EntityCreated(entity);
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
