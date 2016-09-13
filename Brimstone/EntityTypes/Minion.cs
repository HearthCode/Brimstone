using System;
using System.Collections.Generic;
using System.Linq;

namespace Brimstone
{
	public partial class Minion : Character<Minion>
	{
		public Minion(Minion cloneFrom) : base(cloneFrom) { }
		public Minion(Card card, Dictionary<GameTag, int> tags = null) : base(card, tags) { }

		// Checks if it is currently possible to play this minion with a target. Does not check if a suitable target is available
		protected override bool NeedsTargetList() {
			if (Card.RequiresTarget || Card.RequiresTargetIfAvailable)
				return true;

			// Targeted play, if combo is active (e.g. SI:7)
			if (Card.HasCombo && Card.Requirements.ContainsKey(PlayRequirements.REQ_TARGET_FOR_COMBO))
				return Controller.IsComboActive;

			// Targeted play, if dragon in controller's hand (e.g. Blackwing Corruptor)
			if (Card.Requirements.ContainsKey(PlayRequirements.REQ_TARGET_IF_AVAILABLE_AND_DRAGON_IN_HAND) &&
					Controller.Hand.Any(x => x is Minion && ((Minion)x).Race == Race.DRAGON))
				return true;

			// Targeted play, if controller has a minimum number of minions (e.g. Gormok)
			int minimumFriendlyMinions;
			if (Card.Requirements.TryGetValue(PlayRequirements.REQ_TARGET_IF_AVAILABLE_AND_MINIMUM_FRIENDLY_MINIONS, out minimumFriendlyMinions) &&
					Controller.Board.Count >= minimumFriendlyMinions)
				return true;

			// Untargeted play
			return false;
		}

		private IEnumerable<ICharacter> GetValidBattlecryTargets() {
			if (!NeedsTargetList())
				return new List<ICharacter>();

			return Game.Characters.Where(MeetsGenericTargetingRequirements);
		}

		public override IEnumerable<ICharacter> ValidTargets {
			get {
				if (Zone == Controller.Hand)
					return GetValidBattlecryTargets();
				if (Zone == Controller.Board)
					return GetValidAttackTargets();
				throw new TargetingException("Minion can't have targets while in zone " + Zone.Type);
			}
		}

		public override object Clone() {
			return new Minion(this);
		}
	}
}
