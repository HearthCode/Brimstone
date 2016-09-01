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

		private int _lastCardPlayed;
		public IEntity LastCardPlayed {
			get { return (IEntity)Game.Entities[_lastCardPlayed]; }
			set { _lastCardPlayed = value.Id; }
		}

		private int _lastCardDiscarded;
		public IEntity LastCardDiscarded {
			get { return (IEntity)Game.Entities[_lastCardDiscarded]; }
			set { _lastCardDiscarded = value.Id; }
		}

		public object Clone() {
			return MemberwiseClone();
		}
	}
}
