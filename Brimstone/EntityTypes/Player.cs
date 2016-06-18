using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Brimstone
{
	public partial class Player : Entity, IZones {
		public string FriendlyName { get; }

		public Deck Deck { get; private set; }
		public ZoneEntities Hand { get; private set; }
		public ZoneEntities InPlay { get; private set; }
		public ZoneEntities Graveyard { get; private set; }
		public ZoneEntities Secrets { get; private set; }
		public ZoneGroup Zones { get; } = new ZoneGroup();
		public Card StartingHeroCard { get; }

		public Player(Player cloneFrom) : base(cloneFrom) {
			FriendlyName = cloneFrom.FriendlyName;
			StartingHeroCard = cloneFrom.StartingHeroCard;
		}

		public Player(Game game, Card hero, string name) : base(game, game, Cards.Find["Player"],
			new Dictionary<GameTag, int> {
				{ GameTag.PLAYSTATE, (int) PlayState.PLAYING },
				{ GameTag.MAXHANDSIZE, 10 },
				{ GameTag.ZONE, (int) Zone.PLAY },
				{ GameTag.MAXRESOURCES, 10 }
			}) {
			StartingHeroCard = hero;
			FriendlyName = name;
			setZones();
		}

		public void Attach(Game game) {
			Game = game;
			Controller = game;
			setZones();
		}

		private void setZones() {
			Deck = new Deck(Game, this);
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

		public void Start() {
			// Generate hero
			new Hero(Game, this, StartingHeroCard);

			Deck.Shuffle();
			MulliganState = MulliganState.INPUT;
		}

		public IPlayable Give(Card card) {
			return (IPlayable)(Entity)Game.ActionQueue.EnqueueSingleResult(CardBehaviour.Give(this, card));
		}

		// TODO: Add Zone move semantic helpers here

		public override object Clone() {
			return new Player(this);
		}
	}
}