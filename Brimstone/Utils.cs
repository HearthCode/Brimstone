using System;
using System.Collections.Generic;

namespace Brimstone
{
	public static class RNG {
		private static Random random = new Random();

		public static int Between(int min, int max) {
			return random.Next(min, max + 1);
		}
	}

	public static class RNG<T> {
		public static T Choose(List<T> choices) {
			return choices[RNG.Between(0, choices.Count - 1)];
		}
	}
}