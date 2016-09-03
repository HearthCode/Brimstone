#define _ACTIONS_DEBUG

using System;
using System.Collections.Generic;
using System.Linq;

namespace Brimstone
{
	public class Empty : QueueAction
	{
		public override ActionResult Run(Game game, IEntity source, List<ActionResult> args) {
			return ActionResult.Empty;
		}
	}

	// TODO: Optimize this away during graph unravel
	public class FixedNumber : QueueAction
	{
		public int Num { get; set; }

		public override ActionResult Run(Game game, IEntity source, List<ActionResult> args) {
			return Num;
		}
	}

	public class FixedCard : QueueAction
	{
		public Card Card { get; set; }

		public override ActionResult Run(Game game, IEntity source, List<ActionResult> args) {
			return Card;
		}
	}

	public class LazyEntity : QueueAction
	{
		public int EntityId { get; set; }

		public override ActionResult Run(Game game, IEntity source, List<ActionResult> args) {
			return (Entity)game.Entities[EntityId];
		}
	}

	public enum SelectionSource
	{
		Game,
		Player,
		ActionSource
	}

	public class Selector : QueueAction
	{
		public Func<IEntity, IEnumerable<IEntity>> Lambda { get; set; }

		public override ActionResult Run(Game game, IEntity source, List<ActionResult> args) {
			return Lambda(source).ToList();
		}
	}

	public class Func : QueueAction
	{
		public Action<IEntity> F { get; set; }

		public override ActionResult Run(Game game, IEntity source, List<ActionResult> args) {
			F(source);
			return ActionResult.None;
		}
	}

	public class GameBlock : QueueAction
	{
		public BlockStart Block { get; set; }
		public List<QueueAction> Actions { get; set; }

		public GameBlock(BlockStart Block, List<QueueAction> Actions) {
			this.Block = Block;
			this.Actions = Actions;
		}

		public override ActionResult Run(Game game, IEntity source, List<ActionResult> args) {
			game.PowerHistory?.Add(Block);
			game.ActionQueue.StartBlock(source, Actions, Block);
			return ActionResult.None;
		}
	}

	public class RandomChoice : QueueAction
	{
		public const int ENTITIES = 0;

		public override ActionResult Run(Game game, IEntity source, List<ActionResult> args) {
			var entities = (List<IEntity>)args[ENTITIES];
			if (entities.Count == 0)
				return new List<IEntity>();
			var m = RNG.Between(0, entities.Count - 1);
			return (Entity)entities[m];
		}
	}

	public class RandomAmount : QueueAction
	{
		public const int MIN = 0;
		public const int MAX = 1;

		public override ActionResult Run(Game game, IEntity source, List<ActionResult> args) {
			return RNG.Between(args[MIN], args[MAX]);
		}
	}

	public class Repeat : QueueAction
	{
		public ActionGraph Actions { get; set; }

		public const int AMOUNT = 0;

		public override ActionResult Run(Game game, IEntity source, List<ActionResult> args) {
			for (int i = 0; i < args[AMOUNT]; i++)
				game.Queue(source, Actions);
			return ActionResult.None;
		}
	}

	// Runs when STATE = RUNNING
	public class StartGame : QueueAction
	{
		public override ActionResult Run(Game game, IEntity source, List<ActionResult> args) {
			// Pick a random starting player
			if (game.FirstPlayerNum == 0)
				game.FirstPlayer = game.Players[RNG.Between(0, 1)];
			else
				game.FirstPlayer = game.Players[game.FirstPlayerNum - 1];
			game.CurrentPlayer = game.FirstPlayer;

			// Set turn counter
			game.Turn = 1;

			// Draw cards
			foreach (var p in game.Players)
			{
				p.Draw((game.FirstPlayer == p ? 3 : 4));
				p.NumTurnsLeft = 1;

				// Give 2nd player the coin
				if (p != game.FirstPlayer)
					p.Give("The Coin");
			}

			// TODO: Set TIMEOUT for each player here if desired

			if (!game.SkipMulligan)
				game.NextStep = Step.BEGIN_MULLIGAN;
			else
				game.NextStep = Step.MAIN_READY;

			return ActionResult.None;
		}
	}

