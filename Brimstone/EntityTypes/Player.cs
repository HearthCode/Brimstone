using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Brimstone
{
	public partial class Player : Entity, IZoneController {
		public string FriendlyName { get; }

		public Deck Deck { get { return (Deck)Zones[Brimstone.Zone.DECK]; } set { Zones[Brimstone.Zone.DECK] = value; } }
		public Zone<IPlayable> Hand { get { return (Zone<IPlayable>) Zones[Brimstone.Zone.HAND]; } }
		public Zone<Minion> Board { get { return (Zone<Minion>) Zones[Brimstone.Zone.PLAY]; } }
		public Zone<ICharacter> Graveyard { get { return (Zone<ICharacter>) Zones[Brimstone.Zone.GRAVEYARD]; } }
		public Zone<Spell> Secrets { get { return (Zone<Spell>) Zones[Brimstone.Zone.SECRET]; } }
		public Zone<IPlayable> Setaside { get { return null; } }
		public Zones Zones { get; private set; }
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
				Zones = new Zones(Game, this);
			}
		}

		public int RemainingMana => (BaseMana + TemporaryMana) - (UsedMana + Overload);

		// All the entities that we can potentially play or attack with when it's our turn
		public IEnumerable<IEntity> LiveEntities => Hand.Concat(Board).Concat(new List<IEntity> {this, /* HeroPower */});

		public Choice StartMulligan() {
			MulliganState = MulliganState.INPUT;
			return new Choice(this, Game.Action(this, Actions.MulliganChoice(this)), ChoiceType.MULLIGAN);
		}

		public IPlayable Give(Card card) {
			if (Game.Player1.Choice != null || Game.Player2.Choice != null)
				throw new ChoiceException();

			return (IPlayable)(Entity) Game.Action(Game, Actions.Give(this, card));
		}

		public T Draw<T>() where T : Entity {
			if (Game.Player1.Choice != null || Game.Player2.Choice != null)
				throw new ChoiceException();

			return (T) Game.Action(Game, Actions.Draw(this));
		}

		public void Draw(ActionGraph qty) {
			if (Game.Player1.Choice != null || Game.Player2.Choice != null)
				throw new ChoiceException();

			Game.Action(this, Actions.Draw(this) * qty);
		}

		public void Concede() {
			Game.Action(this, Actions.Concede(this));
		}

		public override object Clone() {
			return new Player(this);
		}
	}
}