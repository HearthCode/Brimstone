using System;
using System.Collections.Generic;
using System.Linq;
using Brimstone.Entities;
using Brimstone.PowerActions;

// Core game actions that are not specific to any individual game
namespace Brimstone.QueueActions
{
	public class Empty : PreCompiledQueueAction
	{
		internal override ActionResult Run(Game game, IEntity source, ActionResult[] args) {
			return ActionResult.Empty;
		}
	}

	public class FixedNumber : PreCompiledQueueAction
	{
		public int Num { get; set; }

		internal override ActionResult Run(Game game, IEntity source, ActionResult[] args) {
			return Num;
		}
	}

	public class FixedCard : PreCompiledQueueAction
	{
		public Card Card { get; set; }

		internal override ActionResult Run(Game game, IEntity source, ActionResult[] args) {
			return Card;
		}
	}

	public class LazyEntity : EagerQueueAction
	{
		public int EntityId { get; set; }

		internal override ActionResult Run(Game game, IEntity source, ActionResult[] args) {
			return (Entity)game.Entities[EntityId];
		}

		public override string ToString() {
			return "LazyEntity(" + EntityId + ")";
		}
	}

	public class Selector : EagerQueueAction
	{
		public Func<IEntity, IEnumerable<IEntity>> Lambda { get; set; }

		internal override ActionResult Run(Game game, IEntity source, ActionResult[] args) {
			return Lambda(source)?.ToList() ?? new List<IEntity>();
		}

		// Merge the output of N selectors, allowing duplicates (multi-set union)
		internal static Selector Combine(params Selector[] s) {
			if (s.Length == 0)
				return null;
			if (s.Length == 1)
				return s[0];
			return new Selector {
				Lambda = e => {
					var r = s[0].Lambda(e);
					for (int i = 1; i < s.Length; i++)
						r = r?.Concat((s[i].Lambda(e)) ?? new List<IEntity>()) ?? s[i].Lambda(e);
					return r;
				}
			};
		}

		// Remove any occurrences of items in Y from X (set difference)
		internal static Selector Except(Selector x, Selector y) {
			return new Selector {
				Lambda = e => x.Lambda(e)?.Except((y.Lambda(e)) ?? new List<IEntity>())
			};
		}

		// Return only items present in all N selectors (set intersection)
		internal static Selector InAll(params Selector[] s) {
			if (s.Length == 0)
				return null;
			if (s.Length == 1)
				return s[0];
			return new Selector {
				Lambda = e => {
					var r = s[0].Lambda(e);
					for (int i = 1; i < s.Length; i++)
						r = r?.Intersect((s[i].Lambda(e)) ?? new List<IEntity>()) ?? s[i].Lambda(e);
					return r;
				}
			};
		}

		public static Selector operator +(Selector x, Selector y) {
			return Combine(x, y);
		}

		public static Selector operator -(Selector x, Selector y) {
			return Except(x, y);
		}

		public static Selector operator &(Selector x, Selector y) {
			return InAll(x, y);
		}
	}

	public class Func : QueueAction
	{
		public Action<IEntity> F { get; set; }

		internal override ActionResult Run(Game game, IEntity source, ActionResult[] args) {
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

		internal override ActionResult Run(Game game, IEntity source, ActionResult[] args) {
			game.PowerHistory?.Add(Block);
			game.ActionQueue.StartBlock(source, Actions, Block);
			return ActionResult.None;
		}

		public override string ToString() {
			string s = "GameBlock(" + Block.Type + ", ";
			if (Actions != null)
				foreach (var a in Actions)
					s += a + ", ";
			return s.Substring(0, s.Length - 2) + ")";
		}
	}

	public class RandomChoice : QueueAction
	{
		public const int ENTITIES = 0;

		internal override ActionResult Run(Game game, IEntity source, ActionResult[] args) {
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

		internal override ActionResult Run(Game game, IEntity source, ActionResult[] args) {
			return RNG.Between(args[MIN], args[MAX]);
		}
	}

	public class Repeat : QueueAction
	{
		public ActionGraph Actions { get; set; }

		public const int AMOUNT = 0;

		internal override ActionResult Run(Game game, IEntity source, ActionResult[] args) {
			for (int i = 0; i < args[AMOUNT]; i++)
				game.Queue(source, Actions);
			return ActionResult.None;
		}
	}
}
