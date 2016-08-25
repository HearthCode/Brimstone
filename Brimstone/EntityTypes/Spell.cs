using System;
using System.Collections.Generic;
using System.Linq;

namespace Brimstone
{
	public class Spell : Entity, ISpell
	{
		public Spell(Spell cloneFrom) : base(cloneFrom) { }
		public Spell(Card card, Dictionary<GameTag, int> tags = null) : base(card, tags) { }

		public IPlayable Play(Character target = null) {
			if (Game.Player1.Choice != null || Game.Player2.Choice != null)
				throw new PendingChoiceException();

			// TODO: Check targeting validity
			Target = target;
			return (IPlayable)(Entity)Game.Action(this, Actions.Play(this));
		}

		private bool isValidTarget(Character targetable) {
			Minion minion = targetable as Minion;
			if (minion != null && minion.CantBeTargetedByAbilities)
				return false;

			return this.MeetsGenericTargetingRequirements(targetable);
		}

		public List<IEntity> ValidTargets {
			get {
				// If this is an untargeted spell, return an empty list
				if (!Card.RequiresTargetIfAvailable && !Card.RequiresTarget)
					return new List<IEntity>();

				var controller = (Player)Controller;

				var board = controller.Board.Concat(controller.Opponent.Board);
				var targets = board.Where(x => isValidTarget((Character)x)).ToList();

				var hero = controller.Hero;
				if (isValidTarget(hero))
					targets.Add(hero);

				var opponentHero = controller.Opponent.Hero;
				if (isValidTarget(opponentHero))
					targets.Add(opponentHero);

				return targets;
			}
		}

		public override object Clone() {
			return new Spell(this);
		}
	}
}
