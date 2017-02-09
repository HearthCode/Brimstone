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

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;

namespace Brimstone
{
	internal class EntityData : IEnumerable<KeyValuePair<GameTag, int>>
	{
		public int Id { get; internal set; }
		public Card Card { get; }
		public Dictionary<GameTag, int> Tags { get; }

		public int this[GameTag t] {
			get {
				// Use the entity tag if available, otherwise the card tag
				if (Tags.ContainsKey(t))
					return Tags[t];
				if (Card.Tags.ContainsKey(t))
					return Card[t];
				return 0;
			}
			set {
				if (Settings.ParallelClone)
					lock (Tags) {
						Tags[t] = value;
					}
				else {
					Tags[t] = value;
				}
			}
		}

		internal EntityData(Card card, Dictionary<GameTag, int> tags = null) {
			Card = card;
			if (tags != null)
				Tags = tags;
			else
				Tags = new Dictionary<GameTag, int>((int)GameTag._COUNT);
		}

		// Cloning copy constructor
		internal EntityData(EntityData cloneFrom) {
			Id = cloneFrom.Id;
			Card = cloneFrom.Card;
			if (Settings.ParallelClone)
				lock (cloneFrom.Tags)
					Tags = new Dictionary<GameTag, int>(cloneFrom.Tags);
			else
				Tags = new Dictionary<GameTag, int>(cloneFrom.Tags);
		}

		public string ShortDescription => Card.Name + " [" + Id + "]";

		public override string ToString() {
			string s = Tags.Aggregate(Card.Name + " - ", (current, tag) => current + (new Tag(tag.Key, tag.Value) + ", "));
			return s.Substring(0, s.Length - 2);
		}

		public IEnumerator<KeyValuePair<GameTag, int>> GetEnumerator() {
			var allTags = new Dictionary<GameTag, int>(Card.Tags);

			// Entity ID
			allTags.Add(GameTag.ENTITY_ID, Id);

			// Entity tags override card tags
			foreach (var tag in Tags)
				allTags[tag.Key] = tag.Value;

			return allTags.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}
	}
}
