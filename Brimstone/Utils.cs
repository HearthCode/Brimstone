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
		public IZoneController Controller { get; }
		public ChoiceType ChoiceType { get; }
		public List<IEntity> Choices { get; }
		public List<IEntity> Keeping { get; private set; } = null;

		public List<IEntity> Discarding => (Keeping != null)? Choices.Except(Keeping).ToList() : null;

		public Choice(IZoneController Controller, List<IEntity> Choices, ChoiceType ChoiceType = ChoiceType.GENERAL) {
			this.Controller = Controller;
			this.Choices = Choices;
			this.ChoiceType = ChoiceType;
		}

		public void Pick(IEntity Choice) {
			if (ChoiceType != ChoiceType.GENERAL)
				throw new InvalidChoiceException();

			if (!Choices.Contains(Choice))
				throw new InvalidChoiceException();

			Keeping = new List<IEntity>() {Choice};
			Controller.Game.Action(Controller, Actions.Choose((Player)Controller));
		}

		// TODO: Allow passing of lambda to Keep and Discard
		public void Keep(IEnumerable<IEntity> Choices)
		{
			if (ChoiceType != ChoiceType.MULLIGAN)
				throw new InvalidChoiceException();

			if (Choices.Except(this.Choices).Any())
				throw new InvalidChoiceException();

			Keeping = new List<IEntity>(Choices);
			Controller.Game.Action(Controller, Actions.Choose((Player) Controller));
		}

		public void Discard(IEnumerable<IEntity> Choices) {
			if (ChoiceType != ChoiceType.MULLIGAN)
				throw new InvalidChoiceException();

			if (Choices.Except(this.Choices).Any())
				throw new InvalidChoiceException();

			Keeping = new List<IEntity>(this.Choices.Except(Choices));
			Controller.Game.Action(Controller, Actions.Choose((Player) Controller));
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