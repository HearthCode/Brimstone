using System;
using System.Collections.Generic;

namespace Brimstone
{
	public class Player : Entity
	{
		public string FriendlyName { get; set; }
		public List<Entity>[] Zones { get; } = new List<Entity>[(int)Zone._COUNT];

		public List<Entity> Hand { get { return Zones[(int) Zone.HAND]; } }
		public List<Entity> Board { get { return Zones[(int) Zone.PLAY]; } }

		public Player(Player cloneFrom) : base(cloneFrom) {
			FriendlyName = cloneFrom.FriendlyName;
			for (int i = 0; i < (int)Zone._COUNT; i++)
				Zones[i] = new List<Entity>();
			foreach (var e in cloneFrom.Hand)
				Hand.Add(e.Clone() as Entity);
			foreach (var e in cloneFrom.Board)
				Board.Add(e.Clone() as Entity);
		}

		public Player(Game game = null, Dictionary<GameTag, int?> tags = null) : base(game, game, Cards.Find["Player"], tags) {
			this[GameTag.HEALTH] = 30;
			for (int i = 0; i < (int)Zone._COUNT; i++)
				Zones[i] = new List<Entity>();
		}

		public IPlayable Give(Card card) {
			Game.ActionQueue.Enqueue(CardBehaviour.Give(this, card));
			return (IPlayable)(Entity)Game.ActionQueue.Process()[0];
		}

		public override string ToString() {
			return FriendlyName;
		}

		public override object Clone() {
			return new Player(this);
		}
	}
}