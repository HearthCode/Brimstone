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

using System.Collections.Generic;
using Brimstone.QueueActions;

namespace Brimstone
{
	public class Trigger
	{
		public Condition Condition { get; }
		public List<QueueAction> Action { get; }= new List<QueueAction>();

		public Trigger(ActionGraph action, Condition condition = null) {
			Condition = condition;
			Action = action.Unravel();
		}

		public Trigger(Trigger t) {
			// Only a shallow copy is necessary because Args and Action don't change and are lazily evaluated
			Condition = t.Condition;
			Action = t.Action;
		}

		public static implicit operator Trigger(ActionGraph g) {
			return new Trigger(g);
		}
	}
}
