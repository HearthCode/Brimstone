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
using Brimstone.Entities;
using Brimstone.PowerActions;

namespace Brimstone
{
	public class PowerActionEventArgs : EventArgs
	{
		public Game Game;
		public PowerAction Action;

		public PowerActionEventArgs(Game g, PowerAction a) {
			Game = g;
			Action = a;
		}
	}

	// TODO: Implement rewind stack
	public class PowerHistory : ListTree<PowerAction>
	{
		public Game Game { get; private set; }

		public int OrderedHash { get; private set; }
		public int UnorderedHash { get; private set; }

		public event EventHandler<PowerActionEventArgs> OnPowerAction;

		public PowerHistory(Game game, Game parent = null) : base(parent?.PowerHistory) {
			Game = game;
			OrderedHash = parent?.PowerHistory.OrderedHash ?? 17;
			UnorderedHash = parent?.PowerHistory.UnorderedHash ?? 0;

			// Subscribe to game events
			Game.OnEntityCreated += (g, e) => Add(new CreateEntity(e));
			Game.OnEntityChanged += (g, e, t, o, n) => Add(new TagChange(e, t, n));
		}

		public void Add(PowerAction a) {
			// Ignore PowerHistory for untracked games
			if (Game == null)
				return;

			// Tag changes indicate they are filtered out by setting entity ID to zero
			if (a.EntityId != 0) {
				AddItem(a);

				var hash = a.GetHashCode();
				OrderedHash = OrderedHash*31 + hash;
				UnorderedHash += hash;
			}
			OnPowerAction?.Invoke(this, new PowerActionEventArgs(Game, a));
		}

		// Return the PowerHistory delta from the point where the specified game was created
		public IEnumerable<PowerAction> DeltaSince(Game game) {
			return DeltaSince(game.PowerHistory);
		}

		// Crunch changes to get only latest changed tags for each changed entity
		public HashSet<TagChange> CrunchedDelta(IEnumerable<PowerAction> delta) {
			var collapsedDelta = new HashSet<TagChange>(new EntityAndTagNameComparer());
			foreach (var entry in delta.Reverse()) {
				// TODO: All the other PowerAction types
				// Ignore BlockStart and BlockEnd
				if (entry is CreateEntity) {
					foreach (var tag in ((CreateEntity) entry).Tags)
						collapsedDelta.Add(new TagChange(entry.EntityId, tag.Key, tag.Value));
				}
				else if (entry is TagChange) {
					collapsedDelta.Add((TagChange) entry);
				}
			}
			// Use the default equality comparer for the retuend HashSet
			return new HashSet<TagChange>(collapsedDelta);
		}

		// Compare two PowerHistory logs to see if they are functionally equivalent
		public bool EquivalentTo(PowerHistory History, bool Ordered = false, bool IgnoreHandPosition = true) {
			if (ReferenceEquals(History, null))
				return false;

			// Same log?
			if (ReferenceEquals(this, History))
				return true;

			// Same game?
			if (ReferenceEquals(Game, History.Game))
				return true;

			// Get LCA of both logs
			var lca = LowestCommonAncestor(History);
			// TODO: Delta caching

			// Calculate deltas from LCA to leaf
			var deltaA = DeltaSince(lca);
			var deltaB = History.DeltaSince(lca);

			// TODO: Naive equivalence comparison
			// TODO: Local equivalence if we know in advance both games have the same immediate and unchanged parent
			// TODO: Ignore entity IDs if all other tags same
			// TODO: Ignore board ordering if all other tags same
			// TODO: Tag exclusion filters

			// Pure equality
			if (Ordered) {
				foreach (var pair in deltaA.Zip(deltaB, (x, y) => new {A = x, B = y}))
					// Cannot use == operator because it is not overridden in base PowerAction
					// and will do reference equality only. Use IEquatable<T> instead.
					if (!pair.A.Equals(pair.B))
						return false;
				return true;
			}

			// Equality ignoring tag order
			else {
				// Crunch
				var cDeltaA = CrunchedDelta(deltaA);
				var cDeltaB = CrunchedDelta(deltaB);

				// TODO: What happens when an entity moves from HAND to PLAY without changing its zone position? Bug or not?

				// Remove ZONE_POSITION from entities in hand if we don't care about them
				if (IgnoreHandPosition) {
					IEnumerable<int> entitiesInHand;
					entitiesInHand =
						cDeltaA.Where(x => x.Tag.Name == GameTag.ZONE && x.Tag.Value == (int) Zone.HAND).Select(x => x.EntityId);
					cDeltaA.RemoveWhere(x => x.Tag.Name == GameTag.ZONE_POSITION && entitiesInHand.Contains(x.EntityId));
					entitiesInHand =
						cDeltaB.Where(x => x.Tag.Name == GameTag.ZONE && x.Tag.Value == (int) Zone.HAND).Select(x => x.EntityId);
					cDeltaB.RemoveWhere(x => x.Tag.Name == GameTag.ZONE_POSITION && entitiesInHand.Contains(x.EntityId));
				}

				// Number of changed entities must be same
				if (cDeltaA.Count != cDeltaB.Count)
					return false;
				return cDeltaA.SetEquals(cDeltaB);
			}
		}

		// TODO: PowerHistory views for each player
	}
}