	// Run for each player when MULLIGAN_STATE = DEALING
	public class PerformMulligan : QueueAction
	{
		public override ActionResult Run(Game game, IEntity source, List<ActionResult> args)
		{
			var player = source as Player;

			// Perform mulligan
			foreach (var e in player.Choice.Discarding)
				e.ZoneSwap(player.Deck[RNG.Between(1, player.Deck.Count)]);
			player.Choice = null;

			player.MulliganState = MulliganState.WAITING;
			return ActionResult.None;
		}
	}

	// Run for each player when MULLIGAN_STATE = WAITING
	public class WaitForMulliganComplete : QueueAction
	{
		public override ActionResult Run(Game game, IEntity source, List<ActionResult> args)
		{
			var player = source as Player;
			player.MulliganState = MulliganState.DONE;

			// Start main game if both players have completed mulligan
			if (player.Opponent.MulliganState == MulliganState.DONE)
				game.NextStep = Step.MAIN_READY;
			return ActionResult.None;
		}
	}

	// Runs when STEP = BEGIN_MULLIGAN
	public class BeginMulligan : QueueAction
	{
		public override ActionResult Run(Game game, IEntity source, List<ActionResult> args)
		{
			foreach (var p in game.Players)
				p.StartMulligan();
			return ActionResult.None;
		}
	}

	// Runs when STEP = MAIN_READY
	public class BeginTurn : QueueAction
	{
		public override ActionResult Run(Game game, IEntity source, List<ActionResult> args) {
			// Update the number of turns everything has been in play
			game.CurrentPlayer.Hero.NumTurnsInPlay++;
			// TODO: game.CurrentPlayer.HeroPower.NumTurnsInPlay++;
			foreach (Minion e in game.CurrentPlayer.Board)
				e.NumTurnsInPlay++;
			game.CurrentOpponent.Hero.NumTurnsInPlay++;
			// TODO: game.CurrentPlayer.HeroPower.NumTurnsInPlay++;
			foreach (Minion e in game.CurrentOpponent.Board)
				e.NumTurnsInPlay++;

			// Give player a mana crystal
			game.CurrentPlayer.BaseMana++;
			game.CurrentPlayer.UsedMana = 0;

			// De-activate combo buff
			game.CurrentPlayer.IsComboActive = false;

			// Reset counters
			game.CurrentOpponent.Hero.NumAttacksThisTurn = 0;
			foreach (Minion e in game.CurrentOpponent.Board)
				e.NumAttacksThisTurn = 0;
			game.CurrentPlayer.NumCardsPlayedThisTurn = 0;
			game.CurrentPlayer.NumMinionsPlayedThisTurn = 0;
			game.CurrentPlayer.NumOptionsPlayedThisTurn = 0;

			foreach (Minion e in game.CurrentPlayer.Board)
				e.IsExhausted = false;

			game.CurrentPlayer.NumCardsDrawnThisTurn = 0;

			// Ain't no rest for the triggered...
			game.NextStep = Step.MAIN_START_TRIGGERS;

			game.CurrentPlayer.NumFriendlyMinionsThatDiedThisTurn = 0;
			game.CurrentOpponent.NumFriendlyMinionsThatDiedThisTurn = 0;
			return ActionResult.None;
		}
	}

	// Runs when STEP = MAIN_START_TRIGGERS
	public class BeginTurnTriggers : QueueAction
	{
		public override ActionResult Run(Game game, IEntity source, List<ActionResult> args)
		{
			game.NextStep = Step.MAIN_START;
			return ActionResult.None;
		}
	}

