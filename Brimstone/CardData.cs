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
		private static CardDefs data = new CardDefs();
		
		public static Card FromId(string cardId) {
			return data.Cards[cardId];
		}

		public static Card FromName(string cardName) {
			return data.ByName(cardName);
		}

		public static List<Card> All {
			get { return data.Cards.Values.ToList(); }
		}

		public static int Count {
			get { return data.Cards.Count; }
		}

		public static Card TheCoin {
			get { return data.Cards["GAME_005"]; }
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

		public Card ByName(string cardName) {
			return Cards.First(x => x.Value.Name == cardName).Value;
		}

		public CardDefs() {
			// Get XML definitions from assembly embedded resource
			XDocument def;
			var assembly = Assembly.GetExecutingAssembly();
			var resourceName = "Brimstone.Data.CardDefs.xml";
			using (Stream stream = assembly.GetManifestResourceStream(resourceName))
				def = XDocument.Load(stream);

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

			// Build card database
			Cards = new Dictionary<string, Card>();

			foreach (var card in cards) {
				var c = new Card() {
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
				// Get behaviour script and compile ActionGraph for cards with behaviours
				var b = typeof(CardBehaviour).GetField(c.Id, BindingFlags.Static | BindingFlags.Public);
				if (b != null) {
					var script = b.GetValue(null) as Behaviour;
					c.Behaviour = CompiledBehaviour.Compile(script);
				}
				else
					c.Behaviour = new CompiledBehaviour();
				Cards.Add(c.Id, c);
			}

			// Add in placeholder cards
			Cards.Add("Game", new Card {
				Id = "Game",
				Name = "Game",
				Tags = new Dictionary<GameTag, int> { { GameTag.CARDTYPE, (int)CardType.GAME } },
				Requirements = new Dictionary<PlayRequirements, int>(),
				Behaviour = null
			});
			Cards.Add("Player", new Card {
				Id = "Player",
				Name = "Player",
				Tags = new Dictionary<GameTag, int> { { GameTag.CARDTYPE, (int)CardType.PLAYER } },
				Requirements = new Dictionary<PlayRequirements, int>(),
				Behaviour = null
			});
		}

		public IEnumerator<Card> GetEnumerator() {
			return Cards.Values.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}
	}
}