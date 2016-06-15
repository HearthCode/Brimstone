namespace Brimstone
{
	public enum GameTag
	{
		ZONE,
		ZONE_POSITION,
		ENTITY_ID,
		DAMAGE,
		HEALTH,
		CARDTYPE,
		STEP,
		_COUNT
	}

	public enum Zone
	{
		PLAY = 1,
		HAND = 3,
		GRAVEYARD = 4,
		_COUNT
	}

	public enum CardType
	{
		GAME = 1,
		PLAYER = 2,
		HERO = 3,
		MINION = 4,
		SPELL = 5,
	}

	public enum Step
	{
		MAIN_ACTION,
	}
}