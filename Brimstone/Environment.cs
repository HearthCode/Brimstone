using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Brimstone
{
	public class Environment : ICloneable
	{
		public Game Game { get; set; }

		public Environment(Game game) {
			Game = game;
		}

		private int _lastDamaged;
		public ICharacter LastDamaged {
			get { return (ICharacter) Game.Entities[_lastDamaged]; }
			set { _lastDamaged = value.Id; }
		}

		public object Clone() {
			return MemberwiseClone();
		}
	}
}
