using System.Collections.Generic;
using System.Linq;
using Brimstone.Actions;
using Brimstone.Entities;

namespace Brimstone
{
	// Selector set operations
	public partial class Behaviours
	{
		// Merge the output of N selectors, allowing duplicates (multi-set union)
		internal static Selector Combine(params Selector[] s) {
			if (s.Length == 0)
				return null;
			if (s.Length == 1)
				return s[0];
			return new Selector {
				Lambda = e => {
					var r = s[0].Lambda(e);
					for (int i = 1; i < s.Length; i++)
						r = r?.Concat((s[i].Lambda(e)) ?? new List<IEntity>()) ?? s[i].Lambda(e);
					return r;
				}
			};
		}

		// Remove any occurrences of items in Y from X (set difference)
		internal static Selector Except(Selector x, Selector y) {
			return new Selector {
				Lambda = e => x.Lambda(e)?.Except((y.Lambda(e)) ?? new List<IEntity>())
			};
		}

		// Return only items present in all N selectors (set intersection)
		internal static Selector InAll(params Selector[] s) {
			if (s.Length == 0)
				return null;
			if (s.Length == 1)
				return s[0];
			return new Selector {
				Lambda = e => {
					var r = s[0].Lambda(e);
					for (int i = 1; i < s.Length; i++)
						r = r?.Intersect((s[i].Lambda(e)) ?? new List<IEntity>()) ?? s[i].Lambda(e);
					return r;
				}
			};
		}
	}
}
