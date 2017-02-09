/*
	Copyright 2016, 2017 Katy Coe
	Copyright 2016 Leonard Dahlmann
	Copyright 2016 Timothy Stiles

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
