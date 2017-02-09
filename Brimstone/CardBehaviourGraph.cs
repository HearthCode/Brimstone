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

namespace Brimstone
{
	// All of the fields you can add to a card definition
	internal class CardBehaviourGraph
	{
		public ActionGraph Battlecry;
		public ActionGraph Deathrattle;

		public Dictionary<Zone, Dictionary<TriggerType, Trigger>> TriggersByZone =
			new Dictionary<Zone, Dictionary<TriggerType, Trigger>> {
				[Zone.PLAY] = new Dictionary<TriggerType, Trigger>(),
				[Zone.HAND] = new Dictionary<TriggerType, Trigger>(),
				[Zone.DECK] = new Dictionary<TriggerType, Trigger>(),
				[Zone.GRAVEYARD] = new Dictionary<TriggerType, Trigger>(),
				[Zone.SECRET] = new Dictionary<TriggerType, Trigger>()
			};

		public Dictionary<TriggerType, Trigger> Triggers {
			get { return TriggersByZone[Zone.PLAY]; }
			set { TriggersByZone[Zone.PLAY] = value; }
		}
	}
}
