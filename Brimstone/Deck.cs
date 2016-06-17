using System.Collections.Generic;

namespace Brimstone
{
	public class Deck : ZoneEntities
	{
		public Deck(Game game, IZones controller) : base(game, controller, Zone.DECK) { }

		public void Add(Card card) {
			Add(new List<Card> { card });
		}

		public void Add(List<Card> cards) {
			int nextPos = Count + 1;

			foreach (var card in cards) {
				IEntity e = null;

				var tags = new Dictionary<GameTag, int> {
					{ GameTag.ZONE, (int)Zone.DECK },
					{ GameTag.ZONE_POSITION, nextPos++ }
				};

				if (card[GameTag.CARDTYPE] == (int)CardType.MINION) {
					e = new Minion(Game, (IEntity)Controller, card, tags);
				}
				else if (card[GameTag.CARDTYPE] == (int)CardType.SPELL) {
					e = new Spell(Game, (IEntity)Controller, card, tags);
				}
				// TODO: Weapons
			}
			// Force deck zone contents to update
			Init();
		}
	}
}