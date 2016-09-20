using Brimstone.QueueActions;

namespace Brimstone
{
	// All of the game actions you can use
	public partial class Actions
	{
		public static ActionGraph Concede(ActionGraph Player = null) { return new Concede { Args = { Player } }; }
		public static ActionGraph Give(ActionGraph Player = null, ActionGraph Card = null) { return new Give { Args = { Player, Card } }; }
		public static ActionGraph Draw(ActionGraph Player = null) { return new Draw { Args = { Player } }; }
		public static ActionGraph Play(ActionGraph Entity = null) { return new Play { Args = { Entity } }; }
		public static ActionGraph UseHeroPower(ActionGraph Player = null) { return new UseHeroPower { Args = { Player } }; }
		public static ActionGraph Attack(ActionGraph Attacker = null, ActionGraph Defender = null) { return new Attack { Args = { Attacker, Defender } }; }
		public static ActionGraph Choose(ActionGraph Player = null) { return new Choose { Args = { Player } }; }
		public static ActionGraph Damage(ActionGraph Targets = null, ActionGraph Amount = null) { return new Damage { Args = { Targets, Amount } }; }
		public static ActionGraph Heal(ActionGraph Targets = null, ActionGraph Amount = null) { return new Heal { Args = { Targets, Amount } }; }
		public static ActionGraph Silence(ActionGraph Targets = null) { return new Silence { Args = { Targets } }; }
		public static ActionGraph Bounce(ActionGraph Targets = null) { return new Bounce { Args = { Targets } }; }
		public static ActionGraph Destroy(ActionGraph Targets = null) { return new Destroy { Args = { Targets } }; }
		public static ActionGraph GainMana(ActionGraph Player = null, ActionGraph Amount = null) { return new GainMana { Args = { Player, Amount } }; }
		public static ActionGraph Summon(ActionGraph Player = null, ActionGraph Card = null) { return new Summon { Args = { Player, Card } }; }
		public static ActionGraph Discard(ActionGraph Targets = null) { return new Discard { Args = { Targets } }; }

		// Random decision actions
		public static ActionGraph Random(Selector Selector = null) { return new RandomChoice { Args = { Selector } }; }
		public static ActionGraph RandomAmount(ActionGraph Min = null, ActionGraph Max = null) { return new RandomAmount { Args = { Min, Max } }; }
	}
}
