using System.Collections.Generic;

namespace Brimstone
{
	public class Option
	{
		public IEntity Source;
		public IEnumerable<Option> SubOptions;
		public IEnumerable<IEntity> Targets;
	}
}