	// Runs when STEP = MAIN_START
	public class BeginTurnForPlayer : QueueAction
	{
		public override ActionResult Run(Game game, IEntity source, List<ActionResult> args) {
			// Draw a card then reset all relevant flags
			game.Queue(game.CurrentPlayer, Actions.Draw(game.CurrentPlayer).Then((Action<IEntity>)(_ => {
				game.CurrentPlayer.NumMinionsPlayerKilledThisTurn = 0;
				game.CurrentOpponent.NumMinionsPlayerKilledThisTurn = 0;
				game.CurrentPlayer.NumFriendlyMinionsThatAttackedThisTurn = 0;
				game.NumMinionsKilledThisTurn = 0;
				game.CurrentPlayer.HeroPowerActivationsThisTurn = 0;
				game.NextStep = Step.MAIN_ACTION;
			})));
			return ActionResult.None;
		}
	}

	// Runs when STEP = MAIN_END
	public class EndTurnForPlayer : QueueAction
	{
		public override ActionResult Run(Game game, IEntity source, List<ActionResult> args) {
			game.NextStep = Step.MAIN_CLEANUP;
			return ActionResult.None;
		}
	}

	// Run when STEP = MAIN_CLEANUP
	public class EndTurnCleanupForPlayer : QueueAction
	{
		public override ActionResult Run(Game game, IEntity source, List<ActionResult> args)
		{
			foreach (IPlayable e in game.Entities.Where(x => x is IPlayable && ((IPlayable)x).JustPlayed && x.Controller == source))
				e.JustPlayed = false;
			game.NextStep = Step.MAIN_NEXT;
			return ActionResult.None;
		}
	}

	// Runs when STEP = MAIN_NEXT
	public class EndTurn : QueueAction
	{
		public override ActionResult Run(Game game, IEntity source, List<ActionResult> args)
		{
			// This is probably going to be used to give players extra turns later
			game.CurrentPlayer.NumTurnsLeft = 0;
			game.CurrentOpponent.NumTurnsLeft = 1;

			game.CurrentPlayer = game.CurrentOpponent;
			game.Turn++;

			game.NextStep = Step.MAIN_READY;
			return ActionResult.None;
		}
	}

	public class Concede : QueueAction
	{
		public const int PLAYER = 0;

		public override ActionResult Run(Game game, IEntity source, List<ActionResult> args) {
			Player player = (Player) args[PLAYER];
			player.PlayState = PlayState.CONCEDED;
			player.PlayState = PlayState.LOST;
			player.Opponent.PlayState = PlayState.WON;
			game.End();
			return player.Opponent;
		}
	}

	public class Give : QueueAction
	{
		public const int PLAYER = 0;
		public const int CARD = 1;

		public override ActionResult Run(Game game, IEntity source, List<ActionResult> args) {
			Player player = (Player)args[PLAYER];
			Card card = args[CARD];
#if _ACTIONS_DEBUG
			DebugLog.WriteLine("Game {0}: Giving {1} to {2}", game.GameId, card.Name, player.FriendlyName);
#endif
			return (Entity) Entity.FromCard(card, StartingZone: player.Hand) ?? ActionResult.None;
		}
	}

	public class Draw : QueueAction
	{
		public const int PLAYER = 0;

		public override ActionResult Run(Game game, IEntity source, List<ActionResult> args) {
			Player player = (Player)args[PLAYER];

			if (!player.Deck.IsEmpty) {
				var entity = player.Deck[1];
#if _ACTIONS_DEBUG
				DebugLog.WriteLine("Game {0}: {1} draws {2}", game.GameId, player.FriendlyName, entity.ShortDescription);
#endif
				entity.Zone = player.Hand;
				player.NumCardsDrawnThisTurn++;
				return (Entity) entity;
			}
#if _ACTIONS_DEBUG
			DebugLog.WriteLine("Game {0}: {1} tries to draw but their deck is empty", game.GameId, player.FriendlyName);
#endif
			return ActionResult.None;
		}
	}

