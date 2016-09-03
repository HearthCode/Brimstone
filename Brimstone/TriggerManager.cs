#define _TRIGGER_DEBUG
using System;
using System.Collections.Generic;
using System.Linq;

namespace Brimstone
{
	public class TriggerManager : ICloneable
	{
		private static readonly Dictionary<TriggerType, int> TriggerIndices = new Dictionary<TriggerType, int>
		{
			{ TriggerType.PhaseMainStart, 0 },
			{ TriggerType.PhaseMainReady, 1 },
			{ TriggerType.PhaseMainAction, 2 },
			{ TriggerType.WeaponAttack, 3 },
			{ TriggerType.PhaseMainEnd, 4 },
			{ TriggerType.PhaseMainCleanup, 5 },
			{ TriggerType.DealMulligan, 6 },
			{ TriggerType.MulliganWaiting, 7 },
			{ TriggerType.PhaseMainStartTriggers, 8 }
 		};

		private Game _game;
		public Game Game {
			get { return _game; }
			set {
				if (_game != null && _game != value) {
					_game.OnEntityChanged -= OnEntityChanged;
					_game.OnEntityCreated -= OnEntityCreated;
				}
				_game = value;
				_game.OnEntityChanged += OnEntityChanged;
				_game.OnEntityCreated += OnEntityCreated;
			}
		}

		// Lists of entity IDs which have triggers of the key type
		public Dictionary<TriggerType, List<int>> Triggers { get; }

		public TriggerManager(Game game) {
			Game = game;
			Triggers = new Dictionary<TriggerType, List<int>>();
		}

		public TriggerManager(TriggerManager tm) {
			Game = tm.Game;
			// TODO: Write a unit test that clones twice, creates new entity with same ID and different triggers per game, check all 3 games trigger correctly
			Triggers = new Dictionary<TriggerType, List<int>>(tm.Triggers);
		}

		public void Add(IEntity entity) {
			if (entity.Card.Behaviour?.Triggers != null)
				foreach (var t in entity.Card.Behaviour.Triggers)
					Add(entity, t);
		}

		private void Add(IEntity entity, ITrigger trigger) {
#if _TRIGGER_DEBUG
			DebugLog.WriteLine("Associating trigger " + trigger.Type + " for entity " + entity.ShortDescription + " with game " + Game.GameId);
#endif
			if (Triggers.ContainsKey(trigger.Type))
				Triggers[trigger.Type].Add(entity.Id);
			else
				Triggers.Add(trigger.Type, new List<int> { entity.Id });
		}

		private void OnEntityCreated(Game game, IEntity entity) {
			Add(entity);
		}

		private void OnEntityChanged(Game game, IEntity entity, GameTag tag, int oldValue, int newValue) {
			// Tag change triggers
			switch (tag) {
				case GameTag.STATE:
					if (newValue == (int)GameState.RUNNING)
						Queue(TriggerType.GameStart, game);
					break;

				case GameTag.STEP:
					switch ((Step)newValue) {
						case Step.BEGIN_MULLIGAN:
							Queue(TriggerType.BeginMulligan, game);
							break;
						case Step.MAIN_NEXT:
							Queue(TriggerType.PhaseMainNext, game.CurrentPlayer);
							break;
						case Step.MAIN_READY:
							Queue(TriggerType.PhaseMainReady, game.CurrentPlayer);
							break;
						case Step.MAIN_START_TRIGGERS:
							Queue(TriggerType.PhaseMainStartTriggers, game.CurrentPlayer);
							break;
						case Step.MAIN_START:
							Queue(TriggerType.PhaseMainStart, game.CurrentPlayer);
							break;
						case Step.MAIN_ACTION:
							Queue(TriggerType.PhaseMainAction, game.CurrentPlayer);
							break;
						case Step.MAIN_END:
							Queue(TriggerType.PhaseMainEnd, game.CurrentPlayer);
							break;
						case Step.MAIN_CLEANUP:
							Queue(TriggerType.PhaseMainCleanup, game.CurrentPlayer);
							break;
					}
					break;

				case GameTag.MULLIGAN_STATE:
					switch ((MulliganState)newValue) {
						case MulliganState.DEALING:
							Queue(TriggerType.DealMulligan, entity);
							break;
							// NOTE: We can't trigger on MulliganState.WAITING here
							// because the trigger must run on the top level of the queue
					}
					break;

				case GameTag.JUST_PLAYED:
					if (newValue == 1)
						Queue(TriggerType.Play, entity);
					break;

				case GameTag.DAMAGE:
					if (newValue > oldValue)
						Queue(TriggerType.Damage, entity);
					break;
			}
		}

		public void Queue(TriggerType type, IEntity source) {
			if (!Triggers.ContainsKey(type))
				return;
#if _TRIGGER_DEBUG
			DebugLog.WriteLine("Checking triggers for " + type + " initiated by " + source.ShortDescription);
#endif
			foreach (var entityId in Triggers[type]) {
				var owningEntity = Game.Entities[entityId];
				if (owningEntity.Zone.Type == Zone.PLAY || owningEntity.Zone.Type == Zone.HAND) {
#if _TRIGGER_DEBUG
					DebugLog.WriteLine("Checking trigger conditions for " + owningEntity.ShortDescription);
#endif
					// TODO: Replace per-card trigger list with properties
					var trigger = owningEntity.Card.Behaviour.Triggers.First(x => x.Type == type);
					// Test trigger condition
					if (trigger.Condition?.Eval(owningEntity, source) ?? true) {
#if _TRIGGER_DEBUG
						DebugLog.WriteLine("Firing trigger for " + owningEntity.ShortDescription + " with actions: "
							+ string.Join(" ", trigger.Action.Select(a => a.ToString())));
#endif
						Game.ActionBlock(BlockType.TRIGGER, owningEntity, trigger.Action,
							// Trigger index: 0 for normal entities; -1 for Game; specific index for player if specified, otherwise -1
							Index: TriggerIndices.ContainsKey(type) ? TriggerIndices[type] :
								source == Game ? -1 : source is Player ? -1 : 0);
					}
				}
				else {
#if _TRIGGER_DEBUG
					DebugLog.WriteLine("Ignoring triggers for " + owningEntity.ShortDescription + " because it wasn't in an active zone");
#endif
				}
			}
		}

		public object Clone() {
			return new TriggerManager(this);
		}
	}
}
