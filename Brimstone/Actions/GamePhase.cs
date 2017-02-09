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

using Brimstone.QueueActions;
using Brimstone.Entities;

namespace Brimstone
{
	// All of the game phase actions
	public partial class Actions
	{
		public static ActionGraph StartGame() => new StartGame();
		public static ActionGraph BeginMulligan() => new BeginMulligan();
		public static ActionGraph MulliganChoice(ActionGraph Player = null) { return new CreateChoice { Args = { Player, MulliganSelector, (int)ChoiceType.MULLIGAN } }; }
		public static ActionGraph PerformMulligan() => new PerformMulligan();
		public static ActionGraph WaitForMulliganComplete() => new WaitForMulliganComplete();
		public static ActionGraph BeginTurn() => new BeginTurn();
		public static ActionGraph BeginTurnTriggers() => new BeginTurnTriggers();
		public static ActionGraph BeginTurnForPlayer() => new BeginTurnForPlayer();
		public static ActionGraph EndTurn() => new EndTurn();
		public static ActionGraph EndTurnForPlayer() => new EndTurnForPlayer();
		public static ActionGraph EndTurnCleanupForPlayer() => new EndTurnCleanupForPlayer();
	}
}
