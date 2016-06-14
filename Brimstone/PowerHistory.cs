namespace Brimstone
{
	public class PowerAction
	{
		public Entity Entity { get; set; }
	}

	public class TagChange : PowerAction
	{
		public GameTag Key { get; set; }
		public int? Value { get; set; }

		public override string ToString() {
			return "<" + Key.ToString() + ": " + Value + ">, ";
		}
	}
}