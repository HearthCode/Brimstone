using System;
using System.Collections.Generic;
using System.Linq;
using static Brimstone.Behaviours;

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
		public Player Controller { get; }
		public ChoiceType ChoiceType { get; }
		public List<IEntity> Choices { get; }
		public List<IEntity> Keeping { get; private set; } = null;

		public List<IEntity> Discarding => (Keeping != null)? Choices.Except(Keeping).ToList() : null;

		public Choice(Player Controller, List<IEntity> Choices, ChoiceType ChoiceType = ChoiceType.GENERAL) {
			this.Controller = Controller;
			this.Choices = Choices;
			this.ChoiceType = ChoiceType;
		}

		public void Pick(IEntity Choice) {
			if (ChoiceType != ChoiceType.GENERAL)
				throw new ChoiceException("Attempting to select a card with Pick for a mulligan or unknown choice card");

			if (!Choices.Contains(Choice))
				throw new ChoiceException("Attempting to select an unavailable card with Pick");

			Keeping = new List<IEntity>() {Choice};
			Controller.Game.Action(Controller, Choose(Controller));
		}

		public void Keep(Func<IEntity, bool> Chooser) {
			Keep(Choices.Where(Chooser));
		}

		public void Keep(IEnumerable<IEntity> Choices)
		{
			if (ChoiceType != ChoiceType.MULLIGAN)
				throw new ChoiceException("Attempting to make a non-mulligan selection with Keep");

			if (Choices.Except(this.Choices).Any())
				throw new ChoiceException("Attempting to keep unavailable cards in mulligan selection");

			Keeping = new List<IEntity>(Choices);
			Controller.Game.Action(Controller, Choose(Controller));
		}

		public void Discard(Func<IEntity, bool> Chooser) {
			Discard(Choices.Where(Chooser));
		}

		public void Discard(IEnumerable<IEntity> Choices) {
			if (ChoiceType != ChoiceType.MULLIGAN)
				throw new ChoiceException("Attempting to make a non-mulligan selection with Discard");

			if (Choices.Except(this.Choices).Any())
				throw new ChoiceException("Attempting to discard unavailable cards in mulligan selection");

			Keeping = new List<IEntity>(this.Choices.Except(Choices));
			Controller.Game.Action(Controller, Choose(Controller));
		}
	}

	// TODO: Replace this with log4net
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