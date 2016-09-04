using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Brimstone
{
	// TODO: Implement rewind stack

	public abstract class PowerAction
	{
		public int EntityId { get; protected set; }

		public PowerAction(int eId) {
			EntityId = eId;
		}

		public PowerAction(IEntity e) {
			EntityId = e.Id;
		}

		public override string ToString() {
			return "Entity: " + EntityId;
		}
	}

	// TODO: Implement IEquatable<T> on BlockStart and BlockEnd
	public class BlockStart : PowerAction
	{
		public BlockType Type { get; }
		public int Source { get { return EntityId; } }
		public int Target { get; }
		public int Index { get; }

		public BlockStart(BlockType type, int source, int target = 0, int index = -1) : base(source) {
			Type = type;
			Target = target;
			Index = index;
		}

		public BlockStart(BlockType type, IEntity source, IEntity target = null, int index = -1) : base(source) {
			Type = type;
			Target = target?.Id ?? 0;
			Index = index;
		}

		public override string ToString() {
			return "[Start] " + Type + ": Source = " + EntityId + ", Target = " + Target + ", Index = " + Index;
		}
	}

	public class BlockEnd : PowerAction
	{
		public BlockType Type { get; }

		public BlockEnd(BlockType type) : base(-1) {
			Type = type;
		}

		public override string ToString() {
			return "[End] " + Type;
		}
	}

	public class TagChange : PowerAction, IEquatable<TagChange>
	{
		public Tag Tag { get; }

		public TagChange(int entityId, Tag t) : base(entityId) {
			Tag = t;
		}

		public TagChange(IEntity e, Tag t) : base(e) {
			if (t.Filtered(e) != null) {
				Tag = t;
			}
			else
				EntityId = 0;
		}

		public TagChange(IEntity e, GameTag name, int value) : base(e) {
			var t = new Tag(name, value);
			if (t.Filtered(e) != null) {
				Tag = t;
			}
			else
				EntityId = 0;
		}

		public TagChange(int entityId, GameTag name, int value) : base(entityId) {
			Tag = new Tag(name, value);
		}

		public override string ToString() {
			return "[Tag] Entity " + EntityId + ": " + Tag;
		}

		public static bool operator ==(TagChange x, TagChange y) {
			if (ReferenceEquals(x, null))
				return false;
			return x.Equals(y);
		}

		public static bool operator !=(TagChange x, TagChange y) {
			return !(x == y);
		}

		public override bool Equals(object o) {
			if (!(o is TagChange))
				return false;
			return Equals((TagChange)o);
		}

		public bool Equals(TagChange o) {
			if (ReferenceEquals(o, null))
				return false;
			if (ReferenceEquals(this, o))
				return true;
			return EntityId == o.EntityId && Tag == o.Tag;
		}

		public override int GetHashCode() {
			return ((17 * 31 + EntityId) * 31 + (int)Tag.Name) * 31 + Tag.Value;
		}
	}

	public class CreateEntity : PowerAction, IEquatable<CreateEntity>
	{
		// TODO: Store card ID so we can display friendly entity name

		public Dictionary<GameTag, int> Tags { get; }

		public CreateEntity(IEntity e) : base(e) {
			Tags = new Dictionary<GameTag, int>();
			// Make sure we copy the tags, not the references!
			foreach (var tag in e) {
				// Filtered tags only
				if (new Tag(tag.Key, tag.Value).Filtered(e) != null)
					Tags.Add(tag.Key, tag.Value);
			}
		}

		public override string ToString() {
			string s = "[Create] " + base.ToString();
			foreach (var t in Tags) {
				s += "\n    " + new Tag(t.Key, t.Value);
			}
			return s;
		}

		public static bool operator ==(CreateEntity x, CreateEntity y) {
			if (ReferenceEquals(x, null))
				return false;
			return x.Equals(y);
		}

		public static bool operator !=(CreateEntity x, CreateEntity y) {
			return !(x == y);
		}

		public override bool Equals(object o) {
			if (!(o is CreateEntity))
				return false;
			return Equals((CreateEntity)o);
		}

		public bool Equals(CreateEntity o) {
			if (ReferenceEquals(o, null))
				return false;
			if (ReferenceEquals(this, o))
				return true;
			// Must be same entity ID - we might want to change this later?
			if (EntityId != o.EntityId)
				return false;
			// Same number of tags in each entity
			if (Tags.Count != o.Tags.Count)
				return false;
			// Same tags must be present
			foreach (var tagName in Tags.Keys)
				if (!o.Tags.ContainsKey(tagName))
					return false;
			// Tags must have same tag values
			foreach (var tagName in Tags.Keys)
				if (Tags[tagName] != o.Tags[tagName])
					return false;
			return true;
		}

		public override int GetHashCode() {
			int hash = (17 * 31 + EntityId);
			foreach (var tag in Tags)
				hash = (hash * 31 + (int)tag.Key) * 31 + tag.Value;
			return hash;
		}
	}

	public class PowerActionEventArgs : EventArgs
	{
		public Game Game;
		public PowerAction Action;

		public PowerActionEventArgs(Game g, PowerAction a) {
			Game = g;
			Action = a;
		}
	}

	public class CompareEntityAndTagName : IEqualityComparer<TagChange>
	{
		// Used when adding to and fetching from HashSet, and testing for equality
		public bool Equals(TagChange x, TagChange y) {
			return x.EntityId == y.EntityId && x.Tag.Name == y.Tag.Name;
		}

		public int GetHashCode(TagChange obj) {
			int hash = 17 * 31 + obj.EntityId;
			return hash * 31 + (int)obj.Tag.Name;
		}
	}

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
				OrderedHash = OrderedHash * 31 + hash;
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
			var collapsedDelta = new HashSet<TagChange>(new CompareEntityAndTagName());
			foreach (var entry in delta.Reverse()) {
				// TODO: All the other PowerAction types
				// Ignore BlockStart and BlockEnd
				if (entry is CreateEntity) {
					foreach (var tag in ((CreateEntity)entry).Tags)
						collapsedDelta.Add(new TagChange(entry.EntityId, tag.Key, tag.Value));
				}
				else if (entry is TagChange) {
					collapsedDelta.Add((TagChange)entry);
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
				foreach (var pair in deltaA.Zip(deltaB, (x, y) => new { A = x, B = y }))
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
					entitiesInHand = cDeltaA.Where(x => x.Tag.Name == GameTag.ZONE && x.Tag.Value == (int)Zone.HAND).Select(x => x.EntityId);
					cDeltaA.RemoveWhere(x => x.Tag.Name == GameTag.ZONE_POSITION && entitiesInHand.Contains(x.EntityId));
					entitiesInHand = cDeltaB.Where(x => x.Tag.Name == GameTag.ZONE && x.Tag.Value == (int)Zone.HAND).Select(x => x.EntityId);
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