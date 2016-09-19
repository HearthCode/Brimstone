using System.Collections.Generic;
using System.Linq;
using static Brimstone.Behaviours;

namespace Brimstone.Entities
{
	public partial interface ICharacter : IPlayable
	{
		bool CanAttack { get; }
		
		bool MortallyWounded { get; }

		ICanTarget Attack(ICharacter Target = null);

		void Hit(int amount);
	}

	public abstract partial class Character<T> : Playable<T>, ICharacter where T : Entity
	{
		protected Character(Card card, Dictionary<GameTag, int> tags = null) : base(card, tags) { }
		protected Character(Character<T> cloneFrom) : base(cloneFrom) { }

		public bool CanAttack => !IsExhausted && AttackDamage > 0 && ValidTargets.Any();

		public bool MortallyWounded {
			get { return Health <= 0 || ToBeDestroyed; }
		}

		// Default targeting for hero and minion attack targets
		public override IEnumerable<ICharacter> ValidTargets {
			get {
				// Stealthed minions are ignored in both taunt and non-taunt targeting scenarios
				var opponentNonStealthed =
					Controller.Opponent.Board.Where(x => !x.HasStealth).Concat(new List<ICharacter> {Controller.Opponent.Hero});

				// Must attack non-stealthed taunts
				var opponentTaunts = opponentNonStealthed.Where(x => x.HasTaunt);
				if (opponentTaunts.Any())
					return opponentTaunts;

				// Can attack all opponent characters
				return opponentNonStealthed;
			}
		}

		public ICanTarget Attack(ICharacter target = null) {
			if (Game.Player1.Choice != null || Game.Player2.Choice != null)
				throw new ChoiceException();

			Target = target;
			return (ICanTarget) (Entity) Game.RunActionBlock(BlockType.ATTACK, this, Behaviours.Attack(this, (Entity) target), target);
		}

		public void Hit(int amount) {
			Game.ActiveTriggers.ForceRun(this, Damage(this, amount), this);
		}
	}
}
