using System;
using System.Collections.Generic;
using System.Linq;

namespace Brimstone
{
	public class FixedNumber : QueueAction
	{
		public int Num { get; set; }

		public override ActionResult Run(List<ActionResult> args) {
			return Num;
		}
	}

	public class RandomOpponentMinion : QueueAction
	{
		public override ActionResult Run(List<ActionResult> args) {
			if (Game.Opponent.ZonePlay.Count == 0)
				return new List<IEntity>();
			var m = new Random().Next(Game.Opponent.ZonePlay.Count);
			return (Minion)Game.Opponent.ZonePlay[m];
		}
	}

	public class AllMinions : QueueAction
	{
		public override ActionResult Run(List<ActionResult> args) {
			return Game.CurrentPlayer.ZonePlay.Concat(Game.Opponent.ZonePlay) as List<IEntity>;
		}
	}

	public class RandomAmount : QueueAction
	{
		public override ActionResult Run(List<ActionResult> args) {
			return RNG.Between(args[0], args[1]);
		}
	}

	public class Damage : QueueAction
	{
		private const int TARGETS = 0;
		private const int DAMAGE = 1;

		public override ActionResult Run(List<ActionResult> args) {
			if (args[TARGETS].HasResult)
				foreach (Minion e in args[TARGETS])
					e.Damage(args[DAMAGE]);
			return ActionResult.None;
		}
	}
}