﻿using System;
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
			return (IPlayable) (Entity) Game.ActionQueue.EnqueueSingleResult(CardBehaviour.Play(Controller, this));
		}

		public void Damage(int amount) {
			Game.ActionQueue.Enqueue(CardBehaviour.Damage(this, amount));
		}

		public void CheckForDeath() {
			if (this[GameTag.HEALTH] <= 0) {
				Console.WriteLine(this + " dies!");
				Game.Opponent.Graveyard.MoveTo(this);
				this[GameTag.DAMAGE] = 0;
				Game.ActionQueue.Enqueue(Card.Behaviour.Deathrattle);
			}
		}

		public override object Clone() {
			return new Minion(this);
		}
	}
}