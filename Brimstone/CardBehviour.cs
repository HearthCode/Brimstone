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
	public class CardBehaviour
	{
		public List<QueueAction> Battlecry;
		public List<QueueAction> Deathrattle;
		public Dictionary<Zone, Dictionary<TriggerType, Trigger>> TriggersByZone;

		public Dictionary<TriggerType, Trigger> Triggers {
			get { return TriggersByZone[Zone.PLAY]; }
			set { TriggersByZone[Zone.PLAY] = value; }
		}

		// Compile all the ActionGraph fields in a Behaviour into lists of QueueActions
		internal static CardBehaviour FromGraph(CardBehaviourGraph b) {
			var compiled = new CardBehaviour();
			var behaviourList = new List<string>();

			// Find all the fields we can compile
			var behaviourClass = typeof(CardBehaviourGraph).GetFields();
			foreach (var field in behaviourClass)
				if (field.FieldType == typeof(ActionGraph))
					behaviourList.Add(field.Name);

			// Compile each field that exists
			foreach (var fieldName in behaviourList) {
				var field = b.GetType().GetField(fieldName);
				ActionGraph fieldValue = field.GetValue(b) as ActionGraph;
				if (fieldValue != null) {
					compiled.GetType().GetField(fieldName).SetValue(compiled, fieldValue.Unravel());
				} else
					compiled.GetType().GetField(fieldName).SetValue(compiled, new List<QueueAction>());
			}

			// Copy event triggers
			compiled.TriggersByZone = b.TriggersByZone;

			return compiled;
		}
	}
}
