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
		private bool mustLookForTargets() {
			if (Card.RequiresTarget)
				return true;

			// Targeted play, if available (e.g. Abusive Sergeant)
			if (Card.Requirements.ContainsKey(PlayRequirements.REQ_TARGET_IF_AVAILABLE))
				return true;

			// Targeted play, if combo is active (e.g. SI:7)
			var controller = (Player)Controller;
			if (Card.HasCombo && Card.Requirements.ContainsKey(PlayRequirements.REQ_TARGET_FOR_COMBO))
				return controller.IsComboActive;

			// Targeted play, if dragon in controller's hand (e.g. Blackwing Corruptor)
			if (Card.Requirements.ContainsKey(PlayRequirements.REQ_TARGET_IF_AVAILABLE_AND_DRAGON_IN_HAND) &&
					controller.Hand.Any(x => x is Minion && ((Minion)x).Race == Race.DRAGON))
					      return true;

			// Targeted play, if controller has a minimum number of minions (e.g. Gormok)
			int minimumFriendlyMinions;
			if (Card.Requirements.TryGetValue(PlayRequirements.REQ_TARGET_IF_AVAILABLE_AND_MINIMUM_FRIENDLY_MINIONS, out minimumFriendlyMinions) &&
					controller.Board.Count >= minimumFriendlyMinions)
				return true;

			// Untargeted play
			return false;
		}

		private bool isValidPlayTarget(ICharacter targetable) {
			return MeetsGenericTargetingRequirements(targetable);
		}

		private List<ICharacter> getValidPlayTargets() {
			if (!mustLookForTargets())
				return new List<ICharacter>();

			var controller = (Player)Controller;

			var board = controller.Board.Concat(controller.Opponent.Board);
			// TODO: Remove select after zones are made generic
			var targets = board.Where(x => isValidPlayTarget((ICharacter)x)).Select(x => (ICharacter)x).ToList();

			var hero = controller.Hero;
			if (isValidPlayTarget(hero))
				targets.Add(hero);

			var opponentHero = controller.Opponent.Hero;
			if (isValidPlayTarget(opponentHero))
				targets.Add(opponentHero);

			return targets;
		}

		private List<ICharacter> getValidAttackTargets() {
			var controller = (Player)Controller;

			if (controller.Opponent.Board.Any(x => ((Minion)x).HasTaunt && !((Minion)x).HasStealth)) {
				// Must attack non-stealthed taunts
				// TODO: Remove select after zones are made generic
				return controller.Opponent.Board.Where(x => ((Minion)x).HasTaunt && !((Minion)x).HasStealth).Select(x => (ICharacter)x).ToList();
			} else {
				// Can attack all opponent characters
				// TODO: Remove select after zones are made generic
				var targets = controller.Opponent.Board.Where(x => !((Minion)x).HasStealth).Select(x => (ICharacter)x).ToList();
				targets.Add(controller.Opponent.Hero);
				return targets;
			}
		}

		public override List<ICharacter> ValidTargets {
			get {
				if (Zone.Type == Brimstone.Zone.HAND)
					return getValidPlayTargets();
				else if (Zone.Type == Brimstone.Zone.PLAY)
					return getValidAttackTargets();
				else {
					// TODO: Should subclass BrimstoneException here
					throw new Exception("Minion can't have targets while in zone " + Zone.Type);
				}
			}
		}

		public override object Clone() {
			return new Minion(this);
		}
	}
}
