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
using Brimstone.Entities;

namespace Brimstone
{
	public class Environment : ICloneable
	{
		public Game Game { get; set; }

		public Environment(Game game) {
			Game = game;
		}

		private int _lastDamaged;
		public ICharacter LastDamaged {
			get { return (ICharacter) Game.Entities[_lastDamaged]; }
			set { _lastDamaged = value.Id; }
		}

		private int _lastCardPlayed;
		public IEntity LastCardPlayed {
			get { return (IEntity)Game.Entities[_lastCardPlayed]; }
			set { _lastCardPlayed = value.Id; }
		}

		private int _lastCardDiscarded;
		public IEntity LastCardDiscarded {
			get { return (IEntity)Game.Entities[_lastCardDiscarded]; }
			set { _lastCardDiscarded = value.Id; }
		}

		public object Clone() {
			return MemberwiseClone();
		}
	}
}
