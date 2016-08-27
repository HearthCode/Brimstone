namespace Brimstone
{
	public partial class Entity
	{
		// Setting Zone directly moves the entity to the end of the specified zone
		public ZoneEntities Zone {
			get { return Controller?.Zones[(Zone)this[GameTag.ZONE]]; }
			set { ZoneMove(value); }
		}

		// Setting ZonePosition directly performs a same-zone move
		public int ZonePosition {
			get { return this[GameTag.ZONE_POSITION]; }
			set { ZoneMove(value); }
		}

		// Used only by heroes, hero powers and minions
		public int NumTurnsInPlay {
			get { return this[GameTag.NUM_TURNS_IN_PLAY]; }
			set { this[GameTag.NUM_TURNS_IN_PLAY] = value; }
		}
	}

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

		public Player CurrentOpponent
		{
			get
			{
				return (Player1[GameTag.CURRENT_PLAYER] == 1 ? Player2 : Player2[GameTag.CURRENT_PLAYER] == 1 ? Player1 : null);
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

		public ICharacter ProposedAttacker {
			get {
				int id = this[GameTag.PROPOSED_ATTACKER];
				return (id != 0 ? (ICharacter)Game.Entities[id] : null);
			}
			set {
				this[GameTag.PROPOSED_ATTACKER] = value?.Id ?? 0;;
			}
		}

		public ICharacter ProposedDefender {
			get {
				int id = this[GameTag.PROPOSED_DEFENDER];
				return (id != 0 ? (ICharacter)Game.Entities[id] : null);
			}
			set {
				this[GameTag.PROPOSED_DEFENDER] = value?.Id ?? 0;
			}
		}

		public int NumMinionsKilledThisTurn {
			get { return this[GameTag.NUM_MINIONS_PLAYER_KILLED_THIS_TURN]; }
			set { this[GameTag.NUM_MINIONS_PLAYER_KILLED_THIS_TURN] = value; }
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

		public int BaseMana {
			get { return this[GameTag.RESOURCES]; }
			set { this[GameTag.RESOURCES] = value; }
		}

		public int UsedMana {
			get { return this[GameTag.RESOURCES_USED]; }
			set { this[GameTag.RESOURCES_USED] = value; }
		}

		public int TemporaryMana {
			get { return this[GameTag.TEMP_RESOURCES]; }
			set { this[GameTag.TEMP_RESOURCES] = value; }
		}

		public bool IsComboActive {
			get { return this[GameTag.COMBO_ACTIVE] == 1; }
			set { this[GameTag.COMBO_ACTIVE] = value ? 1 : 0; }
		}

		public MulliganState MulliganState {
			get { return (MulliganState)this[GameTag.MULLIGAN_STATE]; }
			set { this[GameTag.MULLIGAN_STATE] = (int)value; }
		}

		public int NumCardsDrawnThisTurn {
			get { return this[GameTag.NUM_CARDS_DRAWN_THIS_TURN]; }
			set { this[GameTag.NUM_CARDS_DRAWN_THIS_TURN] = value; }
		}

		public int NumAttacksThisTurn {
			get { return this[GameTag.NUM_ATTACKS_THIS_TURN]; }
			set { this[GameTag.NUM_ATTACKS_THIS_TURN] = value; }
		}

		public int NumCardsPlayedThisTurn {
			get { return this[GameTag.NUM_CARDS_PLAYED_THIS_TURN]; }
			set { this[GameTag.NUM_CARDS_PLAYED_THIS_TURN] = value; }
		}

		public int NumMinionsPlayedThisTurn {
			get { return this[GameTag.NUM_MINIONS_PLAYED_THIS_TURN]; }
			set { this[GameTag.NUM_MINIONS_PLAYED_THIS_TURN] = value; }
		}

		public int NumOptionsPlayedThisTurn {
			get { return this[GameTag.NUM_OPTIONS_PLAYED_THIS_TURN]; }
			set { this[GameTag.NUM_OPTIONS_PLAYED_THIS_TURN] = value; }
		}

		public int NumFriendlyMinionsThatAttackedThisTurn {
			get { return this[GameTag.NUM_FRIENDLY_MINIONS_THAT_ATTACKED_THIS_TURN]; }
			set { this[GameTag.NUM_FRIENDLY_MINIONS_THAT_ATTACKED_THIS_TURN] = value; }
		}

		public int NumFriendlyMinionsThatDiedThisTurn {
			get { return this[GameTag.NUM_FRIENDLY_MINIONS_THAT_DIED_THIS_TURN]; }
			set { this[GameTag.NUM_FRIENDLY_MINIONS_THAT_DIED_THIS_TURN] = value; }
		}

		public int NumFriendlyMinionsThatDiedThisGame {
			get { return this[GameTag.NUM_FRIENDLY_MINIONS_THAT_DIED_THIS_GAME]; }
			set { this[GameTag.NUM_FRIENDLY_MINIONS_THAT_DIED_THIS_GAME] = value; }
		}

		public int NumMinionsPlayerKilledThisTurn {
			get { return this[GameTag.NUM_MINIONS_PLAYER_KILLED_THIS_TURN]; }
			set { this[GameTag.NUM_MINIONS_PLAYER_KILLED_THIS_TURN] = value; }
		}

		public int TotalManaSpentThisGame {
			get { return this[GameTag.NUM_RESOURCES_SPENT_THIS_GAME]; }
			set { this[GameTag.NUM_RESOURCES_SPENT_THIS_GAME] = value; }
		}

		public int HeroPowerActivationsThisTurn {
			get { return this[GameTag.HEROPOWER_ACTIVATIONS_THIS_TURN]; }
			set { this[GameTag.HEROPOWER_ACTIVATIONS_THIS_TURN] = value; }
		}

		public int NumTurnsLeft {
			get { return this[GameTag.NUM_TURNS_LEFT]; }
			set { this[GameTag.NUM_TURNS_LEFT] = value; }
		}

		public int OverloadNextTurn {
			get { return this[GameTag.OVERLOAD_OWED]; }
			set { this[GameTag.OVERLOAD_OWED] = value; }
		}

		public int Overload {
			get { return this[GameTag.OVERLOAD_LOCKED]; }
			set { this[GameTag.OVERLOAD_LOCKED] = value; }
		}

		public int OverloadThisGame {
			get { return this[GameTag.OVERLOAD_THIS_GAME]; }
			set { this[GameTag.OVERLOAD_THIS_GAME] = value; }
		}
	}

	public partial interface ICharacter : ICanTarget
	{
		int Attack { get; set; }
		bool CantBeTargetedByOpponents { get; set; }
		int Damage { get; set; }
		int Health { get; set; }
		bool IsAttacking { get; set; }
		bool IsDefending { get; set; }
		bool IsExhausted { get; set; }
		bool IsFrozen { get; set; }
		int NumAttacksThisTurn { get; set; }
		int PreDamage { get; set; }
		Race Race { get; set; }
		bool ShouldExitCombat { get; set; }
		int StartingHealth { get; }
	}

	public abstract partial class Character<T> : Playable<T>, ICharacter where T : Entity
	{
		public int Attack {
			get { return this[GameTag.ATK]; }
			set { this[GameTag.ATK] = value; }
		}

		public bool CantBeTargetedByOpponents {
			get { return this[GameTag.CANT_BE_TARGETED_BY_OPPONENTS] == 1; }
			set { this[GameTag.CANT_BE_TARGETED_BY_OPPONENTS] = value ? 1 : 0; }
		}

		public int Damage {
			get { return this[GameTag.DAMAGE]; }
			set { this[GameTag.DAMAGE] = value; }
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

		public bool IsAttacking {
			get { return this[GameTag.ATTACKING] == 1; }
			set { this[GameTag.ATTACKING] = value ? 1 : 0; }
		}

		public bool IsDefending {
			get { return this[GameTag.DEFENDING] == 1; }
			set { this[GameTag.DEFENDING] = value ? 1 : 0; }
		}

		public bool IsExhausted {
			get { return this[GameTag.EXHAUSTED] == 1; }
			set { this[GameTag.EXHAUSTED] = value ? 1 : 0; }
		}

		public bool IsFrozen {
			get { return this[GameTag.FROZEN] == 1; }
			set { this[GameTag.FROZEN] = value ? 1 : 0; }
		}

		public int NumAttacksThisTurn {
			get { return this[GameTag.NUM_ATTACKS_THIS_TURN]; }
			set { this[GameTag.NUM_ATTACKS_THIS_TURN] = value; }
		}

		public int PreDamage {
			get { return this[GameTag.PREDAMAGE]; }
			set { this[GameTag.PREDAMAGE] = value; }
		}

		public Race Race {
			get { return (Race)this[GameTag.CARDRACE]; }
			set { this[GameTag.CARDRACE] = (int)value; }
		}

		public bool ShouldExitCombat {
			get { return this[GameTag.SHOULDEXITCOMBAT] == 1; }
			set { this[GameTag.SHOULDEXITCOMBAT] = value ? 1 : 0; }
		}

		public int StartingHealth {
			get {
				return Card[GameTag.HEALTH];
			}
		}
	}

	public partial class Minion : Character<Minion>
	{
		public bool CantBeTargetedByAbilities {
			get { return this[GameTag.CANT_BE_TARGETED_BY_ABILITIES] == 1; }
			set { this[GameTag.CANT_BE_TARGETED_BY_ABILITIES ] = value ? 1 : 0; }
		}

		public bool HasDeathrattle {
			get { return this[GameTag.DEATHRATTLE] == 1; }
		}

		public bool HasStealth {
			get { return this[GameTag.STEALTH] == 1; }
			set { this[GameTag.STEALTH] = value ? 1 : 0; }
		}

		public bool HasTaunt {
			get { return this[GameTag.TAUNT] == 1; }
			set { this[GameTag.TAUNT] = value ? 1 : 0; }
		}
	}
}
