using System;

namespace Brimstone
{
	public abstract class BrimstoneException : Exception
	{
		public BrimstoneException() { }
		public BrimstoneException(string message) : base(message) { }
		public BrimstoneException(string message, Exception inner) : base(message, inner) { }
	}

	// We can't continue until a player has made a choice (ie. mulligan, discover)
	public class PendingChoiceException : BrimstoneException
	{
		public PendingChoiceException() { }

		public PendingChoiceException(string message) : base(message) { }

		public PendingChoiceException(string message, Exception inner) : base(message, inner) { }
	}

	public class TreeSearchException : BrimstoneException
	{
		public TreeSearchException() { }

		public TreeSearchException(string message) : base(message) { }

		public TreeSearchException(string message, Exception inner) : base(message, inner) { }
	}
}
