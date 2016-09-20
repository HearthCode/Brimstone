using System;
using System.Collections.Generic;
using System.Linq;
using Brimstone.Exceptions;

namespace Brimstone.Entities
{
	public partial class Minion : Character<Minion>
	{
		internal Minion(Minion cloneFrom) : base(cloneFrom) { }
		/// <summary>
		/// Create a new orphaned minion
		/// </summary>
		/// <remarks>The minion is orphaned by default. To attach the minion to a game, set its <see cref="Minion.Zone"/> property.</remarks>
		/// <param name="card">The card on which to base the entity</param>
		/// <param name="tags">Tags to set upon creation</param>
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

		public override IEnumerable<ICharacter> ValidTargets {
			get {
				if (Zone == Controller.Hand)
					return GetValidAbilityTargets();
				if (Zone == Controller.Board)
					return base.ValidTargets;
				throw new TargetingException("Minion can't have targets while in zone " + Zone.Type);
			}
		}

		public override object Clone() {
			return new Minion(this);
		}
	}
}
