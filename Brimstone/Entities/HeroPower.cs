/*
	Copyright 2016, 2017 Katy Coe

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

using System;
using System.Collections.Generic;
using System.Linq;
 using Brimstone.Exceptions;

namespace Brimstone.Entities
{
	public class HeroPower : CanTarget
	{
		public HeroPower(HeroPower cloneFrom) : base(cloneFrom) { }
		internal HeroPower(Card card, Dictionary<GameTag, int> tags = null) : base(card, tags) { }

		public override IEnumerable<ICharacter> ValidTargets => GetValidAbilityTargets();

		protected internal override bool MeetsGenericTargetingRequirements(ICharacter target) {
			Minion minion = target as Minion;

			// Spells and hero powers can't target CantBeTargetedByAbilities minions
			return (minion != null && minion.CantBeTargetedByAbilities ? false : base.MeetsGenericTargetingRequirements(target));
		}

		public HeroPower Activate(ICharacter target = null) {
			if (Game.Player1.Choice != null || Game.Player2.Choice != null)
				throw new ChoiceException();

			Target = target;
			try {
				return (HeroPower)Game.RunActionBlock(BlockType.PLAY, this, Actions.UseHeroPower(this), Target);
			}
			// Action was probably cancelled causing an uninitialized ActionResult to be returned
			catch (NullReferenceException) {
				return null;
			}
		}

		public override object Clone() {
			return new HeroPower(this);
		}
	}
}
