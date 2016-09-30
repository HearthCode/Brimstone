using System;
using System.Collections.Generic;
using System.Linq;
using Brimstone.Entities;

namespace Brimstone
{
	public class Deck : Zone<IPlayable>
	{
		public int StartingCards { get; set; } = 30;

		public HeroClass HeroClass { get; }

		public Deck(Game game, HeroClass hero, Player controller) : base(game, controller, Zone.DECK) {
			HeroClass = hero;
		}

		public void Add(Card card) {
			Add(new List<Card> { card });
		}

		public void Add(List<Card> cards) {
			int nextPos = Count + 1;

			foreach (var card in cards) {
				var tags = new Dictionary<GameTag, int> {
					{ GameTag.ZONE, (int)Zone.DECK },
					{ GameTag.ZONE_POSITION, nextPos++ }
				};
#if _DECK_DEBUG
				DebugLog.WriteLine("Game " + Game.GameId + ": Adding " + card.Name + " to " + Controller.ShortDescription + "'s deck");
#endif
				Game.Add(Entity.FromCard(card, tags), (Player)Controller);
			}
			// Force deck zone contents to update
			SetDirty();
		}

		public void Shuffle() {
#if _DECK_DEBUG
			DebugLog.WriteLine("Game " + Game.GameId + ": Shuffling " + Controller.ShortDescription + "'s deck");
#endif
			var possiblePositions = Enumerable.Range(1, Count).ToList();
			foreach (var c in this) {
				int selIndex = RNG.Between(0, possiblePositions.Count - 1);
				// Quicker to just set zone positions than do a ton of same-zone moves
				c[GameTag.ZONE_POSITION] = possiblePositions[selIndex];
				possiblePositions.RemoveAt(selIndex);
			}
			SetDirty();
		}

		public void Fill() {
			// TODO: Add filter/availableCards arguments later
			var deck = Entities.Select(x => x.Card).ToList();
			var cardsAlreadyInDeck = deck.Count;
			var cardsToAdd = StartingCards - cardsAlreadyInDeck;
			var availableCards = Cards.Wild[HeroClass];
#if _DECK_DEBUG
			DebugLog.WriteLine("Game " + Game.GameId + ": Adding " + cardsToAdd + " random cards to " + Controller.ShortDescription + "'s deck");
#endif
			while (cardsToAdd > 0) {
				var card = RNG<Card>.Choose(availableCards);
				if (deck.Count(c => c == card) < card.MaxAllowedInDeck) {
					deck.Add(card);
					cardsToAdd--;
				}
			}
			Add(deck.Skip(cardsAlreadyInDeck).ToList());
		}

		public int Qty(Card card) {
			return this.Select(x => x.Card == card).Count();
		}
	}
}
