namespace Brimstone.Entities
{
	public interface IZoneController : IEntity
	{
		Zones Zones { get; }

		Deck Deck { get; set; }
		Zone<IPlayable> Hand { get; }
		Zone<Minion> Board { get; }
		Zone<ICharacter> Graveyard { get; }
		// TODO: Change to Secret later
		Zone<Spell> Secrets { get; }
		Zone<IPlayable> Setaside { get; }
	}
}
