using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Brimstone
{
	public enum TriggerType
	{
		BeginTurn,
		EndTurn,
		PlaySpell,
		AfterPlaySpell,
		Spellbender,
		PlayWeapon,
		PreSummon,
		Summon,
		PlayMinion,
		AfterPlayCard,
		AfterSummon,
		ProposedAttack,
		Attack,
		AfterAttack,
		Inspire,
		Death,
		DrawCard,
		AddToHand,
		PreDamage,
		Damage, // 0
		Heal,
		Silence,
		Discard,
		GainArmor,
		RevealSecret,
		EquipWeapon,
		WeaponAttack, // 3

		GameStart, // -1
		BeginMulligan, // -1
		DealMulligan, // 6
		MulliganWaiting, // 7
		PhaseMainReady, // 1
		PhaseMainStartTriggers, // 8
		PhaseMainStart, // 0
		PhaseMainAction, // 2
		PhaseMainEnd, // 4
		PhaseMainCleanup, // 5
		PhaseMainNext, // -1
	}

	public class Trigger
	{
		public Selector Condition { get; }
		public List<QueueAction> Action { get; }= new List<QueueAction>();
		public TriggerType Type { get; }

		public Trigger(TriggerType type, ActionGraph action, Selector condition = null) {
			DebugLog.WriteLine("Creating trigger " + type + " using " + action.Graph[0].GetType().Name);
			Condition = condition;
			Action = action.Unravel();
			Type = type;
		}

		public Trigger(Trigger t) {
			// Only a shallow copy is necessary because Args and Action don't change and are lazily evaluated
			Condition = t.Condition;
			Action = t.Action;
			Type = t.Type;
		}

		public AttachedTrigger CreateAttachedTrigger(IEntity entity) {
			// The base Triggers property on Card.Behaviour has no linked entity
			// Link by creating a copy of the trigger and setting EntityId
			return new AttachedTrigger(this, entity);
		}

		// TODO: Detach triggers

		public static Trigger At(TriggerType type, ActionGraph g, Selector condition = null) {
			return new Trigger(type, g, condition);
		}
	}

	public class AttachedTrigger : Trigger
	{
		public int EntityId { get; }

		public AttachedTrigger(Trigger t, IEntity e) : base(t) {
			DebugLog.WriteLine("Attaching trigger " + t.Type + " to entity " + e.ShortDescription);
			EntityId = e.Id;
		}

		public AttachedTrigger(AttachedTrigger t) : base(t) {
			EntityId = t.EntityId;
		}
	}

	public class TriggerManager : ICloneable
	{
		public Game Game { get; set; }
		public Dictionary<TriggerType, List<AttachedTrigger>> Triggers { get; }

		public TriggerManager(Game game) {
			Game = game;
			Triggers = new Dictionary<TriggerType, List<AttachedTrigger>>();
		}

		public TriggerManager(TriggerManager tm) {
			Game = tm.Game;
			Triggers = new Dictionary<TriggerType, List<AttachedTrigger>>(tm.Triggers);
		}

		public void Add(IEntity entity) {
			if (entity.Card.Behaviour != null)
				if (entity.Card.Behaviour.Triggers != null)
					foreach (var t in entity.Card.Behaviour.Triggers)
						Add(t.CreateAttachedTrigger(entity));
		}

		public void Add(AttachedTrigger t) {
			if (Triggers.ContainsKey(t.Type))
				Triggers[t.Type].Add(t);
			else
				Triggers.Add(t.Type, new List<AttachedTrigger> { t });
		}

		public void Fire(TriggerType type, IEntity source) {
			if (!Triggers.ContainsKey(type))
				return;

			DebugLog.WriteLine("Checking triggers for " + type + " initiated by " + source.ShortDescription);

			foreach (var trigger in Triggers[type]) {
				var owningEntity = Game.Entities[trigger.EntityId];

				// Get condition entities
				IEnumerable<IEntity> conditionEntities = trigger.Condition?.Lambda(owningEntity);

				// No condition? Conditions met
				bool conditionMet = conditionEntities == null;

				// Check for entity match
				if (!conditionMet) {
					switch (trigger.Type) {
						case TriggerType.Damage:
							conditionMet = conditionEntities.Contains(Game.Environment.LastDamaged);
							break;
						case TriggerType.DealMulligan:
						case TriggerType.MulliganWaiting:
							conditionMet = conditionEntities.Contains(source);
							break;
						default:
							throw new TriggerException("Trigger type " + trigger.Type + " not implemented");
					}
				}

				// Run trigger if condition met
				if (conditionMet)
					Game.TriggerBlock(owningEntity, trigger.Action);
			}
		}

		public void At(TriggerType Type, ActionGraph Actions, IEntity Owner = null, Selector Condition = null) {
			Add(new Trigger(Type, Actions, Condition).CreateAttachedTrigger(Owner ?? Game));
		}

		public object Clone() {
			return new TriggerManager(this);
		}
	}
}
