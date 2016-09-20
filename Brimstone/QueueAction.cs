using System;
using System.Collections.Generic;
using Brimstone.Entities;

namespace Brimstone.QueueActions
{
	// Standard QueueAction, placed into queue and processed normally
	// QueueActions can take zero or more ActionGraph arguments and have access to Game, Source and Args in Run()
	public abstract class QueueAction : ICloneable
	{
		// Prevent instantiation from outside the Brimstone class library
		internal QueueAction() { }

		internal List<ActionGraph> Args { get; } = new List<ActionGraph>();
		internal QueueAction[] EagerArgs { get; set; }
		internal ActionResult[] CompiledArgs { get; set; }

		internal abstract ActionResult Run(Game game, IEntity source, ActionResult[] args);

		internal ActionGraph Then(ActionGraph g) {
			return ((ActionGraph) this).Then(g);
		}

		public override string ToString() {
			string s = GetType().Name;
			if (Args.Count > 0) {
				s += "(";
				for (int i = 0; i < Args.Count; i++) {
					if (CompiledArgs != null && CompiledArgs[i].HasResult)
						s += CompiledArgs[i];
					else if (EagerArgs?[i] != null)
						s += EagerArgs[i];
					else {
						s += "?";
					}
					s += ", ";
				}
				s = s.Substring(0, s.Length - 2) + ")";
			}
			return s;
		}

		public object Clone() {
			// A shallow copy is good enough: all properties and fields are value types
			// except for Args which is immutable
			return MemberwiseClone();
		}
	}

	// QueueActions that will be evaluated when they are unravelled in an ActionGraph, if they are arguments to another QueueAction
	// The results are stored in CompiledArgs in the QueueAction to which this QueueAction is an argument
	// PreCompiledQueueActions must require no ActionGraph arguments and must not require access to Game, Source or Args in Run()
	public abstract class PreCompiledQueueAction : QueueAction {}

	// QueueActions that will be evaluated in-place as arguments to another QueueAction before that QueueAction is evaluated
	// They will not be placed in the queue directly when used as arguments to another QueueAction
	// EagerQueueActions must require no ActionGraph arguments and must not require acces to Args in Run(). They can access Game and Source in Run()
	public abstract class EagerQueueAction : QueueAction {}
}
