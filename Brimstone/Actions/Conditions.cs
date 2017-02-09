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

using Brimstone.Entities;

namespace Brimstone
{
	// All of the trigger conditions you can use
	public partial class Actions
	{
		public static Condition IsSelf { get; } = new Condition((me, other) => me == other);
		public static Condition IsFriendlySpell { get; } = new Condition((me, other) => me.Controller == other.Controller && other is Spell);
		public static Condition IsFriendly { get; } = new Condition((me, other) => me.Controller == other.Controller);
		public static Condition IsFriendlyMinion { get; } = new Condition((me, other) => me.Controller == other.Controller && other is Minion);
		public static Condition IsOpposing { get; } = new Condition((me, other) => me.Controller == other.Controller.Opponent);
	}
}
