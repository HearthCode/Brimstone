using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace Brimstone.Games.Hearthstone
{
	internal class CardLoader : ICardLoader
	{
		public List<Card> Load()
		{
			// Get XML definitions from assembly embedded resource
			var assembly = Assembly.GetExecutingAssembly();
			var def = XDocument.Load(assembly.GetManifestResourceStream("Brimstone.Games.Hearthstone.Data.CardDefs.xml"));
			var dbfDef = XDocument.Load(assembly.GetManifestResourceStream("Brimstone.Games.Hearthstone.Data.CARD.xml"));

			// Parse XML
			var cards = (from r in def.Descendants("Entity")
				select new
				{
					Id = r.Attribute("CardID").Value,

					// Unfortunately the file contains some duplicate tags
					// so we have to make a list first and weed out the unique ones
					Tags = (from tag in r.Descendants("Tag")
						select new Tag(
							Name: (GameTag) Enum.Parse(typeof(GameTag), tag.Attribute("enumID").Value),
							Value: (tag.Attribute("value") != null
								? (TagValue) int.Parse(tag.Attribute("value").Value)
								: (tag.Attribute("type").Value == "String"
									? (TagValue) tag.Value
									: (tag.Attribute("type").Value == "LocString"
										? (TagValue) tag.Element("enUS").Value
										: (TagValue) 0)))
							)).ToList(),

					Requirements = (from req in r.Descendants("PlayRequirement")
						select new
						{
							Req = (PlayRequirements) Enum.Parse(typeof(PlayRequirements), req.Attribute("reqID").Value),
							Param = (req.Attribute("param").Value != "" ? int.Parse(req.Attribute("param").Value) : 0)
						}).ToDictionary(x => x.Req, x => x.Param),

					Entourage = (from ent in r.Descendants("EntourageCard")
						select ent.Attribute("cardID").Value).ToList()
				}).ToList();

			var dbfCards = (from r in dbfDef.Descendants("Record")
				select new
				{
					AssetId =
						(from field in r.Descendants("Field") where field.Attribute("column").Value == "ID" select int.Parse(field.Value))
							.FirstOrDefault(),
					CardId =
						(from field in r.Descendants("Field") where field.Attribute("column").Value == "NOTE_MINI_GUID" select field.Value)
							.FirstOrDefault(),
					Guid =
						(from field in r.Descendants("Field") where field.Attribute("column").Value == "LONG_GUID" select field.Value)
							.FirstOrDefault()
				}).ToDictionary(x => x.CardId, x => x);

			// Build card database
			var Cards = new List<Card>();

			foreach (var card in cards)
			{
				// Skip PlaceholderCard etc.
				if (!dbfCards.ContainsKey(card.Id))
					continue;

				var c = new Card()
				{
					AssetId = dbfCards[card.Id].AssetId,
					Guid = Guid.Parse(dbfCards[card.Id].Guid),
					Id = card.Id,
					Tags = new Dictionary<GameTag, int>(),
					Requirements = card.Requirements
				};
				// Get unique int and bool tags, ignore duplicate and string tags
				foreach (var tag in card.Tags)
				{
					if (c.Tags.ContainsKey(tag.Name))
						continue;
					if (tag.Value.HasIntValue)
					{
						c.Tags.Add(tag.Name, tag.Value);
					}
					else if (tag.Value.HasBoolValue)
					{
						c.Tags.Add(tag.Name, tag.Value ? 1 : 0);
					}
					else if (tag.Value.HasStringValue)
					{
						if (tag.Name == GameTag.CARDNAME)
							c.Name = tag.Value;
					}
				}
				Cards.Add(c);
			}
			return Cards;
		}
	}
}
