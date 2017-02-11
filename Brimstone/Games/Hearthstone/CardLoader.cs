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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace Brimstone.Games.Hearthstone
{
	internal class CardLoader : ICardLoader
	{
		public IEnumerable<Card> Load() {
			// Get XML definitions from assembly embedded resource
			var assembly = Assembly.GetExecutingAssembly();
			var def = XDocument.Load(assembly.GetManifestResourceStream("Brimstone.Games.Hearthstone.Data.Cards.xml"));

			// Parse XML
			return (from r in def.Descendants("Entity")
				select new Card {
					Id = r.Attribute("CardID")?.Value,
					AssetId = int.Parse(r.Attribute("AssetID")?.Value ?? "0"),
					Name = r.Element("Name")?.Value,
					Guid = Guid.Parse(r.Element("Guid")?.Value ?? "00000000-0000-0000-0000-000000000000"),

					Tags = (from tag in r.Descendants("Tag")
						select new {
							Name = (GameTag) Enum.Parse(typeof(GameTag), tag.Attribute("enumID").Value),
							Value = int.Parse(tag.Attribute("value").Value)
						}).ToDictionary(x => x.Name, x => x.Value),

					Requirements = (from req in r.Descendants("PlayRequirement")
						select new {
							Req = (PlayRequirements) Enum.Parse(typeof(PlayRequirements), req.Attribute("reqID").Value),
							Param = (req.Attribute("param").Value != "" ? int.Parse(req.Attribute("param").Value) : 0)
						}).ToDictionary(x => x.Req, x => x.Param),
					/*
						Entourage = (from ent in r.Descendants("EntourageCard")
							select ent.Attribute("cardID").Value).ToList()
						*/
					// Skip PlaceholderCard
				}).Where(x => x.AssetId != 0);
		}
	}
}
