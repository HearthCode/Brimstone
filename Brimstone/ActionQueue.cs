using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Brimstone
{
	public class ActionQueue
	{
		public Game Game { get; private set; }
		public Queue<QueueAction> Queue = new Queue<QueueAction>();
		public Stack<ActionResult> ResultStack = new Stack<ActionResult>();

		public void Attach(Game game) {
			Game = game;
		}

		public void Enqueue(ActionGraph g) {
			// Don't queue unimplemented cards
			if (g != null)
				g.Queue(this);
		}

		public List<ActionResult> Process() {
			while (Queue.Count > 0) {
				var action = Queue.Dequeue();
				Console.WriteLine(action);
				var args = new List<ActionResult>();
				for (int i = 0; i < action.Args.Count; i++)
					args.Add(ResultStack.Pop());
				args.Reverse();
				var result = action.Run(Game, args);
				if (result.HasResult)
					ResultStack.Push(result);
			}
			// Return whatever is left on the stack
			var stack = new List<ActionResult>(ResultStack);
			ResultStack.Clear();
			return stack;
		}
	}
}
