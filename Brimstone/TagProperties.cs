namespace Brimstone
{
	public partial class Game : Entity, IZoneOwner
	{
		public int Turn {
			get { return this[GameTag.TURN]; }
			set { this[GameTag.TURN] = value; }
		}

		public GameState State {
			get { return (GameState)this[GameTag.STATE]; }
			set { this[GameTag.STATE] = (int)value; }
		}

		// IMPORTANT: We cannot cache entity references for these properties
		// because it breaks copy-on-write cloning

		public Player FirstPlayer {
			get {
				return (Player1[GameTag.FIRST_PLAYER] == 1 ? Player1 : Player2[GameTag.FIRST_PLAYER] == 1? Player2 : null);
			}
			set {
				// The opponent's FIRST_PLAYER tag won't be set so it will be zero by default
				value[GameTag.FIRST_PLAYER] = 1;
			}
		}

		public Player CurrentPlayer {
			get {
				return (Player1[GameTag.CURRENT_PLAYER] == 1 ? Player1 : Player2[GameTag.CURRENT_PLAYER] == 1? Player2 : null);
			}
			set {
				value.Opponent[GameTag.CURRENT_PLAYER] = 0;
				value[GameTag.CURRENT_PLAYER] = 1;
			}
		}

		public Step Step {
			get { return (Step)this[GameTag.STEP]; }
			set { this[GameTag.STEP] = (int)value; }
		}

		public Step NextStep {
			get { return (Step)this[GameTag.NEXT_STEP]; }
			set { this[GameTag.NEXT_STEP] = (int)value; }
		}
	}

	public partial class Player : Entity, IZoneOwner
	{
		public Hero Hero {
			get {
				var heroEntityId = this[GameTag.HERO_ENTITY];
				return (heroEntityId > 0 ? Game.Entities[heroEntityId] as Hero : null);
			}
			set {
				this[GameTag.HERO_ENTITY] = value.Id;
			}
		}

		public Player Opponent {
			get {
				return Game.Player1 == this ? Game.Player2 : Game.Player1;
			}
		}

		public PlayState PlayState {
			get { return (PlayState)this[GameTag.PLAYSTATE]; }
			set { this[GameTag.PLAYSTATE] = (int)value; }
		}

		public MulliganState MulliganState {
			get { return (MulliganState)this[GameTag.MULLIGAN_STATE]; }
			set { this[GameTag.MULLIGAN_STATE] = (int)value; }
		}

		public int NumCardsDrawnThisTurn {
			get { return this[GameTag.NUM_CARDS_DRAWN_THIS_TURN]; }
			set { this[GameTag.NUM_CARDS_DRAWN_THIS_TURN] = value; }
		}

		public int NumTurnsLeft {
			get { return this[GameTag.NUM_TURNS_LEFT]; }
			set { this[GameTag.NUM_TURNS_LEFT] = value; }
		}
	}

	public abstract partial class Character : Entity
	{
		public int StartingHealth {
			get {
				return Card[GameTag.HEALTH];
			}
		}

		public int Health {
			get {
				return this[GameTag.HEALTH] - this[GameTag.DAMAGE];
			}
			set {
				// Absolute change in health, removes damage dealt (eg. Equality)
				this[GameTag.HEALTH] = value;
				this[GameTag.DAMAGE] = 0;
			}
		}

		public int Damage {
			get { return this[GameTag.DAMAGE]; }
			set { this[GameTag.DAMAGE] = value; }
		}
	}
}
