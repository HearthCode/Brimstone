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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Brimstone
{
	interface ICardLoader
	{
		IEnumerable<Card> Load();
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

			// Common sets
			// TODO: Add Standard sets
			foreach (var heroClass in Enum.GetValues(typeof(HeroClass)).Cast<HeroClass>())
				Wild.Add(heroClass, All.Where(
					c => c.Collectible && (c.Class == (CardClass)heroClass || c.Class == CardClass.NEUTRAL) && c.Type != CardType.HERO).ToList());
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

		public static Dictionary<HeroClass, List<Card>> Wild { get; } = new Dictionary<HeroClass, List<Card>>();

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
				var b = behavioursType.GetField(c.Id, BindingFlags.Static | BindingFlags.NonPublic);
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
