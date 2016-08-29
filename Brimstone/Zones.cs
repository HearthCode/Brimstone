using System;
using System.Collections;
using System.Collections.Generic;

namespace Brimstone
{
	public interface IZoneController : IEntity
	{
		Zones Zones { get; }

		Deck Deck { get; set; }
		Zone<IPlayable> Hand { get; }
		Zone<Minion> Board { get; }
		Zone<ICharacter> Graveyard { get; }
		// TODO: Change to Secret later
		Zone<Spell> Secrets { get; }
		Zone<IPlayable> Setaside { get; }
	}

	public class Zones : IEnumerable<IZone>
	{
		public Game Game { get; }
		public IZoneController Controller { get; }

		private IZone[] _zones = new IZone[(int)Zone._COUNT];

		public Zones(Game game, IZoneController controller) {
			Game = game;
			Controller = controller;
		}

		public IZone this[Zone z] {
			get {
				if (_zones[(int)z] == null) {
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
					_zones[(int)z] = newZone;
				}
				return _zones[(int)z];
			}
			// For decks
			set {
				_zones[(int)z] = value;
			}
		}

		public IEnumerator<IZone> GetEnumerator() {
			return ((IEnumerable<IZone>)_zones).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return _zones.GetEnumerator();
		}
	}
}
