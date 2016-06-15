using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Brimstone
{
	public class Player : Entity
	{
		public string FriendlyName { get; set; }

		public ZoneEntities Deck { get; private set; }
		public ZoneEntities Hand { get; private set; }
		public ZoneEntities InPlay { get; private set; }
		public ZoneEntities Graveyard { get; private set; }
		public ZoneEntities Secrets { get; private set; }
		public ZoneGroup Zones { get; } = new ZoneGroup();

		public Player(Player cloneFrom) : base(cloneFrom) {
			FriendlyName = cloneFrom.FriendlyName;
		}

		public Player(Game game = null, Dictionary<GameTag, int?> tags = null) : base(game, game, Cards.Find["Player"], tags) {
			this[GameTag.HEALTH] = 30;
			setZones();
		}

		public void Attach(Game game) {
			Game = game;
			Controller = game;
			setZones();
		}

		private void setZones() {
			if (Game == null)
				return;
			Deck = new ZoneEntities(Game, this, Zone.DECK);
			Hand = new ZoneEntities(Game, this, Zone.HAND);
			InPlay = new ZoneEntities(Game, this, Zone.PLAY);
			Graveyard = new ZoneEntities(Game, this, Zone.GRAVEYARD);
			Secrets = new ZoneEntities(Game, this, Zone.SECRET);
			Zones[Zone.DECK] = Deck;
			Zones[Zone.HAND] = Hand;
			Zones[Zone.PLAY] = InPlay;
			Zones[Zone.GRAVEYARD] = Graveyard;
			Zones[Zone.SECRET] = Secrets;
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