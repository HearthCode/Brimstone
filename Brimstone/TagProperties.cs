using System.Collections.Generic;

namespace Brimstone
{
	public partial class Game : Entity, IZones
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
				return (Player1[GameTag.FIRST_PLAYER] == 1 ? Player1 : Player2);
			}
			set {
				// The opponent's FIRST_PLAYER tag won't be set so it will be zero by default
				value[GameTag.FIRST_PLAYER] = 1;
			}
		}

		public Player CurrentPlayer {
			get {
				return (Player1[GameTag.CURRENT_PLAYER] == 1 ? Player1 : Player2);
			}
			set {
				value.Opponent[GameTag.CURRENT_PLAYER] = 0;
				value[GameTag.CURRENT_PLAYER] = 1;
			}
		}
	}

	public abstract class CanBeDamaged : Entity
	{
		public CanBeDamaged(Game game, IEntity controller, Card card, Dictionary<GameTag, int> tags = null) : base(game, controller, card, tags) { }
		public CanBeDamaged(CanBeDamaged cloneFrom) : base(cloneFrom) { }

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

	public partial class Player : CanBeDamaged, IZones
	{
		public Player Opponent {
			get {
				return Game.Player1 == this ? Game.Player2 : Game.Player1;
			}
		}
	}

	public partial class Minion : CanBeDamaged, IMinion
	{
	}
}