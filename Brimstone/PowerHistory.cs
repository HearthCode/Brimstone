using System;
using System.Collections;
using System.Collections.Generic;

namespace Brimstone
{
	public abstract class PowerAction
	{
		public int EntityId { get; set; }

		public PowerAction(IEntity e) {
			EntityId = e.Id;
		}

		public override string ToString() {
			return "Entity: " + EntityId;
		}
	}

	public class TagChange : PowerAction
	{
		public Tag Tag { get; }

		public TagChange(IEntity e, Tag t) : base(e) {
			Tag = t;
		}

		public override string ToString() {
			return "[Tag] Entity " + EntityId + ": " + Tag;
		}
	}

	public class CreateEntity : PowerAction
	{
		public Dictionary<GameTag, int?> Tags { get; set; }

		public CreateEntity(IEntity e) : base(e) {
			// Make sure we copy the tags, not the references!
			Tags = e.CopyTags();
		}

		public override string ToString() {
			string s = "[Create] " + base.ToString();
			foreach (var t in Tags) {
				s += "\n    " + new Tag(t.Key, t.Value);
			}
			return s;
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

	public class PowerHistory : IEnumerable<PowerAction>
	{
		public Game Game { get; private set; }
		public List<PowerAction> Log { get; } = new List<PowerAction>();

		public event EventHandler<PowerActionEventArgs> OnPowerAction;

		public void Attach(Game game) {
			Game = game;
		}
		public void Detach() {
			Game = null;
		}

		public void Add(PowerAction a) {
			// Ignore PowerHistory for untracked games
			if (Game == null)
				return;

			Log.Add(a);

			if (OnPowerAction != null)
				OnPowerAction(this, new PowerActionEventArgs(Game, a));
		}

		public IEnumerator<PowerAction> GetEnumerator() {
			return Log.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}

		public override string ToString() {
			string ph = string.Empty;
			foreach (var p in Log)
				ph += p.ToString() + "\n";
			return ph;
		}
	}
}