	public class Play : QueueAction
	{
		// TODO: Deal with targeting

		public const int ENTITY = 0;

		public override ActionResult Run(Game game, IEntity source, List<ActionResult> args) {
			Player player = source.Controller;
			IPlayable entity = (IPlayable) (Entity) args[ENTITY];

			// TODO: Update ResourcesUsed
			// TODO: Update NumResourcesSpentThisGame

			player.NumCardsPlayedThisTurn++;
			if (entity is Minion)
				player.NumMinionsPlayedThisTurn++;

			entity.Zone = player.Board;

			if (entity is Minion && !((Minion) entity).HasCharge)
				((Minion) entity).IsExhausted = true;

			entity.JustPlayed = true;
			player.LastCardPlayed = entity;
#if _ACTIONS_DEBUG
			DebugLog.WriteLine("Game {0}: {1} is playing {2}", game.GameId, player.FriendlyName, entity.ShortDescription);
#endif
			// TODO: Deaths

			game.Queue(source, entity.Card.Behaviour.Battlecry);
			game.Queue(source, new Action<IEntity>(_ =>
			{
				player.IsComboActive = true;
				player.NumOptionsPlayedThisTurn++;

				// Spells go to the graveyard after they are played
				if (entity is Spell)
					entity.Zone = entity.Controller.Graveyard;
			}));

			game.ActiveTriggers.Queue(TriggerType.AfterPlay, entity); // TODO: Attach this to a tag change

			// TODO: Deaths, other triggers, etc

			return (Entity) entity;
		}
	}

	public class Damage : QueueAction
	{
		public const int TARGETS = 0;
		public const int DAMAGE = 1;

		public override ActionResult Run(Game game, IEntity source, List<ActionResult> args) {
			if (args[TARGETS].HasResult)
				foreach (ICharacter e in args[TARGETS]) {
#if _ACTIONS_DEBUG
					DebugLog.WriteLine("Game {0}: {1} is getting hit for {2} points of damage", game.GameId, e.ShortDescription, args[DAMAGE]);
#endif

					game.Environment.LastDamaged = e;
					e.Damage += args[DAMAGE];

					// TODO: Handle Spell Damage +1, Prophet Velen, Fallen Hero, Predamage, on-damage triggers and more, full specification here https://hearthstone.gamepedia.com/Advanced_rulebook#Damage_and_Healing
				}
			return ActionResult.None;
		}
	}

	public class Heal : QueueAction
	{
		public const int TARGETS = 0;
		public const int AMOUNT = 1;

		public override ActionResult Run(Game game, IEntity source, List<ActionResult> args) {
			if (args[TARGETS].HasResult)
				foreach (ICharacter e in args[TARGETS]) {
					DebugLog.WriteLine("{0} is getting healed for {1} points", e.ShortDescription, args[AMOUNT]);

					e.Damage -= args[AMOUNT];

					// TODO: Handle on-healing triggers and more, full specification here https://hearthstone.gamepedia.com/Advanced_rulebook#Damage_and_Healing
					// TODO: Handle Prehealing. Currently nothing cares about it, but it does exist in the log.
					// TODO: In Hearthstone tags can't be set to a negative value (clamps to 0 instead), so I should be able to write it like this and not worry about reaching negative Damage.
				}
			return ActionResult.None;
		}
	}

	public class Silence : QueueAction
	{
		public const int TARGETS = 0;

		public override ActionResult Run(Game game, IEntity source, List<ActionResult> args) {
			throw new NotImplementedException(); // TODO: implement https://hearthstone.gamepedia.com/Advanced_rulebook#Silence + https://hearthstone.gamepedia.com/Silence
		}
	}

	public class Bounce : QueueAction
	{
		public const int TARGETS = 0;

		public override ActionResult Run(Game game, IEntity source, List<ActionResult> args) {
			throw new NotImplementedException(); // TODO: implement https://hearthstone.gamepedia.com/Advanced_rulebook#Zones + https://hearthstone.gamepedia.com/Return_to_hand
		}
	}

