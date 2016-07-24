using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Brimstone
{
	public partial class Player : Entity, IZoneOwner {
		public string FriendlyName { get; }

		public Deck Deck { get; private set; }
		public ZoneEntities Hand { get; private set; }
		public ZoneEntities Board { get; private set; }
		public ZoneEntities Graveyard { get; private set; }
		public ZoneEntities Secrets { get; private set; }
		public ZoneGroup Zones { get; } = new ZoneGroup();
		public HeroClass HeroClass { get; }

		public Player(Player cloneFrom) : base(cloneFrom) {
			FriendlyName = cloneFrom.FriendlyName;
			HeroClass = cloneFrom.HeroClass;
			// TODO: Shallow clone choices
		}

		public Player(HeroClass hero, string name, int playerId, int teamId = 0) : base(Cards.FromId("Player"),
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
		}

		public void Attach(Game game) {
			Game = game;
			Controller = game;

			Deck = new Deck(Game, HeroClass, this);
			Hand = new ZoneEntities(Game, this, Zone.HAND);
			Board = new ZoneEntities(Game, this, Zone.PLAY);
			Graveyard = new ZoneEntities(Game, this, Zone.GRAVEYARD);
			Secrets = new ZoneEntities(Game, this, Zone.SECRET);
			Zones[Zone.DECK] = Deck;
			Zones[Zone.HAND] = Hand;
			Zones[Zone.PLAY] = Board;
			Zones[Zone.GRAVEYARD] = Graveyard;
			Zones[Zone.SECRET] = Secrets;

			// TODO: Update choices to point to new game entities
		}

		public void Start() {
			// Shuffle deck
			Deck.Shuffle();

			// Generate hero
			Hero = Game.Add(new Hero(DefaultHero.For(HeroClass)), this) as Hero;

			// Draw cards
			Draw((Game.FirstPlayer == this ? 3 : 4));

			// Give 2nd player the coin
			if (Game.FirstPlayer != this)
				Give("The Coin");
		}

		public List<IEntity> StartMulligan() {
			MulliganState = MulliganState.INPUT;
			return Game.ActionQueue.Enqueue(this, CardBehaviour.CreateMulligan);
		}

		public IPlayable Give(Card card) {
			return (IPlayable)(Entity)Game.ActionQueue.Enqueue(Game, CardBehaviour.Give(this, card));
		}

		public IPlayable Draw() {
			return (IPlayable)(Entity)Game.ActionQueue.Enqueue(Game, CardBehaviour.Draw(this));
		}

		public void Draw(ActionGraph qty) {
			Game.ActionQueue.Enqueue(this, CardBehaviour.Draw(this) * qty);
		}

		// TODO: Add Zone move semantic helpers here

		public override object Clone() {
			return new Player(this);
		}
	}
}