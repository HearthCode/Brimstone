﻿/*
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
using Brimstone.Exceptions;

namespace Brimstone.Entities
{
	public partial class Player : Entity, IZoneController {
		public string FriendlyName { get; }

		public Deck Deck { get { return (Deck)Zones[Brimstone.Zone.DECK]; } set { Zones[Brimstone.Zone.DECK] = value; } }
		public Zone<IPlayable> Hand { get { return (Zone<IPlayable>) Zones[Brimstone.Zone.HAND]; } }
		public Zone<Minion> Board { get { return (Zone<Minion>) Zones[Brimstone.Zone.PLAY]; } }
		public Zone<ICharacter> Graveyard { get { return (Zone<ICharacter>) Zones[Brimstone.Zone.GRAVEYARD]; } }
		public Zone<Spell> Secrets { get { return (Zone<Spell>) Zones[Brimstone.Zone.SECRET]; } }
		public Zone<IPlayable> Setaside { get { return null; } }
		public Zones Zones { get; private set; }
		public HeroClass HeroClass { get; }

		public int MaxHandSize { get; set; } = 10;
		public bool DisableFatigue { get; set; }

		public Choice Choice { get; set; }

		internal Player(Player cloneFrom) : base(cloneFrom) {
			FriendlyName = cloneFrom.FriendlyName;
			HeroClass = cloneFrom.HeroClass;
			MaxHandSize = cloneFrom.MaxHandSize;
			DisableFatigue = cloneFrom.DisableFatigue;
			// TODO: Shallow clone choices
			// TODO: Update choices to point to new game entities
		}

		public Player(HeroClass hero, string name, int playerId, int teamId = 0) : base(Cards.FromId("Player"),
			new Dictionary<GameTag, int> {
				{ GameTag.MAXHANDSIZE, 10 },
				{ GameTag.MAXRESOURCES, 10 },
				{ GameTag.PLAYER_ID, playerId },
				{ GameTag.TEAM_ID, (teamId != 0? teamId : playerId) },
				{ GameTag.STARTHANDSIZE, 4 }
			}) {
			HeroClass = hero;
			FriendlyName = name;
		}

		public override Game Game {
			get {
				return base.Game;
			}
			set {
				base.Game = value;

				// Create zones
				Zones = new Zones(Game, this);
			}
		}

		public int RemainingMana => (BaseMana + TemporaryMana) - (UsedMana + Overload);

		public bool SufficientResources(IEntity e) => RemainingMana >= e.Cost;

		// All the entities that we can potentially play or attack with when it's our turn
		public IEnumerable<IEntity> LiveEntities => Hand.Concat(Board).Concat(new List<IEntity> {Hero, Hero.Power});

		// TODO: Cache options
		public IEnumerable<Option> Options
		{
			get
			{
				foreach (var e in Hand)
					if (e.IsPlayable)
						yield return new Option {Source = e, Targets = e.ValidTargets};

				foreach (var e in Board)
					if (e.CanAttack)
						yield return new Option {Source = e, Targets = e.ValidTargets};

				if (Hero.CanAttack)
					yield return new Option {Source = Hero, Targets = Hero.ValidTargets};

				if (!Hero.Power.IsExhausted)
					yield return new Option {Source = Hero.Power, Targets = Hero.Power.ValidTargets};
			}
		}

		public void Start(bool Shuffle = true) {
			// Shuffle deck
			if (Shuffle)
				Deck.Shuffle();

			// Add player to board
			Zone = Board;

			// Create hero entity
			// TODO: Add Start() parameters for non-default hero skins
			Hero = new Hero(DefaultHero.For(HeroClass)) {Zone = Board};
			Hero.Start();
		}

		public void StartMulligan() {
			Game.Action(this, Actions.MulliganChoice(this));
		}
			
		public IPlayable Give(Card card) {
			if (Game.Player1.Choice != null || Game.Player2.Choice != null)
				throw new ChoiceException();

			return (IPlayable)(Entity) Game.Action(Game, Actions.Give(this, card));
		}

		public IPlayable Draw() {
			if (Game.Player1.Choice != null || Game.Player2.Choice != null)
				throw new ChoiceException();

			return (IPlayable) (Entity) Game.Action(Game, Actions.Draw(this));
		}

		public void Draw(ActionGraph qty) {
			if (Game.Player1.Choice != null || Game.Player2.Choice != null)
				throw new ChoiceException();

			Game.Action(this, Actions.Draw(this) * qty);
		}

		public void Concede() {
			Game.Action(this, Actions.Concede(this));
		}

		// For internal use only (by Actions)
		internal void PayCost(IEntity source) {
			// Pay casting cost
			// TODO: Cho'gall
			var cost = source.Cost;
			var tempUsed = Math.Min(TemporaryMana, cost);
			TemporaryMana -= tempUsed;
			UsedMana += cost - tempUsed;
			TotalManaSpentThisGame += cost;
		}

		public override object Clone() {
			return new Player(this);
		}
	}
}