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

				if (card[GameTag.CARDTYPE] == (int)CardType.MINION) {
					e = new Minion(Game, (IEntity)Controller, card);
				}
				else if (card[GameTag.CARDTYPE] == (int)CardType.SPELL) {
					e = new Spell(Game, (IEntity)Controller, card);
				}
				// TODO: Weapons

				e[GameTag.ZONE] = (int)Zone.DECK;
				e[GameTag.ZONE_POSITION] = nextPos++;
			}
			// Force deck zone contents to update
			Init();
		}
	}
}