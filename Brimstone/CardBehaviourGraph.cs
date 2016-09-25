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
