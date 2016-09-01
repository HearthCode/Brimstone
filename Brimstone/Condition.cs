using System;
using System.Linq;

namespace Brimstone
{
	public interface ICondition {
		bool Eval(IEntity owner, IEntity affected);
	}


	public class Condition<T, U> : ICondition where T : IEntity where U : IEntity
	{
		private readonly Func<T, U, bool> condition;

		public Condition(Func<T, U, bool> Condition) {
			condition = Condition;
		}

		bool ICondition.Eval(IEntity owner, IEntity affected) {
			return Eval((T) owner, (U) affected);
		}

		public bool Eval(T owner, U affected) {
			return condition(owner, affected);
		}

		public static implicit operator Condition<T, U>(Selector s) {
			return new Condition<T, U>((x, y) => s.Lambda(x).Contains(y));
		}
	}
}
