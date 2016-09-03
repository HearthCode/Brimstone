namespace Brimstone
{
	// All of the game triggers you can use
	public partial class Behaviours
	{
		// Pre-defined trigger types
		public static Trigger OnBeginTurn(Condition Condition, ActionGraph Action) {
			return Trigger.At(TriggerType.PhaseMainStartTriggers, Action, Condition);
		}
		public static Trigger OnEndTurn(Condition Condition, ActionGraph Action) {
			return Trigger.At(TriggerType.PhaseMainEnd, Action, Condition);
		}
		public static Trigger OnPlay(Condition Condition, ActionGraph Action) {
			return Trigger.At(TriggerType.Play, Action, Condition);
		}
		public static Trigger AfterPlay(Condition Condition, ActionGraph Action) {
			return Trigger.At(TriggerType.AfterPlay, Action, Condition);
		}
		public static Trigger OnSpellbender(Condition Condition, ActionGraph Action) {
			return Trigger.At(TriggerType.Spellbender, Action, Condition);
		}
		public static Trigger OnPreSummon(Condition Condition, ActionGraph Action) {
			return Trigger.At(TriggerType.PreSummon, Action, Condition);
		}
		public static Trigger OnSummon(Condition Condition, ActionGraph Action) {
			return Trigger.At(TriggerType.Summon, Action, Condition);
		}
		public static Trigger AfterSummon(Condition Condition, ActionGraph Action) {
			return Trigger.At(TriggerType.AfterSummon, Action, Condition);
		}
		public static Trigger OnProposedAttack(Condition Condition, ActionGraph Action) {
			return Trigger.At(TriggerType.ProposedAttack, Action, Condition);
		}
		public static Trigger OnAttack(Condition Condition, ActionGraph Action) {
			return Trigger.At(TriggerType.Attack, Action, Condition);
		}
		public static Trigger AfterAttack(Condition Condition, ActionGraph Action) {
			return Trigger.At(TriggerType.AfterAttack, Action, Condition);
		}
		public static Trigger OnInspire(Condition Condition, ActionGraph Action) {
			return Trigger.At(TriggerType.Inspire, Action, Condition);
		}
		public static Trigger OnDeath(Condition Condition, ActionGraph Action) {
			return Trigger.At(TriggerType.Death, Action, Condition);
		}
		public static Trigger OnDrawCard(Condition Condition, ActionGraph Action) {
			return Trigger.At(TriggerType.DrawCard, Action, Condition);
		}
		public static Trigger OnAddToHand(Condition Condition, ActionGraph Action) {
			return Trigger.At(TriggerType.AddToHand, Action, Condition);
		}
		public static Trigger OnPreDamage(Condition Condition, ActionGraph Action) {
			return Trigger.At(TriggerType.PreDamage, Action, Condition);
		}
		public static Trigger OnDamage(Condition Condition, ActionGraph Action) {
			return Trigger.At(TriggerType.Damage, Action, Condition);
		}
		public static Trigger OnHeal(Condition Condition, ActionGraph Action) {
			return Trigger.At(TriggerType.Heal, Action, Condition);
		}
		public static Trigger OnSilence(Condition Condition, ActionGraph Action) {
			return Trigger.At(TriggerType.Silence, Action, Condition);
		}
		public static Trigger OnDiscard(Condition Condition, ActionGraph Action) {
			return Trigger.At(TriggerType.Discard, Action, Condition);
		}
		public static Trigger OnGainArmour(Condition Condition, ActionGraph Action) {
			return Trigger.At(TriggerType.GainArmour, Action, Condition);
		}
		public static Trigger OnRevealSecret(Condition Condition, ActionGraph Action) {
			return Trigger.At(TriggerType.RevealSecret, Action, Condition);
		}
		public static Trigger OnEquipWeapon(Condition Condition, ActionGraph Action) {
			return Trigger.At(TriggerType.EquipWeapon, Action, Condition);
		}
		public static Trigger OnWeaponAttack(Condition Condition, ActionGraph Action) {
			return Trigger.At(TriggerType.WeaponAttack, Action, Condition);
		}
		public static Trigger OnMainNext(Condition Condition, ActionGraph Action) {
			return Trigger.At(TriggerType.PhaseMainNext, Action, Condition);
		}

		// Generic triggers (use to create triggers for events not specified in the section above)
		public static Trigger At(TriggerType TriggerType, Condition Condition, ActionGraph Action) {
			return Trigger.At(TriggerType, Action, Condition);
		}

		public static Trigger At(TriggerType TriggerType, ActionGraph Action) {
			return Trigger.At(TriggerType, Action);
		}
	}
}
