using System;
using System.Collections.Generic;

namespace Brimstone
{
	public class Minion : Entity, IMinion
	{
		public Minion(Minion cloneFrom) : base(cloneFrom) { }
		public Minion(Game game, IEntity controller, Card card, Dictionary<GameTag, int?> tags = null) : base(game, controller, card, tags) {
			// TODO: Move to entity creation
			this[GameTag.HEALTH] = card[GameTag.HEALTH];
		}

		public IPlayable Play() {
			return (IPlayable) (Entity) Game.ActionQueue.EnqueueSingleResult(CardBehaviour.Play((Entity) Controller, this));
		}

		public void Damage(int amount) {
			Game.ActionQueue.Enqueue(CardBehaviour.Damage(this, amount));
		}

		public void CheckForDeath() {
			if (this[GameTag.HEALTH] <= 0) {
				Console.WriteLine(Card.Name + " dies!");
				((Player)Controller).Graveyard.MoveTo(this);
				this[GameTag.DAMAGE] = 0;
				Game.ActionQueue.Enqueue(Card.Behaviour.Deathrattle);
			}
		}

		public override object Clone() {
			return new Minion(this);
		}
	}
}