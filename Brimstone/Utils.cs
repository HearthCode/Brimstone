using System;
using System.Collections.Generic;
using System.Linq;

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

	public class Choice
	{
		public IZoneOwner Controller { get; }
		public ChoiceType ChoiceType { get; }
		public IEnumerable<IEntity> Choices { get; }

		// TODO: Implement
		public Choice(IZoneOwner Controller, IEnumerable<IEntity> Choices, ChoiceType ChoiceType = ChoiceType.GENERAL) {
			this.Controller = Controller;
			this.Choices = Choices;
			this.ChoiceType = ChoiceType;
		}

		public void Select(IEntity Choice) {
			if (ChoiceType != ChoiceType.GENERAL)
				throw new InvalidChoiceException();

			throw new NotImplementedException();
		}

		public void Keep(IEnumerable<IEntity> Choices) {
			if (ChoiceType != ChoiceType.MULLIGAN)
				throw new InvalidChoiceException();

			if (!Choices.Except(this.Choices).Any())
				throw new InvalidChoiceException();
		}

		public void Discard(List<IEntity> Choices) {
			if (ChoiceType != ChoiceType.MULLIGAN)
				throw new InvalidChoiceException();

			if (!Choices.Except(this.Choices).Any())
				throw new InvalidChoiceException();
		}
	}

	public static class DebugLog {
		public static void WriteLine(string s, params object[] p) {
#if DEBUG
			System.Diagnostics.Debug.WriteLine(
				string.Format("[{0}] Thread {1}: ",
					DateTime.Now.ToString("HH:mm:ss.fff"),
					System.Threading.Thread.CurrentThread.ManagedThreadId)
				+ s, p);
#endif
		}
	}
}