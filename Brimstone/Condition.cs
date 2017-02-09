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
using System.Linq;
using Brimstone.QueueActions;
using Brimstone.Entities;

namespace Brimstone
{
	public class Condition
	{
		private readonly Func<IEntity, IEntity, bool> condition;

		public Condition(Func<IEntity, IEntity, bool> Condition) {
			condition = Condition;
		}

		public bool Eval(IEntity owner, IEntity affected) {
			return condition(owner, affected);
		}

		public static implicit operator Condition(Selector s) {
			return new Condition((x, y) => s.Lambda(x).Contains(y));
		}
	}
}
