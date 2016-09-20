using System.Collections.Generic;

namespace Brimstone
{
	// All of the fields you can add to a card definition
	internal class CardBehaviourGraph
	{
		public ActionGraph Battlecry;
		public ActionGraph Deathrattle;

		public Dictionary<TriggerType, Trigger> Triggers = new Dictionary<TriggerType, Trigger>();
	}
}
