using System;

namespace Brimstone
{
	public static class RNG
	{
		private static Random random = new Random();

		public static int Between(int min, int max) {
			return random.Next(min, max + 1);
		}
	}
}