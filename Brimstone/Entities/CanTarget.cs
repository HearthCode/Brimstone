using System.Collections.Generic;
using System.Linq;
using Brimstone.Exceptions;

namespace Brimstone.Entities
{
	public partial interface ICanTarget : IEntity
	{
		// TODO: HasTarget
		IEnumerable<ICharacter> ValidTargets { get; }

		// TODO: Add cloning code + cloning unit test
		ICharacter Target { get; set; }
	}

	public abstract partial class CanTarget : Entity, ICanTarget
	{
		public ICharacter Target { get; set; }

		protected internal CanTarget(CanTarget cloneFrom) : base(cloneFrom) {}
		protected CanTarget(Card card, Dictionary<GameTag, int> tags = null) : base(card, tags) {}

		// Checks if target entity meets the targeting requirements that are shared by spells and battlecries
		// Note that additional targeting requirements for spells, battlecries and hero powers are not checked here
		protected internal virtual bool MeetsGenericTargetingRequirements(ICharacter target) {
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
						if (target.Race != (Race) param)
							return false;
						break;
					case PlayRequirements.REQ_HERO_TARGET:
						if (target.Card.Type != CardType.HERO)
							return false;
						break;
					case PlayRequirements.REQ_TARGET_MIN_ATTACK:
						if (target.AttackDamage < param)
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
						throw new PlayRequirementException("Card requirement not implemented: " + req);
				}
			}
			return true;
		}

		// Default definition of whether the entity currently requires a target list to be calculated before use
		protected internal virtual bool NeedsTargetList() {
			return Card.RequiresTarget || Card.RequiresTargetIfAvailable;
		}

		// Default targeting for spells and hero powers
		protected internal virtual IEnumerable<ICharacter> GetValidAbilityTargets() {
			// If this is an untargeted card, return an empty list
			if (!NeedsTargetList())
				return new List<ICharacter>();

			return Game.Characters.Where(MeetsGenericTargetingRequirements);
		}

		// TODO: Cache ValidTargets
		public abstract IEnumerable<ICharacter> ValidTargets { get; }
	}
}
