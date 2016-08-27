using System.Collections.Generic;
using System.Linq;

namespace Brimstone
{
	public interface ICanTarget : IEntity
	{	
		// TODO: Caching
		// TODO: HasTarget
		List<ICharacter> ValidTargets { get; }

		// TODO: Add cloning code + cloning unit test
		ICharacter Target { get; set; }
	}

	public abstract class CanTarget : Entity, ICanTarget
	{
		protected CanTarget(CanTarget cloneFrom) : base(cloneFrom) { }
		protected CanTarget(Card card, Dictionary<GameTag, int> tags = null) : base(card, tags) { }

		public ICharacter Target { get; set; }

		public abstract List<ICharacter> ValidTargets { get; }
	}
}
