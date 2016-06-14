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
			if (game.Opponent.ZonePlay.Count == 0)
				return new List<IEntity>();
			var m = new Random().Next(game.Opponent.ZonePlay.Count);
			return (Minion)game.Opponent.ZonePlay[m];
		}
	}

	public class AllMinions : QueueAction
	{
		public override ActionResult Run(Game game, List<ActionResult> args) {
			return game.CurrentPlayer.ZonePlay.Concat(game.Opponent.ZonePlay) as List<IEntity>;
		}
	}

	public class RandomAmount : QueueAction
	{
		public override ActionResult Run(Game game, List<ActionResult> args) {
			return RNG.Between(args[0], args[1]);
		}
	}

	public class Give : QueueAction
	{
		public const int TARGET = 0;
		public const int CARD = 1;

		public override ActionResult Run(Game game, List<ActionResult> args) {
			Player player = (Player)args[TARGET];
			Card card = args[CARD];

			Console.WriteLine("Giving {0} to {1}", card, player);

			if (card[GameTag.CARDTYPE] == (int)CardType.MINION) {
				var minion = new Minion(game, card);
				player.ZoneHand.Add(minion);
				minion[GameTag.ZONE] = (int)Zone.HAND;
				minion[GameTag.ZONE_POSITION] = player.ZoneHand.Count + 1;
				return minion;
			}
			return ActionResult.None;
		}
	}

	public class Play : QueueAction
	{
		public const int PLAYER = 0;
		public const int ENTITY = 1;

		public override ActionResult Run(Game game, List<ActionResult> args) {
			Player player = (Player)args[PLAYER];
			IMinion entity = (IMinion) (Entity) args[ENTITY];

			entity.Health = (int)entity.Card[GameTag.HEALTH];
			player.ZoneHand.Remove(entity);
			player.ZonePlay.Add(entity);
			entity[GameTag.ZONE] = (int)Zone.PLAY;
			entity[GameTag.ZONE_POSITION] = player.ZonePlay.Count;

			Console.WriteLine("{0} is playing {1}", player, entity);

			game.ActionQueue.Enqueue(entity.Card.Behaviour.Battlecry);
			game.ActionQueue.Process();
			return (Entity) entity;
		}
	}

	public class Damage : QueueAction
	{
		private const int TARGETS = 0;
		private const int DAMAGE = 1;

		public override ActionResult Run(Game game, List<ActionResult> args) {
			if (args[TARGETS].HasResult)
				foreach (Minion e in args[TARGETS]) {
					Console.WriteLine("{0} is getting hit for {1} points of damage", e, args[DAMAGE]);

					e.Health -= args[DAMAGE];
					e[GameTag.DAMAGE] = e.Card[GameTag.HEALTH] - e.Health;
					e.CheckForDeath();
				}
			return ActionResult.None;
		}
	}
}