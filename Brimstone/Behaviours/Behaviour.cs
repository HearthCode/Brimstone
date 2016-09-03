using System.Collections.Generic;

namespace Brimstone
{
	// All of the fields you can add to a card definition
	public class Behaviour
	{
		public ActionGraph Battlecry;
		public ActionGraph Deathrattle;
		public List<Trigger> Triggers = new List<Trigger>();
	}
}
