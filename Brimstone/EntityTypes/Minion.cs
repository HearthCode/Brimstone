using System;
using System.Collections.Generic;

namespace Brimstone
{
	public partial class Minion : CanBeDamaged, IMinion
	{
		public Minion(Minion cloneFrom) : base(cloneFrom) { }
		public Minion(Game game, IEntity controller, Card card, Dictionary<GameTag, int> tags = null) : base(game, controller, card, tags) { }

		public IPlayable Play() {
			return (IPlayable) (Entity) Game.ActionQueue.EnqueueSingleResult(CardBehaviour.Play((Entity) Controller, this));
		}

		public void Hit(int amount) {
			Game.ActionQueue.Enqueue(CardBehaviour.Damage(this, amount));
		}

		public void CheckForDeath() {
			if (Health <= 0) {
				Console.WriteLine(Card.Name + " dies!");
				((Player)Controller).Graveyard.MoveTo(this);
				Damage = 0;
				Game.ActionQueue.Enqueue(Card.Behaviour.Deathrattle);
			}
		}

		public override object Clone() {
			return new Minion(this);
		}
	}
}