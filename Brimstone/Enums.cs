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
		CONTROLLER,
		_COUNT
	}

	public enum Zone
	{
		INVALID = 0,
		PLAY = 1,
		DECK = 2,
		HAND = 3,
		GRAVEYARD = 4,
		REMOVEDFROMGAME = 5,
		SETASIDE = 6,
		SECRET = 7,
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