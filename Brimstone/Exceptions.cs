/*
	Copyright 2016, 2017 Katy Coe
	Copyright 2016 Leonard Dahlmann

	This file is part of Brimstone.

	Brimstone is free software: you can redistribute it and/or modify
	it under the terms of the GNU Lesser General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	Brimstone is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU Lesser General Public License for more details.

	You should have received a copy of the GNU Lesser General Public License
	along with Brimstone.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;

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
