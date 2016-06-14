using System.Collections.Generic;
using System.Linq;

namespace Brimstone
{
	public static class Cards
	{
		private static CardDefs data = new CardDefs();
		public static Dictionary<string, Card> Find {
			get {
				return data.Cards;
			}
		}

		public static Card FindByName(string cardName) {
			return data.ByName(cardName);
		}
	}

	public class CardDefs
	{
		public Dictionary<string, Card> Cards = new Dictionary<string, Card>();

		public Card this[string cardId] {
			get {
				return Cards[cardId];
			}
		}

		public Card ByName(string cardName) {
			return Cards.First(x => x.Value.Name == cardName).Value;
		}

		public CardDefs() {
			// Build the card definitions from the 'XML' and the behaviour scripts
			// These will never be modified once created
			Cards = new Dictionary<string, Card> {
				{ "GVG_096", new GVG_096() },
				{ "AT_094", new AT_094 { Behaviour = CardBehaviour.AT_094 } },
				{ "GVG_110t", new GVG_110t { Behaviour = CardBehaviour.GVG_110t } },
				{ "EX1_400", new EX1_400 { Behaviour = CardBehaviour.EX1_400 } },
				{ "Player", new Card { Id = "Player", Name = "Player" } },
				{ "Game", new Card { Id = "Game", Name = "Game" } }
			};
		}
	}

	// Let's pretend this crap is XML or whatever
	public class GVG_096 : Card
	{
		public override string Id { get; set; } = "GVG_096";
		public override string Name { get; set; } = "Piloted Shredder";
		public override Dictionary<GameTag, int> Tags { get; set; } = new Dictionary<GameTag, int> {
			{ GameTag.CARDTYPE, (int) CardType.MINION },
			{ GameTag.HEALTH, 3 }
		};
	}

	public class AT_094 : Card
	{
		public override string Id { get; set; } = "AT_094";
		public override string Name { get; set; } = "Flame Juggler";
		public override Dictionary<GameTag, int> Tags { get; set; } = new Dictionary<GameTag, int> {
			{ GameTag.CARDTYPE, (int) CardType.MINION },
			{ GameTag.HEALTH, 3 }
		};
	}

	public class GVG_110t : Card
	{
		public override string Id { get; set; } = "GVG_110t";
		public override string Name { get; set; } = "Boom Bot";
		public override Dictionary<GameTag, int> Tags { get; set; } = new Dictionary<GameTag, int> {
			{ GameTag.CARDTYPE, (int) CardType.MINION },
			{ GameTag.HEALTH, 1 }
		};
	}

	public class EX1_400 : Card
	{
		public override string Id { get; set; } = "EX1_400";
		public override string Name { get; set; } = "Whirlwind";
		public override Dictionary<GameTag, int> Tags { get; set; } = new Dictionary<GameTag, int> {
			{ GameTag.CARDTYPE, (int) CardType.SPELL },
		};
	}

}