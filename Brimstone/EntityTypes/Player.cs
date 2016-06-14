using System.Collections.Generic;

namespace Brimstone
{
	public class Player : Entity
	{
		public int Health { get; private set; } = 30;
		public List<IPlayable> ZoneHand { get; private set; } = new List<IPlayable>();
		public List<IMinion> ZonePlay { get; private set; } = new List<IMinion>();

		public Player(Game game, Dictionary<GameTag, int?> tags = null) : base(game, Cards.Find["Player"], tags) { }

		public IPlayable Give(Card card) {
			if (card[GameTag.CARDTYPE] == (int)CardType.MINION) {
				var minion = new Minion(Game, card);
				ZoneHand.Add(minion);
				minion[GameTag.ZONE] = (int)Zone.HAND;
				minion[GameTag.ZONE_POSITION] = ZoneHand.Count + 1;
				return minion;
			}
			return null;
		}

		public override string ToString() {
			return Card.Id;
		}

		protected override BaseEntity OnClone() {
			return new Player(Game, Tags);
		}
		public override object Clone() {
			Player clone = (Player)base.Clone();
			clone.Health = Health;
			clone.ZoneHand = new List<IPlayable>();
			clone.ZonePlay = new List<IMinion>();
			foreach (var e in ZoneHand)
				clone.ZoneHand.Add(e.Clone() as IPlayable);
			foreach (var e in ZonePlay)
				clone.ZonePlay.Add(e.Clone() as IMinion);
			return clone;
		}
	}
}