﻿using System;

namespace Brimstone.Exceptions
{
	public abstract class BrimstoneException : Exception
	{
		protected BrimstoneException() { }
		protected BrimstoneException(string message) : base(message) { }
		protected BrimstoneException(string message, Exception inner) : base(message, inner) { }
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

	public class ActionBlockException : BrimstoneException
	{
		public ActionBlockException() { }

		public ActionBlockException(string message) : base(message) { }

		public ActionBlockException(string message, Exception inner) : base(message, inner) { }
	}

	public class TriggerException : BrimstoneException
	{
		public TriggerException() { }

		public TriggerException(string message) : base(message) { }

		public TriggerException(string message, Exception inner) : base(message, inner) { }
	}
}
