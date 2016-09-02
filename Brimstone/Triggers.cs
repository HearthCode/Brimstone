#define _TRIGGER_DEBUG

using System;
using System.Collections.Generic;

namespace Brimstone
{
	// TODO: We need a list of which TriggerTypes are top-level only (ie. MulliganWaiting)
	// TODO: We need to supply the trigger indexes to Game.ActionBlock
	public enum TriggerType
	{
		Play,
		AfterPlay,
		Spellbender,
		PreSummon,
		Summon,
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
		GainArmour,
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

	public interface ITrigger {
		ICondition Condition { get; }
		List<QueueAction> Action { get; }
		TriggerType Type { get; }
		IAttachedTrigger CreateAttachedTrigger(IEntity entity);
	}

	public class Trigger<T, U> : ITrigger where T : IEntity where U : IEntity
	{
		ICondition ITrigger.Condition => Condition;
		public Condition<T, U> Condition { get; }
		public List<QueueAction> Action { get; }= new List<QueueAction>();
		public TriggerType Type { get; }

		public Trigger(TriggerType type, ActionGraph action, Condition<T, U> condition = null) {
#if _TRIGGER_DEBUG
			DebugLog.WriteLine("Creating trigger " + type + " using " + action.Graph[0].GetType().Name);
#endif
			Condition = condition;
			Action = action.Unravel();
			Type = type;
		}

		public Trigger(Trigger<T, U> t) {
			// Only a shallow copy is necessary because Args and Action don't change and are lazily evaluated
			Condition = t.Condition;
			Action = t.Action;
			Type = t.Type;
		}

		IAttachedTrigger ITrigger.CreateAttachedTrigger(IEntity entity) {
			return CreateAttachedTrigger(entity);
		}

		public AttachedTrigger<T, U> CreateAttachedTrigger(IEntity entity) {
			// The base Triggers property on Card.Behaviour has no linked entity
			// Link by creating a copy of the trigger and setting EntityId
			return new AttachedTrigger<T, U>(this, entity);
		}

		// TODO: Detach triggers

		public static Trigger<T, U> At(TriggerType type, ActionGraph g, Condition<T, U> condition = null) {
			return new Trigger<T, U>(type, g, condition);
		}
	}

	public interface IAttachedTrigger : ITrigger {
		int EntityId { get; }
	}

	public class AttachedTrigger<T, U> : Trigger<T, U>, IAttachedTrigger where T : IEntity where U : IEntity
	{
		public int EntityId { get; }

		public AttachedTrigger(Trigger<T, U> t, IEntity e) : base(t) {
#if _TRIGGER_DEBUG
			DebugLog.WriteLine("Attaching trigger " + t.Type + " to entity " + e.ShortDescription);
#endif
			EntityId = e.Id;
		}

		public AttachedTrigger(AttachedTrigger<T, U> t) : base(t) {
			EntityId = t.EntityId;
		}
	}

	public class TriggerManager : ICloneable
	{
		private Game _game;
		public Game Game
		{
			get { return _game; }
			set
			{
				if (_game != null && _game != value)
					_game.OnEntityChanged -= OnEntityChanged;
				_game = value;
				_game.OnEntityChanged += OnEntityChanged;
			}
		}

		public Dictionary<TriggerType, List<IAttachedTrigger>> Triggers { get; }

		public TriggerManager(Game game) {
			Game = game;
			Triggers = new Dictionary<TriggerType, List<IAttachedTrigger>>();
		}

		public TriggerManager(TriggerManager tm) {
			Game = tm.Game;
			Triggers = new Dictionary<TriggerType, List<IAttachedTrigger>>(tm.Triggers);
		}

		public void Add(IEntity entity) {
			if (entity.Card.Behaviour != null)
				if (entity.Card.Behaviour.Triggers != null)
					foreach (var t in entity.Card.Behaviour.Triggers)
						Add(t.CreateAttachedTrigger(entity));
		}

		public void Add(IAttachedTrigger t) {
			if (Triggers.ContainsKey(t.Type))
				Triggers[t.Type].Add(t);
			else
				Triggers.Add(t.Type, new List<IAttachedTrigger> { t });
		}

		private void OnEntityChanged(Game game, IEntity entity, GameTag tag, int oldValue, int newValue) {
			// Tag change triggers
			switch (tag) {
				case GameTag.STATE:
					if (newValue == (int)GameState.RUNNING)
						Queue(TriggerType.GameStart, entity);
					break;

				case GameTag.STEP:
					switch ((Step)newValue) {
						case Step.BEGIN_MULLIGAN:
							Queue(TriggerType.BeginMulligan, entity);
							break;
						case Step.MAIN_NEXT:
							Queue(TriggerType.PhaseMainNext, entity);
							break;
						case Step.MAIN_READY:
							Queue(TriggerType.PhaseMainReady, entity);
							break;
						case Step.MAIN_START_TRIGGERS:
							Queue(TriggerType.PhaseMainStartTriggers, entity);
							break;
						case Step.MAIN_START:
							Queue(TriggerType.PhaseMainStart, entity);
							break;
						case Step.MAIN_ACTION:
							Queue(TriggerType.PhaseMainAction, entity);
							break;
						case Step.MAIN_END:
							Queue(TriggerType.PhaseMainEnd, entity);
							break;
						case Step.MAIN_CLEANUP:
							Queue(TriggerType.PhaseMainCleanup, entity);
							break;
					}
					break;

				case GameTag.MULLIGAN_STATE:
					switch ((MulliganState)newValue) {
						case MulliganState.DEALING:
							Queue(TriggerType.DealMulligan, entity);
							break;
						case MulliganState.WAITING:
							Queue(TriggerType.MulliganWaiting, entity);
							break;
					}
					break;

				case GameTag.JUST_PLAYED:
					if (newValue == 1)
						Queue(TriggerType.Play, entity);
					break;

				case GameTag.DAMAGE:
					if (newValue != 0) { // TODO: Replace with checking if the value increased
						Queue(TriggerType.Damage, entity);
					}
					break;
			}
		}

		public void Queue(TriggerType type, IEntity source) {
			if (!Triggers.ContainsKey(type))
				return;
#if _TRIGGER_DEBUG
			DebugLog.WriteLine("Checking triggers for " + type + " initiated by " + source.ShortDescription);
#endif
			foreach (var trigger in Triggers[type]) {
				var owningEntity = Game.Entities[trigger.EntityId];

				// Only allow triggers to trigger in PLAY for now. TODO: Add support for triggers in HAND etc, or make it so triggers attach/detach only while they're in the correct zone
				if (owningEntity.Zone.Type == Zone.PLAY)
					// Test trigger condition
					if (trigger.Condition?.Eval(owningEntity, source) ?? true)
						Game.ActionBlock(BlockType.TRIGGER, owningEntity, trigger.Action);
			}
		}

		public void At<T, U>(TriggerType Type, ActionGraph Actions, IEntity Owner = null, Condition<T, U> Condition = null)
			where T : IEntity where U : IEntity {
			Add(new Trigger<T, U>(Type, Actions, Condition).CreateAttachedTrigger(Owner ?? Game));
		}

		public object Clone() {
			return new TriggerManager(this);
		}
	}
}
