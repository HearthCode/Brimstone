using System;

namespace Brimstone
{
	// We can't continue until a player has made a choice (ie. mulligan, discover)
	public class PendingChoiceException : Exception
	{
		public PendingChoiceException() { }

		public PendingChoiceException(string message) : base(message) { }

		public PendingChoiceException(string message, Exception inner) : base(message, inner) { }
	}
}
