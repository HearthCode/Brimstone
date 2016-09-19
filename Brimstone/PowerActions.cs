using System;
using System.Collections.Generic;
using Brimstone.Entities;

namespace Brimstone.PowerActions
{
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

		public int Source {
			get { return EntityId; }
		}

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
}
