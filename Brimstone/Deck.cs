using System.Collections.Generic;
using System.Linq;

namespace Brimstone
{
	public class Deck : Zone<IPlayable>
	{
		public const int MaxCards = 30;

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
				DebugLog.WriteLine("Adding " + card.Name + " to " + Controller.ShortDescription + "'s deck");
#endif
				Game.Add(Entity.FromCard(card, tags), Controller);
			}
			// Force deck zone contents to update
			Init();
		}

		public void Shuffle() {
#if _DECK_DEBUG
			DebugLog.WriteLine("Shuffling " + Controller.ShortDescription + "'s deck");
#endif
			var possiblePositions = Enumerable.Range(1, Count).ToList();
			foreach (var c in this) {
				int selIndex = RNG.Between(0, possiblePositions.Count - 1);
				// Quicker to just set zone positions than do a ton of same-zone moves
				c[GameTag.ZONE_POSITION] = possiblePositions[selIndex];
				possiblePositions.RemoveAt(selIndex);
			}
			Init();
		}

		public void Fill() {
			// TODO: Add filters later
			var cardsToAdd = MaxCards - Count;
			var fillCards = new List<Card>(cardsToAdd);
#if _DECK_DEBUG
			DebugLog.WriteLine("Adding " + cardsToAdd + " random cards to " + Controller.ShortDescription + "'s deck");
#endif
			while (fillCards.Count < cardsToAdd) {
				// TODO: Change Cards.All to a Linq statement selecting only relevant cards
				var chosenCard = RNG<Card>.Choose(Cards.All);
				if (chosenCard.Collectible && chosenCard.Class == (CardClass)HeroClass && chosenCard.Type != CardType.HERO)
					fillCards.Add(chosenCard);
			}
			Add(fillCards);
		}

		public int Qty(Card card) {
			return this.Select(x => x.Card == card).Count();
		}
	}
}
