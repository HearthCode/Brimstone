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
	public partial interface IPlayable : ICanTarget
	{
		bool IsPlayable { get; }
		IPlayable Play(ICharacter target = null);
	}

	public abstract partial class Playable<T> : CanTarget, IPlayable where T : Entity
	{
		protected Playable(Card card, Dictionary<GameTag, int> tags = null) : base(card, tags) { }
		protected internal Playable(Playable<T> cloneFrom) : base(cloneFrom) { }

		public T GiveTo(Player player)
		{
			Zone = player.Hand;
			return (T) (IEntity) this;
		}

		public virtual bool IsPlayable
		{
			get
			{
				if (Controller != Game.CurrentPlayer)
					return false;

				if (Zone != Controller.Hand)
					return false;

				if (Controller.Choice != null)
					return false;

				if (!Controller.SufficientResources(this))
					return false;

				if (Card.RequiresTarget && ValidTargets.Any())
					return false;

				// TODO: Alot more criteria

				return true;
			}
		}

		// Return IPlayable when calling Play from interface
		IPlayable IPlayable.Play(ICharacter target) { return (IPlayable) Play(target); }

		// Return T when calling Play on concrete class
		public T Play(ICharacter target = null) {
			if (Game.Player1.Choice != null || Game.Player2.Choice != null)
				throw new ChoiceException();

			// TODO: Check card is in player's hand
			Target = target;
			try {
				return (T) Game.RunActionBlock(BlockType.PLAY, this, Actions.Play(this), Target);
			}
			// Action was probably cancelled causing an uninitialized ActionResult to be returned
			catch (NullReferenceException) {
				return default(T);
			}
		}
	}
}