	public class Destroy : QueueAction
	{
		public const int TARGETS = 0;

		public override ActionResult Run(Game game, IEntity source, List<ActionResult> args) {
			throw new NotImplementedException(); // TODO: implement https://hearthstone.gamepedia.com/Advanced_rulebook#Destroy_effects_in_all_zones
		}
	}

	public class GainMana : QueueAction
	{
		public const int PLAYER = 0;
		public const int AMOUNT = 1;

		public override ActionResult Run(Game game, IEntity source, List<ActionResult> args) {
			throw new NotImplementedException(); // TODO: implement https://hearthstone.gamepedia.com/Advanced_rulebook#Mana_Crystals_and_mana_costs
		}
	}

	public class Summon : QueueAction
	{
		public const int PLAYER = 0;
		public const int ENTITY = 1;
		public const int AMOUNT = 2;

		public override ActionResult Run(Game game, IEntity source, List<ActionResult> args) {
			throw new NotImplementedException(); // TODO: implement https://hearthstone.gamepedia.com/Advanced_rulebook#Playing.2Fsummoning_a_minion

			// Notes on summon position:
			// 1) Friendly summon, unknown amount, battlecry/trigger: All to the right of the summoning minion (N'Zoth, Kel'Thuzad, Herald Volazj)
			// 2) Friendly summon, known amount, battlecry/trigger: Alternate right/left/right/left... of the summoner (Onyxia/Dr. Boom)
			// 3) Opponent summon: All to the far right (Leeroy Jenkins, Hungry Dragon)
			// 4) Deathrattle, unknown amount: All to the far right (Moat Lurker, Thaddius)
			// 5) Deathrattle, known amount: the position the minion died in (Haunted Creeper, Soul of the Forest, Ancestral Spirit) see https://hearthstone.gamepedia.com/Advanced_rulebook#Where_do_Minions_summoned_by_Deathrattles_spawn.3F
			// 6) Other: All to the far right (Reincarnate, Resurrect)
			// For edge cases where the played minion died or returned to hand before its summoning Battlecry resolves, see https://www.youtube.com/watch?v=nuzvKVL2Vlg
		}
	}

	public class Discard : QueueAction
	{
		public const int TARGETS = 0;

		public override ActionResult Run(Game game, IEntity source, List<ActionResult> args) {
			if (args[TARGETS].HasResult)
				foreach (IEntity e in args[TARGETS]) {
					DebugLog.WriteLine("{0} is discarded", e.ShortDescription);
					game.Environment.LastCardDiscarded = e;
					e.Zone = e.Controller.Graveyard;
					//TODO: Detach Enchantments, run on discard triggers, etc
				}
			return ActionResult.None;
		}
	}

	public class Death : QueueAction
	{
		public const int TARGETS = 0;

		public override ActionResult Run(Game game, IEntity source, List<ActionResult> args) {
			if (args[TARGETS].HasResult) {
				bool gameEnd = false;

				foreach (var e in args[TARGETS]) {
#if _ACTIONS_DEBUG
					DebugLog.WriteLine("Game {0}: {1} dies", game.GameId, e.ShortDescription);
#endif
					e.Zone = e.Controller.Graveyard;

					// Minion death
					if (e is Minion) {
						var minion = ((Minion)e);
						minion.Damage = 0;
						game.Queue(e, e.Card.Behaviour.Deathrattle);
					}

					// Hero death
					if (e is Hero) {
						e.Controller.PlayState = PlayState.LOSING;
						gameEnd = true;
					}
				}
				if (gameEnd)
					game.GameWon();
			}
			return ActionResult.None;
		}
	}

	public class CreateChoice : QueueAction
	{
		public const int PLAYER = 0;
		public const int ENTITIES = 1;
		public const int CHOICE_TYPE = 2;

