using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Brimstone
{
	public partial class Player : Entity, IZoneOwner {
		public string FriendlyName { get; }

		public Deck Deck { get { return (Deck)Zones[Brimstone.Zone.DECK]; } set { Zones[Brimstone.Zone.DECK] = value; } }
		public ZoneEntities Hand { get { return Zones[Brimstone.Zone.HAND]; } }
		public ZoneEntities Board { get { return Zones[Brimstone.Zone.PLAY]; } }
		public ZoneEntities Graveyard { get { return Zones[Brimstone.Zone.GRAVEYARD]; } }
		public ZoneEntities Secrets { get { return Zones[Brimstone.Zone.SECRET]; } }
		public ZoneEntities Setaside { get { return null; } }
		public ZoneGroup Zones { get; private set; }
		public HeroClass HeroClass { get; }

		public Choice Choice { get; set; }

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
				{ GameTag.ZONE, (int) Brimstone.Zone.PLAY }
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

		public int RemainingMana => (BaseMana + TemporaryMana) - (UsedMana + Overload);

		public Choice StartMulligan() {
			MulliganState = MulliganState.INPUT;
			return new Choice(this, Game.Action(this, Actions.MulliganChoice(this)), ChoiceType.MULLIGAN);
		}

		public IPlayable Give(Card card) {
			if (Game.Player1.Choice != null || Game.Player2.Choice != null)
				throw new PendingChoiceException();

			return (IPlayable)(Entity)Game.Action(Game, Actions.Give(this, card));
		}

		public IPlayable Draw() {
			if (Game.Player1.Choice != null || Game.Player2.Choice != null)
				throw new PendingChoiceException();

			return (IPlayable)(Entity)Game.Action(Game, Actions.Draw(this));
		}

		public void Draw(ActionGraph qty) {
			if (Game.Player1.Choice != null || Game.Player2.Choice != null)
				throw new PendingChoiceException();

			Game.Action(this, Actions.Draw(this) * qty);
		}

		public override object Clone() {
			return new Player(this);
		}
	}
}