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
		public SelectionSource SelectionSource { get; set; }

		public Func<IEntity, IEnumerable<IEntity>> Lambda { get; set; }

		public override ActionResult Run(Game game, IEntity source, List<ActionResult> args) {
			switch (SelectionSource) {
				case SelectionSource.Game:
					return Lambda(game).ToList();
				case SelectionSource.Player:
					if (source is Game)
						return Lambda(game.CurrentPlayer).ToList();
					else if (source is Player)
						return Lambda(source).ToList();
					else
						return Lambda(source.Controller).ToList();
				case SelectionSource.ActionSource:
					return Lambda(source).ToList();
				default:
					throw new NotImplementedException();
			}
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

	public class BeginTurn : QueueAction
	{
		public override ActionResult Run(Game game, IEntity source, List<ActionResult> args)
		{
			game.Step = Step.MAIN_READY;

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

			// TODO: Reset EXHAUSTED tags for current player here

			game.CurrentPlayer.NumCardsDrawnThisTurn = 0;

			// Ain't no rest for the triggered...
			game.NextStep = Step.MAIN_START_TRIGGERS;

			game.CurrentPlayer.NumFriendlyMinionsThatDiedThisTurn = 0;
			game.CurrentOpponent.NumFriendlyMinionsThatDiedThisTurn = 0;

			game.Step = Step.MAIN_START_TRIGGERS;
			game.NextStep = Step.MAIN_START;

			// TODO: DEATHs block for eg. Doomsayer

			game.Step = Step.MAIN_START;
			game.Queue(game.CurrentPlayer, Actions.Draw(game.CurrentPlayer).Then((Action<IEntity>)(_ =>
			{
				game.CurrentPlayer.NumMinionsPlayerKilledThisTurn = 0;
				game.CurrentOpponent.NumMinionsPlayerKilledThisTurn = 0;
				game.CurrentPlayer.NumFriendlyMinionsThatAttackedThisTurn = 0;
				game.NumMinionsKilledThisTurn = 0;
				game.CurrentPlayer.HeroPowerActivationsThisTurn = 0;
				game.NextStep = Step.MAIN_ACTION;
				
				game.Step = Step.MAIN_ACTION;
				game.NextStep = Step.MAIN_END;
			}
			)));

			return ActionResult.None;
		}
	}

	public class EndTurn : QueueAction
	{
		public override ActionResult Run(Game game, IEntity source, List<ActionResult> args)
		{
			game.Step = Step.MAIN_END;
			game.NextStep = Step.MAIN_CLEANUP;
			game.Step = Step.MAIN_CLEANUP;

			// TODO: reset JUST_PLAYEDs for current player to zero here

			game.NextStep = Step.MAIN_NEXT;
			game.Step = Step.MAIN_NEXT;

			// This is probably going to be used to give players extra turns later
			game.CurrentPlayer.NumTurnsLeft = 0;
			game.CurrentOpponent.NumTurnsLeft = 1;

			game.CurrentPlayer = game.CurrentOpponent;
			game.Turn++;

			game.NextStep = Step.MAIN_READY;
			game.Queue(game, Actions.BeginTurn);

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

			DebugLog.WriteLine("Giving {0} to {1}", card.Name, player.FriendlyName);

			if (card.Type == CardType.MINION)
				return new Minion(card) {Zone = player.Hand};
			if (card.Type == CardType.SPELL)
				return new Spell(card) {Zone = player.Hand};
			// TODO: Weapons

			return ActionResult.None;
		}
	}

	public class Draw : QueueAction
	{
		public const int PLAYER = 0;

		public override ActionResult Run(Game game, IEntity source, List<ActionResult> args) {
			Player player = (Player)args[PLAYER];

			if (!player.Deck.IsEmpty) {
				Entity entity = (Entity)player.Deck[1];

				DebugLog.WriteLine("{0} draws {1}", player.FriendlyName, entity.ShortDescription);

				entity.Zone = player.Hand;
				player.NumCardsDrawnThisTurn++;
				return entity;
			}

			DebugLog.WriteLine("{0} tries to draw but their deck is empty", player.FriendlyName);
			return ActionResult.None;
		}
	}

	public class Play : QueueAction
	{
		// TODO: Deal with targeting

		public const int ENTITY = 0;

		public override ActionResult Run(Game game, IEntity source, List<ActionResult> args) {
			Player player = (Player)source.Controller;
			Entity entity = args[ENTITY];

			entity.Zone = player.Board;

			DebugLog.WriteLine("{0} is playing {1}", player.FriendlyName, entity.ShortDescription);

			if (entity is Minion)
				game.Queue(entity, entity.Card.Behaviour.Battlecry);
			else if (entity is Spell)
				game.Queue(entity, entity.Card.Behaviour.Battlecry.Then((Action<IEntity>) (_ =>
				{
					// Spells go to the graveyard after they are played
					entity.Zone = entity.Controller.Graveyard;
				})));
			return entity;
		}
	}

	public class Damage : QueueAction
	{
		public const int TARGETS = 0;
		public const int DAMAGE = 1;

		public override ActionResult Run(Game game, IEntity source, List<ActionResult> args) {
			if (args[TARGETS].HasResult)
				foreach (Character e in args[TARGETS]) {
					DebugLog.WriteLine("{0} is getting hit for {1} points of damage", e.ShortDescription, args[DAMAGE]);

					e.Damage += args[DAMAGE];
					e.CheckForDeath();

					// TODO: What if one of our targets gets killed?
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
					DebugLog.WriteLine("{0} dies", e.ShortDescription);

					e.Zone = e.Controller.Graveyard;

					// Minion death
					if (e is Minion) {
						((Minion) e).Damage = 0;
						game.Queue(e, e.Card.Behaviour.Deathrattle);
					}

					// Hero death
					if (e is Hero) {
						((Player) e.Controller).PlayState = PlayState.LOSING;
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
			var choice = new Choice(
				Controller: (IZoneOwner)(Entity)args[PLAYER],
				ChoiceType: (ChoiceType)(int)args[CHOICE_TYPE],
				Choices: args[ENTITIES]
			);
			((Player)args[PLAYER]).Choice = choice;

			// The mulligan is the only situation where:
			// 1. We are waiting for both players' input at the same time
			// 2. There will not be an action chaining on from the result
			// In all other cases, we must pause the queue until the user responds with a choice
			if (choice.ChoiceType != ChoiceType.MULLIGAN)
				game.ActionQueue.Paused = true;

			return ActionResult.None;
		}
	}

	public class Choose : QueueAction
	{
		public const int PLAYER = 0;

		private void chooseMulligan(Player p) {
			p.MulliganState = MulliganState.DEALING;

			// Perform mulligan
			foreach (var e in p.Choice.Discarding)
				e.ZoneSwap(p.Deck[RNG.Between(1, p.Deck.Count)]);

			p.MulliganState = MulliganState.WAITING;
			p.MulliganState = MulliganState.DONE;

			// Start main game if both players have completed mulligan
			if (p.Opponent.MulliganState == MulliganState.DONE)
			{
				p.Game.NextStep = Step.MAIN_READY;
				p.Game.Queue(p.Game, Actions.BeginTurn);
			}
		}

		private void chooseGeneral(Player p) {
			// TODO: General choices
			throw new NotImplementedException();
		}

		public override ActionResult Run(Game game, IEntity source, List<ActionResult> args) {
			var player = (Player)args[PLAYER];

			if (player.Choice == null)
				throw new InvalidChoiceException();

			if (player.Choice.ChoiceType == ChoiceType.MULLIGAN)
				chooseMulligan(player);
			else if (player.Choice.ChoiceType == ChoiceType.GENERAL)
				chooseGeneral(player);
			else
				throw new InvalidChoiceException();

			var result = player.Choice.Choices;
			player.Choice = null;
			return result.ToList();
		}
	}
}
