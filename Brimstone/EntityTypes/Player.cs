using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Brimstone
{
	public partial class Player : Entity, IZoneOwner {
		public string FriendlyName { get; }

		public Deck Deck { get { return (Deck)Zones[Zone.DECK]; } set { Zones[Zone.DECK] = value; } }
		public ZoneEntities Hand { get { return Zones[Zone.HAND]; } }
		public ZoneEntities Board { get { return Zones[Zone.PLAY]; } }
		public ZoneEntities Graveyard { get { return Zones[Zone.GRAVEYARD]; } }
		public ZoneEntities Secrets { get { return Zones[Zone.SECRET]; } }
		public ZoneGroup Zones { get; private set; }
		public HeroClass HeroClass { get; }

		public Player(Player cloneFrom) : base(cloneFrom) {
			FriendlyName = cloneFrom.FriendlyName;
			HeroClass = cloneFrom.HeroClass;
			// TODO: Shallow clone choices
			// TODO: Update choices to point to new game entities
		}

		public Player(HeroClass hero, string name, int playerId, int teamId = 0) : base(Cards.FromId("Player"),
			new Dictionary<GameTag, int> {
				{ GameTag.MAXHANDSIZE, 10 },
				{ GameTag.MAXRESOURCES, 10 },
				{ GameTag.PLAYER_ID, playerId },
				{ GameTag.TEAM_ID, (teamId != 0? teamId : playerId) },
				{ GameTag.STARTHANDSIZE, 4 },
				{ GameTag.ZONE, (int) Zone.PLAY }
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

				// Create zones
				Zones = new ZoneGroup(Game, this);
			}
		}

		public List<IEntity> StartMulligan() {
			MulliganState = MulliganState.INPUT;
			return Game.Action(this, Actions.CreateMulligan);
		}

		public IPlayable Give(Card card) {
			return (IPlayable)(Entity)Game.Action(Game, Actions.Give(this, card));
		}

		public IPlayable Draw() {
			return (IPlayable)(Entity)Game.Action(Game, Actions.Draw(this));
		}

		public void Draw(ActionGraph qty) {
			Game.Action(this, Actions.Draw(this) * qty);
		}

		// TODO: Add Zone move semantic helpers here

		public override object Clone() {
			return new Player(this);
		}
	}
}