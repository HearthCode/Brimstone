using System;
using System.Collections.Generic;

namespace Brimstone
{
	public class Minion : Entity, IMinion
	{
		public Minion(Minion cloneFrom) : base(cloneFrom) { }
		public Minion(Game game, Entity controller, Card card, Dictionary<GameTag, int?> tags = null) : base(game, controller, card, tags) {
			this[GameTag.HEALTH] = card[GameTag.HEALTH];
		}

		public IPlayable Play() {
			Game.ActionQueue.Enqueue(CardBehaviour.Play(Controller, this));
			return (IPlayable)(Entity)Game.ActionQueue.Process()[0];
		}

		public void Damage(int amount) {
			Game.ActionQueue.Enqueue(CardBehaviour.Damage(this, amount));
			Game.ActionQueue.Process();
		}

		public void CheckForDeath() {
			if (this[GameTag.HEALTH] <= 0) {
				Console.WriteLine(this + " dies!");
				Game.Opponent.Graveyard.MoveTo(this);
				this[GameTag.DAMAGE] = 0;
				Game.ActionQueue.Enqueue(Card.Behaviour.Deathrattle);
				Game.ActionQueue.Process();
			}
		}

		public override object Clone() {
			return new Minion(this);
		}
	}
}