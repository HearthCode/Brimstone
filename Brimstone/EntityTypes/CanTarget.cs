using System.Collections.Generic;
using System.Linq;

namespace Brimstone
{
	public interface ICanTarget : IEntity
	{	
		// TODO: Caching
		// TODO: HasTarget
		List<ICharacter> ValidTargets { get; }

		// TODO: Add cloning code + cloning unit test
		ICharacter Target { get; set; }
	}

	public abstract class CanTarget : Entity, ICanTarget
	{
		public ICharacter Target { get; set; }

		public abstract List<ICharacter> ValidTargets { get; }

		protected CanTarget(CanTarget cloneFrom) : base(cloneFrom) { }
		protected CanTarget(Card card, Dictionary<GameTag, int> tags = null) : base(card, tags) { }

		// Checks if target entity meets the targeting requirements that are shared by spells and battlecries
		// Note that additional targeting requirements for spells, battlecries and hero powers are not checked here
		protected bool MeetsGenericTargetingRequirements(ICharacter target) {
			Minion minion = target as Minion;

			// Can't target your opponent's stealth minions
			if (minion != null && minion.HasStealth && minion.Controller != Controller)
				return false;

			if (target.CantBeTargetedByOpponents && target.Controller != Controller)
				return false;

			foreach (var item in Card.Requirements) {
				var req = item.Key;
				var param = item.Value;

				switch (req) {
					case PlayRequirements.REQ_MINION_TARGET:
						if (minion == null)
							return false;
						break;
					case PlayRequirements.REQ_FRIENDLY_TARGET:
						if (target.Controller != Controller)
							return false;
						break;
					case PlayRequirements.REQ_ENEMY_TARGET:
						if (target.Controller == Controller)
							return false;
						break;
					case PlayRequirements.REQ_DAMAGED_TARGET:
						if (target.Damage == 0)
							return false;
						break;
					case PlayRequirements.REQ_FROZEN_TARGET:
						if (!target.IsFrozen)
							return false;
						break;
					case PlayRequirements.REQ_TARGET_WITH_RACE:
						if (target.Race != (Race)param)
							return false;
						break;
					case PlayRequirements.REQ_HERO_TARGET:
						if (target.Card.Type != CardType.HERO)
							return false;
						break;
					case PlayRequirements.REQ_TARGET_MIN_ATTACK:
						if (target.Attack < param)
							return false;
						break;
					case PlayRequirements.REQ_MUST_TARGET_TAUNTER:
						if (minion == null || !minion.HasTaunt)
							return false;
						break;
					case PlayRequirements.REQ_UNDAMAGED_TARGET:
						if (target.Damage > 0)
							return false;
						break;
					case PlayRequirements.REQ_LEGENDARY_TARGET:
						if (target.Card.Rarity != Rarity.LEGENDARY)
							return false;
						break;
					case PlayRequirements.REQ_TARGET_WITH_DEATHRATTLE:
						if (minion == null || !minion.HasDeathrattle)
							return false;
						break;

					// The following cases are used to determine if a target is required at all, while this method checks if a given target is valid
					// Thus, we ignore these cases here
					case PlayRequirements.REQ_TARGET_TO_PLAY:
					case PlayRequirements.REQ_TARGET_FOR_COMBO:
					case PlayRequirements.REQ_TARGET_IF_AVAILABLE:
					case PlayRequirements.REQ_TARGET_IF_AVAILABLE_AND_DRAGON_IN_HAND:
					case PlayRequirements.REQ_TARGET_IF_AVAILABLE_AND_MINIMUM_FRIENDLY_MINIONS:
						break;

					default:
						throw new PlayRequirementNotImplementedException("Card requirement not implemented: " + req);
				}
			}
			return true;
		}

		protected List<ICharacter> GetValidAttackTargets() {
			var controller = (Player)Controller;

			if (controller.Opponent.Board.Any(x => x.HasTaunt && !x.HasStealth)) {
				// Must attack non-stealthed taunts
				return controller.Opponent.Board.Where(x => x.HasTaunt && !x.HasStealth).ToList<ICharacter>();
			}
			else {
				// Can attack all opponent characters
				var targets = controller.Opponent.Board.Where(x => !x.HasStealth).ToList<ICharacter>();
				targets.Add(controller.Opponent.Hero);
				return targets;
			}
		}
	}
}
