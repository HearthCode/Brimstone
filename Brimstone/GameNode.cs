using System.Collections.Generic;
using System.Linq;

namespace Brimstone
{
	public class GameNode
	{
		public Game Game { get; }
		public GameNode Parent { get; set; }
		public HashSet<GameNode> Children { get; }

		public GameNode(Game Game, GameNode Parent = null, bool TrackChildren = true) {
			this.Game = Game;
			this.Parent = Parent;
			if (TrackChildren) {
				Children = new HashSet<GameNode>();
				if (Parent != null)
					Parent.AddChild(this);
			}
		}

		public void AddChild(GameNode Child) {
			Child.Parent = this;
			Children.Add(Child);
		}

		public GameNode AddChild(Game Child) {
			// Creating GameNode also calls AddChild
			return new GameNode(Child, this, Children != null);
		}

		public HashSet<GameNode> AddChildren(IEnumerable<Game> Children) {
			var newChildren = new HashSet<GameNode>();
			foreach (var child in Children)
				newChildren.Add(AddChild(child));
			return newChildren;
		}

		public void AddChildren(IEnumerable<GameNode> Children) {
			foreach (var child in Children)
				this.Children.Add(child);
		}

		public GameNode Branch() {
			var clone = Game.CloneState();
			return new GameNode(clone, this, Children != null);
		}

		public IEnumerable<GameNode> Branch(int Quantity) {
			var clones = Game.CloneStates(Quantity);
			var nodes = clones.Select(c => new GameNode(c, this, Children != null));
			return nodes;
		}
	}

	public class WeightedGameNode : GameNode
	{
		public double Weight { get; set; }

		public WeightedGameNode(Game Game, GameNode Parent = null, double Weight = 1.0, bool TrackChildren = true)
			: base(Game, Parent, TrackChildren) {
			this.Weight = Weight;
		}

		public virtual WeightedGameNode AddChild(Game Child, double Weight = 1.0) {
			// Creating GameNode also calls AddChild
			return new WeightedGameNode(Child, this, Weight, Children != null);
		}

		public HashSet<GameNode> AddChildren(IEnumerable<Game> Children, double Weight = 1.0) {
			var newChildren = new HashSet<GameNode>();
			foreach (var child in Children)
				newChildren.Add(AddChild(child, Weight));
			return newChildren;
		}

		public IEnumerable<WeightedGameNode> AddChildren(Dictionary<Game, double> Children) {
			// Creating GameNode also calls AddChild
			return Children.Select(kv => new WeightedGameNode(kv.Key, this, kv.Value, Children != null));
		}

		public WeightedGameNode Branch(double Weight = 1.0) {
			var clone = Game.CloneState();
			return new WeightedGameNode(clone, this, Weight, Children != null);
		}

		public IEnumerable<WeightedGameNode> Branch(int Quantity, double Weight = 1.0) {
			var clones = Game.CloneStates(Quantity);
			var nodes = clones.Select(c => new WeightedGameNode(c, this, Weight, Children != null));
			return nodes;
		}
	}

	public class ProbabilisticGameNode : WeightedGameNode
	{
		public double Probability { get; set; }

		public ProbabilisticGameNode(Game Game, ProbabilisticGameNode Parent = null, double Weight = 1.0, bool TrackChildren = true)
			: base(Game, Parent, Weight, TrackChildren) {

			Probability = (Parent != null ? Parent.Probability * Weight : Weight);
		}

		public new ProbabilisticGameNode AddChild(Game Child, double Weight = 1.0) {
			// Creating GameNode also calls AddChild
			return new ProbabilisticGameNode(Child, this, Weight, Children != null);
		}

		public new IEnumerable<ProbabilisticGameNode> AddChildren(Dictionary<Game, double> Children) {
			// Creating GameNode also calls AddChild
			return Children.Select(kv => new ProbabilisticGameNode(kv.Key, this, kv.Value, Children != null));
		}

		public new ProbabilisticGameNode Branch(double Weight = 1.0) {
			var clone = Game.CloneState();
			return new ProbabilisticGameNode(clone, this, Weight, Children != null);
		}

		public new IEnumerable<ProbabilisticGameNode> Branch(int Quantity, double Weight = 1.0) {
			var clones = Game.CloneStates(Quantity);
			var nodes = clones.Select(c => new ProbabilisticGameNode(c, this, Weight, Children != null));
			return nodes;
		}
	}
}