		public override ActionResult Run(Game game, IEntity source, List<ActionResult> args) {
			var player = (Player) args[PLAYER];

			var choice = new Choice(
				Controller: player,
				ChoiceType: (ChoiceType)(int)args[CHOICE_TYPE],
				Choices: args[ENTITIES]
			);
			player.Choice = choice;

			// The mulligan is the only situation where:
			// 1. We are waiting for both players' input at the same time
			// 2. There will not be an action chaining on from the result
			// In all other cases, we must pause the queue until the user responds with a choice
			if (choice.ChoiceType != ChoiceType.MULLIGAN)
				game.ActionQueue.Paused = true;
			else
				player.MulliganState = MulliganState.INPUT;

			return ActionResult.None;
		}
	}

	public class Attack : QueueAction
	{
		public const int ATTACKER = 0;
		public const int DEFENDER = 1;

		public override ActionResult Run(Game game, IEntity source, List<ActionResult> args) {
			var attacker = (ICharacter)(Entity)args[ATTACKER];
			var defender = (ICharacter)(Entity)args[DEFENDER];

#if _ACTIONS_DEBUG
			DebugLog.WriteLine("Game {0}: {1} is attacking {2}", game.GameId, attacker.ShortDescription, defender.ShortDescription);
#endif
			source.Controller.NumFriendlyMinionsThatAttackedThisTurn++;
			game.ProposedAttacker = attacker;
			game.ProposedDefender = defender;
			attacker.IsAttacking = true;

			game.NextStep = Step.MAIN_ACTION;
			game.Step = Step.MAIN_COMBAT;
			attacker.Controller.NumOptionsPlayedThisTurn++;

			defender.IsDefending = true;

			defender.PreDamage = attacker.AttackDamage;

			// TODO: Allow other things to change the proposed attacker/defender here
			defender.PreDamage = 0;
			defender = game.ProposedDefender;

			if (attacker.ShouldExitCombat) {
				// TODO: Tag ordering unchecked for this case
				game.ProposedAttacker = null;
				game.ProposedDefender = null;
				attacker.IsAttacking = false;
				defender.IsDefending = false;
				return ActionResult.None;
			}

			// Save defender's attack as it might change after being damaged (e.g. enrage)
			int defAttack = defender.AttackDamage;

			defender.LastAffectedBy = attacker;

			game.Queue(attacker, Actions.Damage((Entity)defender, attacker.AttackDamage));

			// TODO: Attacker Predamage

			if (defAttack > 0)
				game.Queue(defender, Actions.Damage((Entity)attacker, defAttack));

			// TODO: Attacker LastAffectedBy

			game.Queue(source, new Action<IEntity>(_ =>
			{
				attacker.NumAttacksThisTurn++;
				// TODO: Use EXTRA_ATTACKS_THIS_TURN?
				attacker.IsExhausted = true;

				game.ProposedAttacker = null;
				game.ProposedDefender = null;
				attacker.IsAttacking = false;
				defender.IsDefending = false;

				// TODO: Move this to after death processing etc.
				game.Step = Step.MAIN_ACTION;
				game.NextStep = Step.MAIN_END;
			}));

			return ActionResult.None;
		}
	}

	public class Choose : QueueAction
	{
		public override ActionResult Run(Game game, IEntity source, List<ActionResult> args)
		{
			var player = (Player) source;
			var choices = player.Choice.Choices;

			if (player.Choice == null)
				throw new ChoiceException(source + " attempted to make a choice when no choice was available");

			if (player.Choice.ChoiceType == ChoiceType.MULLIGAN)
				player.MulliganState = MulliganState.DEALING;
			else if (player.Choice.ChoiceType == ChoiceType.GENERAL) {
				player.Choice = null;
				throw new NotImplementedException();
			}
			else
				throw new ChoiceException("Unknown choice type: " + player.Choice.ChoiceType);

			return choices;
		}
	}
}
