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
		public HeroClass HeroClass { get; }

		public Player(Player cloneFrom) : base(cloneFrom) {
			FriendlyName = cloneFrom.FriendlyName;
			HeroClass = cloneFrom.HeroClass;
		}

		public Player(Game game, HeroClass hero, string name, int playerId, int teamId = 0) : base(game, game, Cards.Find["Player"],
			new Dictionary<GameTag, int> {
				{ GameTag.PLAYSTATE, (int) PlayState.PLAYING },
				{ GameTag.MAXHANDSIZE, 10 },
				{ GameTag.ZONE, (int) Zone.PLAY },
				{ GameTag.MAXRESOURCES, 10 },
				{ GameTag.PLAYER_ID, playerId },
				{ GameTag.TEAM_ID, (teamId != 0? teamId : playerId) },
				{ GameTag.STARTHANDSIZE, 4 }
			}) {
			HeroClass = hero;
			FriendlyName = name;
			setZones();
		}

		public void Attach(Game game) {
			Game = game;
			Controller = game;
			setZones();
		}

		private void setZones() {
			Deck = new Deck(Game, HeroClass, this);
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
			// Shuffle deck
			Deck.Shuffle();

			// Generate hero
			new Hero(Game, this, DefaultHero.For(HeroClass));
		}

		public void StartMulligan() {
			MulliganState = MulliganState.INPUT;
		}

		public IPlayable Give(Card card) {
			return (IPlayable)(Entity)Game.ActionQueue.EnqueueSingleResult(CardBehaviour.Give(this, card));
		}

		public IPlayable Draw() {
			return (IPlayable)(Entity)Game.ActionQueue.EnqueueSingleResult(CardBehaviour.Draw(this));
		}

		// TODO: Add Zone move semantic helpers here

		public override object Clone() {
			return new Player(this);
		}
	}
}