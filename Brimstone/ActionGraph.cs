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
using Brimstone.QueueActions;
using Brimstone.Entities;

namespace Brimstone
{
	public class ActionGraph
	{
		public List<QueueAction> Graph { get; private set; }

		public ActionGraph(QueueAction q) {
			Graph = new List<QueueAction>() { q };
		}

		public ActionGraph(ActionGraph g) {
			Graph = new List<QueueAction>(g.Graph);
		}

		// Convert single QueueAction to ActionGraph
		public static implicit operator ActionGraph(QueueAction q) {
			return new ActionGraph(q);
		}

		public ActionGraph Then(ActionGraph act) {
			Graph.AddRange(act.Graph);
			return this;
		}

		public ActionGraph Repeat(ActionGraph qty) {
			var repeatAction = new Repeat { Actions = new ActionGraph(this), Args = { qty } };
			Graph = new List<QueueAction>() { repeatAction };
			return this;
		}

		// Convert values to actions
		public static implicit operator ActionGraph(int x) {
			return new FixedNumber { Num = x };
		}
		public static implicit operator ActionGraph(Card x) {
			return new FixedCard { Card = x };
		}
		public static implicit operator ActionGraph(string x) {
			return new FixedCard { Card = x };
		}
		public static implicit operator ActionGraph(Entity x) {
			return new LazyEntity { EntityId = x.Id };
		}
		public static implicit operator ActionGraph(Action<IEntity> x) {
			return new Func { F = x };
		}
		public static implicit operator ActionGraph(List<IEntity> e) {
			return new Selector { Lambda = x => e };
		}

		// Unravel graph implicitly
		public static implicit operator List<QueueAction>(ActionGraph g) {
			return g.Unravel();
		}

		// Repeated action
		public static ActionGraph operator *(ActionGraph x, ActionGraph y) {
			return x.Repeat(y);
		}

		// Convert "Condition > ActionGraph" into a Trigger
		public static Trigger operator>(Condition x, ActionGraph y) {
			return new Trigger(y, x);
		}
		public static Trigger operator<(Condition x, ActionGraph y) {
			throw new NotImplementedException();
		}

		public List<QueueAction> Unravel(ActionGraph g = null) {
			if (g == null)
				g = this;
			var ql = new List<QueueAction>();
			foreach (var action in g.Graph) {
				action.CompiledArgs = new ActionResult[action.Args.Count];
				action.EagerArgs = new QueueAction[action.Args.Count];
				for (int i = 0; i < action.Args.Count; i++)
					if (action.Args[i] != null) {
						var l = Unravel(action.Args[i]);
						// If the argument unravels to a single action, check if it can be evaluated now
						if (l.Count == 1 && l[0] is PreCompiledQueueAction)
							// These actions always give the same results and can be evaluated now
							action.CompiledArgs[i] = l[0].Run(null, null, null);
						else if (l.Count == 1 && l[0] is EagerQueueAction)
							// These actions should be evaluated as in-place arguments rather than queued directly
							action.EagerArgs[i] = l[0];
						else
							ql.AddRange(l);
					}
				ql.Add(action);
			}
			return ql;
		}
	}

	internal static class QueueActionListExtensions
	{
		public static List<QueueAction> Then(this List<QueueAction> ql, ActionGraph act) {
			ql.AddRange(act.Unravel());
			return ql;
		}
	}
}
