using System.Collections.Generic;

namespace Brimstone
{
	public class Option
	{
		public IEntity Source;
		public IEnumerable<Option> SubOptions;
		public IEnumerable<IEntity> Targets;

		public override string ToString()
		{
			string s = Source.ShortDescription + " => {{  ";
			foreach (var t in Targets)
				s += t.ShortDescription + ", ";
			return s.Substring(0, s.Length - 2) + "  }}";
		}
	}
}
