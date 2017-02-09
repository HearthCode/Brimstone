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

using System.Collections;
using System.Collections.Generic;
using Brimstone.Entities;
using Brimstone.Exceptions;

namespace Brimstone
{
	public class Zones : IEnumerable<IZone>
	{
		public Game Game { get; }
		public IZoneController Controller { get; }

		private IZone[] _zones = new IZone[(int) Zone._COUNT];

		public Zones(Game game, IZoneController controller) {
			Game = game;
			Controller = controller;
		}

		public IZone this[Zone z] {
			get {
				if (_zones[(int) z] == null) {
					IZone newZone;

					switch (z) {
						case Zone.GRAVEYARD:
							newZone = new Zone<ICharacter>(Game, Controller, z);
							break;
						case Zone.HAND:
						case Zone.DECK:
						case Zone.SETASIDE:
							newZone = new Zone<IPlayable>(Game, Controller, z);
							break;
						case Zone.PLAY:
							newZone = new Zone<Minion>(Game, Controller, z);
							break;
						case Zone.SECRET:
							// TODO: Change to Secret later
							newZone = new Zone<Spell>(Game, Controller, z);
							break;
						case Zone.INVALID:
							newZone = new Zone<IEntity>(Game, Controller, z);
							break;
						default:
							throw new ZoneException("No such zone type when creating zone: " + z);
					}
					_zones[(int) z] = newZone;
				}
				return _zones[(int) z];
			}
			// For decks
			set { _zones[(int) z] = value; }
		}

		public IEnumerator<IZone> GetEnumerator() {
			return ((IEnumerable<IZone>) _zones).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return _zones.GetEnumerator();
		}
	}
}
