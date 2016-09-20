using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Brimstone
{
	interface ICardLoader
	{
		List<Card> Load();
	}

	public static class Cards
	{
		private static readonly CardDefinitions data;

		static Cards() {
			// Load cards for selected game
			var gameNamespace = "Brimstone.Games." + Settings.Game;
			var cardLoader = Activator.CreateInstance(Type.GetType(gameNamespace + ".CardLoader")) as ICardLoader;
			var cards = cardLoader.Load();

			// Set as card definitions
			data = new CardDefinitions();
			data.Load(cards, gameNamespace);
		}

		public static Card FromId(string cardId) {
			if (data.Cards.ContainsKey(cardId))
				return data.Cards[cardId];
			return null;
		}

		public static Card FromName(string cardName) {
			return data.ByName(cardName);
		}

		public static Card FromAssetId(int assetId) {
			return data.Cards.Values.FirstOrDefault(x => x.AssetId == assetId);
		}

		public static IEnumerable<Card> All => data.Cards.Values;

		public static int Count => data.Cards.Count;
	}

	internal class CardDefinitions : IEnumerable<Card>
	{
		internal Dictionary<string, Card> Cards { get; private set; }

		internal Card this[string cardId] => Cards[cardId];

		// Card must be collectible to avoid selecting duplicate cards. Use CardID to retrieve adventure cards etc.
		internal Card ByName(string cardName) {
			var card = Cards.FirstOrDefault(x => x.Value.Name == cardName && x.Value.Collectible);
			return !card.Equals(default(KeyValuePair<string, Card>)) ? card.Value : null;
		}

		internal void Load(IEnumerable<Card> cards, string gameNamespace) {
			// Set cards (without behaviours)
			Cards = (from c in cards select new {Key = c.Id, Value = c}).ToDictionary(x => x.Key, x => x.Value);

			// Add in placeholder cards
			Cards.Add("Game", new Card
			{
				AssetId = -1,
				Guid = new Guid("00000000-0000-0000-0000-000000000001"),
				Id = "Game",
				Name = "Game",
				Tags = new Dictionary<GameTag, int> {{GameTag.CARDTYPE, (int) CardType.GAME}},
				Requirements = new Dictionary<PlayRequirements, int>(),
				Behaviour = null
			});
			Cards.Add("Player", new Card
			{
				AssetId = -2,
				Guid = new Guid("00000000-0000-0000-0000-000000000002"),
				Id = "Player",
				Name = "Player",
				Tags = new Dictionary<GameTag, int> {{GameTag.CARDTYPE, (int) CardType.PLAYER}},
				Requirements = new Dictionary<PlayRequirements, int>(),
				Behaviour = null
			});

			// Compile card behaviours
			var behavioursType = Type.GetType(gameNamespace + ".Cards");
			foreach (var c in Cards.Values)
			{
				// Get behaviour script and compile ActionGraph for cards with behaviours
				// TODO: Allow fetch from card name as well as ID
				var b = behavioursType.GetField(c.Id, BindingFlags.Static | BindingFlags.Public);
				if (b != null) {
					var script = b.GetValue(null) as CardBehaviourGraph;
					c.Behaviour = CardBehaviour.FromGraph(script);
				} else
					c.Behaviour = new CardBehaviour();
			}
		}

		public IEnumerator<Card> GetEnumerator() {
			return Cards.Values.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}
	}
}
