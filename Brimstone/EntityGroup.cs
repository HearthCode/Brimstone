using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Brimstone
{
	public class EntityGroup<T> where T : class, IEntity
	{
		// Use an array because it's faster
		public T[] Entities { get; }

		public EntityGroup(T source, int clones) {
			Entities = new T[clones];
			for (int i = 0; i < clones; i++)
				Entities[i] = source.Clone() as T;
		}
	}
}
