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
			// TODO: Update choices to point to new game entities
		}

		public Player(HeroClass hero, string name, int playerId, int teamId = 0) : base(Cards.FromId("Player"),
			new Dictionary<GameTag, int> {
				{ GameTag.PLAYSTATE, (int) PlayState.PLAYING },
				{ GameTag.MAXHANDSIZE, 10 },
				{ GameTag.MAXRESOURCES, 10 },
				{ GameTag.PLAYER_ID, playerId },
				{ GameTag.TEAM_ID, (teamId != 0? teamId : playerId) },
				{ GameTag.STARTHANDSIZE, 4 }
			}) {
			HeroClass = hero;
			FriendlyName = name;
		}

		public override Game Game {
			get {
				return base.Game;
			}
			set {
				base.Game = value;
				Zones[Zone.DECK] = Deck = new Deck(value, HeroClass, this);
				Zones[Zone.HAND] = Hand = new ZoneEntities(value, this, Zone.HAND);
				Zones[Zone.PLAY] = Board = new ZoneEntities(value, this, Zone.PLAY);
				Zones[Zone.GRAVEYARD] = Graveyard = new ZoneEntities(value, this, Zone.GRAVEYARD);
				Zones[Zone.SECRET] = Secrets = new ZoneEntities(value, this, Zone.SECRET);
			}
		}

		public void Start() {
			// Shuffle deck
			Deck.Shuffle();

			// Generate hero
			Hero = Game.Add(new Hero(DefaultHero.For(HeroClass)), this) as Hero;

			// Draw cards
			Draw((Game.FirstPlayer == this ? 3 : 4));
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