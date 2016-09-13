using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace Brimstone
{
	public static class Cards
	{
		static Cards()
		{
			data = new CardDefs();
			data.Load();
		}
		private static CardDefs data;
		
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

		public static IEnumerable<Card> All {
			get { return data.Cards.Values; }
		}

		public static int Count {
			get { return data.Cards.Count; }
		}
	}

	public class CardDefs : IEnumerable<Card>
	{
		public Dictionary<string, Card> Cards = new Dictionary<string, Card>();

		public Card this[string cardId] {
			get {
				return Cards[cardId];
			}
		}

		// Card must be collectible to avoid selecting duplicate cards. Use CardID to retrieve adventure cards etc.
		public Card ByName(string cardName)
		{
			var card = Cards.FirstOrDefault(x => x.Value.Name == cardName && x.Value.Collectible);
			return !card.Equals(default(KeyValuePair<string, Card>)) ? card.Value : null;
		}

		public CardDefs() { }

		internal void Load() {
			// Get XML definitions from assembly embedded resource
			var assembly = Assembly.GetExecutingAssembly();
			var def = XDocument.Load(assembly.GetManifestResourceStream("Brimstone.Data.CardDefs.xml"));
			var dbfDef = XDocument.Load(assembly.GetManifestResourceStream("Brimstone.Data.CARD.xml"));

			// Parse XML
			var cards = (from r in def.Descendants("Entity")
						 select new {

							 Id = r.Attribute("CardID").Value,

							 // Unfortunately the file contains some duplicate tags
							 // so we have to make a list first and weed out the unique ones
							 Tags = (from tag in r.Descendants("Tag")
									 select new Tag(
											Name: (GameTag) Enum.Parse(typeof(GameTag), tag.Attribute("enumID").Value),
											Value: (tag.Attribute("value") != null? (Variant) int.Parse(tag.Attribute("value").Value)
													: (tag.Attribute("type").Value == "String"? (Variant) tag.Value
														: (tag.Attribute("type").Value == "LocString"?
															(Variant) tag.Element("enUS").Value : (Variant) 0)))
										)).ToList(),
										
							Requirements = (from req in r.Descendants("PlayRequirement")
												select new {
													Req = (PlayRequirements) Enum.Parse(typeof(PlayRequirements), req.Attribute("reqID").Value),
													Param = (req.Attribute("param").Value != ""? int.Parse(req.Attribute("param").Value) : 0)
												}).ToDictionary(x => x.Req, x => x.Param),

							Entourage = (from ent in r.Descendants("EntourageCard")
											select ent.Attribute("cardID").Value).ToList()
						}).ToList();

			var dbfCards = (from r in dbfDef.Descendants("Record")
							select new {
								AssetId = (from field in r.Descendants("Field") where field.Attribute("column").Value == "ID" select int.Parse(field.Value)).FirstOrDefault(),
								CardId = (from field in r.Descendants("Field") where field.Attribute("column").Value == "NOTE_MINI_GUID" select field.Value).FirstOrDefault(),
								Guid = (from field in r.Descendants("Field") where field.Attribute("column").Value == "LONG_GUID" select field.Value).FirstOrDefault()
							}).ToDictionary(x => x.CardId, x => x);

			// Build card database
			Cards = new Dictionary<string, Card>();

			foreach (var card in cards) {
				// Skip PlaceholderCard etc.
				if (!dbfCards.ContainsKey(card.Id))
					continue;

				var c = new Card() {
					AssetId = dbfCards[card.Id].AssetId,
					Guid = Guid.Parse(dbfCards[card.Id].Guid),
					Id = card.Id,
					Tags = new Dictionary<GameTag, int>(),
					Requirements = card.Requirements
				};
				// Get unique int and bool tags, ignore duplicate and string tags
				foreach (var tag in card.Tags) {
					if (c.Tags.ContainsKey(tag.Name))
						continue;
					if (tag.Value.HasIntValue) {
						c.Tags.Add(tag.Name, tag.Value);
					}
					else if (tag.Value.HasBoolValue) {
						c.Tags.Add(tag.Name, tag.Value ? 1 : 0);
					}
					else if (tag.Value.HasStringValue) {
						if (tag.Name == GameTag.CARDNAME)
							c.Name = tag.Value;
					}
				}
				
				Cards.Add(c.Id, c);
			}

			// Add in placeholder cards
			Cards.Add("Game", new Card {
				AssetId = -1,
				Guid = new Guid("00000000-0000-0000-0000-000000000001"),
				Id = "Game",
				Name = "Game",
				Tags = new Dictionary<GameTag, int> { { GameTag.CARDTYPE, (int)CardType.GAME } },
				Requirements = new Dictionary<PlayRequirements, int>(),
				Behaviour = null
			});
			Cards.Add("Player", new Card {
				AssetId = -2,
				Guid = new Guid("00000000-0000-0000-0000-000000000002"),
				Id = "Player",
				Name = "Player",
				Tags = new Dictionary<GameTag, int> { { GameTag.CARDTYPE, (int)CardType.PLAYER } },
				Requirements = new Dictionary<PlayRequirements, int>(),
				Behaviour = null
			});
			
			foreach (var c in Cards.Values) {
				// Get behaviour script and compile ActionGraph for cards with behaviours
				// TODO: Allow fetch from card name as well as ID
				var b = typeof(BehaviourScripts).GetField(c.Id, BindingFlags.Static | BindingFlags.Public);
				if (b != null) {
					var script = b.GetValue(null) as Behaviour;
					c.Behaviour = CompiledBehaviour.Compile(script);
				}
				else
					c.Behaviour = new CompiledBehaviour();
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
