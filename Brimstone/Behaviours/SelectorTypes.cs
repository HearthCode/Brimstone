using System;
using System.Collections.Generic;
using System.Linq;

namespace Brimstone
{
	// All of the selector types you can use
	public partial class Behaviours
	{
		// TODO: Add selector set ops

		// Base selectors taking any lambda expression
		public static Selector Select(Func<IEntity, IEntity> selector) {
			return new Selector { Lambda = e => new List<IEntity> { selector(e) } };
		}
		public static Selector Select(Func<IEntity, IEnumerable<IEntity>> selector) {
			return new Selector { Lambda = selector };
		}

		// Merge the output of N selectors, allowing duplicates
		public static Selector Union(params Selector[] s) {
			if (s.Length < 2)
				throw new SelectorException("Selector union requires at least 2 arguments");

			if (s.Length > 2)
				s[1] = Union(s.Skip(1).ToArray());

			var sel = new Selector {
				Lambda = e => s[0].Lambda(e).Concat(s[1].Lambda(e))
			};
			return sel;
		}
	}
}
