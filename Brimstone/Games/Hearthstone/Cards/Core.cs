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

using Brimstone.Entities;
using static Brimstone.Actions;
using static Brimstone.TriggerType;
using Behaviour = Brimstone.CardBehaviourGraph;

namespace Brimstone.Games.Hearthstone
{
	internal partial class Cards
	{
		// Game
		internal static Behaviour Game = new Behaviour {
			Triggers =
			{
				[OnGameStart]			= StartGame(),
				[OnBeginMulligan]		= BeginMulligan(),
				[OnEndTurnTransition]	= EndTurn()
			}
		};

		// Player
		internal static Behaviour Player = new Behaviour {
			Triggers = {
				[OnDealMulligan]		= IsSelf > PerformMulligan(),
				[OnMulliganWaiting]		= IsSelf > WaitForMulliganComplete(),
				[OnBeginTurnTransition]	= IsFriendly > BeginTurn(),
				[OnBeginTurn]			= IsFriendly > BeginTurnTriggers(),
				[OnBeginTurnForPlayer]	= IsFriendly > BeginTurnForPlayer(),
				[OnWaitForAction]		= IsFriendly > (ActionGraph)(System.Action<IEntity>)(p => { p.Game.NextStep = Step.MAIN_END; }),
				[OnEndTurn]				= IsFriendly > EndTurnForPlayer(),
				[OnEndTurnCleanup]		= IsFriendly > EndTurnCleanupForPlayer(),
				[OnHeroPower]			= IsFriendly > (ActionGraph)(System.Action<IEntity>)(p => { ((Player)p).NumTimesHeroPowerUsedThisGame++; })
			}
		};
	}
}
