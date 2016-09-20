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
