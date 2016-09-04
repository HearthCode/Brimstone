using System;
using System.Linq;
using Brimstone.Actions;

namespace Brimstone
{
	public class Condition
	{
		private readonly Func<IEntity, IEntity, bool> condition;

		public Condition(Func<IEntity, IEntity, bool> Condition) {
			condition = Condition;
		}

		public bool Eval(IEntity owner, IEntity affected) {
			return condition(owner, affected);
		}

		public static implicit operator Condition(Selector s) {
			return new Condition((x, y) => s.Lambda(x).Contains(y));
		}
	}
}
