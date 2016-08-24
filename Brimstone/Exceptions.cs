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

	public class InvalidChoiceException : BrimstoneException
	{
		public InvalidChoiceException() { }

		public InvalidChoiceException(string message) : base(message) { }

		public InvalidChoiceException(string message, Exception inner) : base(message, inner) { }
	}

	public class TreeSearchException : BrimstoneException
	{
		public TreeSearchException() { }

		public TreeSearchException(string message) : base(message) { }

		public TreeSearchException(string message, Exception inner) : base(message, inner) { }
	}

	public class ZoneMoveException : BrimstoneException
	{
		public ZoneMoveException() { }

		public ZoneMoveException(string message) : base(message) { }

		public ZoneMoveException(string message, Exception inner) : base(message, inner) { }
	}

	public class PlayRequirementNotImplementedException : BrimstoneException
	{
		public PlayRequirementNotImplementedException() { }

		public PlayRequirementNotImplementedException(string message) : base(message) { }

		public PlayRequirementNotImplementedException(string message, Exception inner) : base(message, inner) { }
	}
}
