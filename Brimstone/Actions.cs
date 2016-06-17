using System;
using System.Collections.Generic;
using System.Linq;

namespace Brimstone
{
	public class LazyNumber : QueueAction
	{
		public int Num { get; set; }

		public override ActionResult Run(Game game, List<ActionResult> args) {
			return Num;
		}
	}

	public class LazyCard : QueueAction
	{
		public Card Card { get; set; }

		public override ActionResult Run(Game game, List<ActionResult> args) {
			return Card;
		}
	}

	public class LazyEntity : QueueAction
	{
		public Entity Entity { get; set; }

		public override ActionResult Run(Game game, List<ActionResult> args) {
			return Entity;
		}
	}

	public class RandomOpponentMinion : QueueAction
	{
		public override ActionResult Run(Game game, List<ActionResult> args) {
			if (game.Opponent.InPlay.Count == 0)
				return new List<IEntity>();
			var m = RNG.Between(1, game.Opponent.InPlay.Count);
			return (Minion)game.Opponent.InPlay[m];
		}
	}

	public class AllMinions : QueueAction
	{
		public override ActionResult Run(Game game, List<ActionResult> args) {
			return game.CurrentPlayer.InPlay.Concat(game.Opponent.InPlay).ToList();
		}
	}

	public class RandomAmount : QueueAction
	{
		public override ActionResult Run(Game game, List<ActionResult> args) {
			return RNG.Between(args[0], args[1]);
		}
	}

	public class BeginTurn : QueueAction
	{
		public override ActionResult Run(Game game, List<ActionResult> args) {
			var temp = game.CurrentPlayer;
			game.CurrentPlayer = game.Opponent;
			game.Opponent = temp;
			game[GameTag.STEP] = (int) Step.MAIN_ACTION;

			return ActionResult.None;
		}
	}

	public class Give : QueueAction
	{
		public const int TARGET = 0;
		public const int CARD = 1;

		public override ActionResult Run(Game game, List<ActionResult> args) {
			Player player = (Player)args[TARGET];
			Card card = args[CARD];

			Console.WriteLine("Giving {0} to {1}", card.Name, player.FriendlyName);

			if (card[GameTag.CARDTYPE] == (int)CardType.MINION) {
				return (Minion)player.Hand.MoveTo(new Minion(game, player, card));
			} else if (card[GameTag.CARDTYPE] == (int)CardType.SPELL) {
				return (Spell)player.Hand.MoveTo(new Spell(game, player, card));
			}
			// TODO: Weapons

			return ActionResult.None;
		}
	}

	public class Play : QueueAction
	{
		public const int PLAYER = 0;
		public const int ENTITY = 1;

		public override ActionResult Run(Game game, List<ActionResult> args) {
			Player player = (Player)args[PLAYER];
			Entity entity = args[ENTITY];

			player.InPlay.MoveTo(entity);

			Console.WriteLine("{0} is playing {1}", player.FriendlyName, entity.Card.Name);

			game.ActionQueue.Enqueue(entity.Card.Behaviour.Battlecry);
			return entity;
		}
	}

	public class Damage : QueueAction
	{
		private const int TARGETS = 0;
		private const int DAMAGE = 1;

		public override ActionResult Run(Game game, List<ActionResult> args) {
			if (args[TARGETS].HasResult)
				foreach (Minion e in args[TARGETS]) {
					Console.WriteLine("{0} is getting hit for {1} points of damage", e.Card.Name, args[DAMAGE]);

					e[GameTag.HEALTH]-= args[DAMAGE];
					e[GameTag.DAMAGE] = e.Card[GameTag.HEALTH] - e[GameTag.HEALTH];
					e.CheckForDeath();

					// TODO: What if one of our targets gets killed?
				}
			return ActionResult.None;
		}
	}
}