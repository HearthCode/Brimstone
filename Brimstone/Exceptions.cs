using System;

namespace Brimstone
{
	public abstract class BrimstoneException : Exception
	{
		public BrimstoneException() { }
		public BrimstoneException(string message) : base(message) { }
		public BrimstoneException(string message, Exception inner) : base(message, inner) { }
	}

	public class ChoiceException : BrimstoneException
	{
		public ChoiceException() { }

		public ChoiceException(string message) : base(message) { }

		public ChoiceException(string message, Exception inner) : base(message, inner) { }
	}

	public class TreeSearchException : BrimstoneException
	{
		public TreeSearchException() { }

		public TreeSearchException(string message) : base(message) { }

		public TreeSearchException(string message, Exception inner) : base(message, inner) { }
	}

	public class ZoneException : BrimstoneException
	{
		public ZoneException() { }

		public ZoneException(string message) : base(message) { }

		public ZoneException(string message, Exception inner) : base(message, inner) { }
	}

	public class PlayRequirementException : BrimstoneException
	{
		public PlayRequirementException() { }

		public PlayRequirementException(string message) : base(message) { }

		public PlayRequirementException(string message, Exception inner) : base(message, inner) { }
	}

	public class TargetingException : BrimstoneException
	{
		public TargetingException() { }

		public TargetingException(string message) : base(message) { }

		public TargetingException(string message, Exception inner) : base(message, inner) { }
	}

	public class SelectorException : BrimstoneException
	{
		public SelectorException() { }

		public SelectorException(string message) : base(message) { }

		public SelectorException(string message, Exception inner) : base(message, inner) { }
	}
}
