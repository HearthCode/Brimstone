using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Brimstone.Benchmark
{
	internal class Supervisor
	{
		private TextWriter cOut;
		private Stopwatch sw;
		private StringBuilder csv = new StringBuilder();
		private int timeoutMs = 0;

		public Supervisor(int timeout) {
			timeoutMs = timeout;
		}

		public void Run(Test test) {
			var testName = test.Name + (test.Iterations > 1 ? "; " + test.Iterations + " iterations" : "");
			var results = new List<long>();

			for (int i = 0; i < 6; i++) {
				Settings.CopyOnWrite = true;
				Settings.ZoneCaching = true;
				Settings.EntityHashCaching = true;
				Settings.GameHashCaching = true;
				Settings.UseGameHashForEquality = true;

				switch (i) {
					case 0: break;
					case 1: Settings.CopyOnWrite = false; break;
					case 2: Settings.ZoneCaching = false; break;
					case 3: Settings.EntityHashCaching = Settings.GameHashCaching = false; break;
					case 4: Settings.UseGameHashForEquality = false; break;
					case 5: Settings.CopyOnWrite = Settings.ZoneCaching =
							Settings.EntityHashCaching = Settings.GameHashCaching =
							Settings.UseGameHashForEquality = false; break;
				}

				var game = test.SetupCode();
				var cts = new CancellationTokenSource();
				Thread taskThread = null;
				cts.CancelAfter(timeoutMs);
				try {
					Task.Run((() => {
						taskThread = Thread.CurrentThread;
						Start(i == 0 ? testName : "");
						test.BenchmarkCode(game, test.Iterations);
						sw.Stop();
					})).Wait(cts.Token);
				}
				catch (OperationCanceledException) {
					taskThread.Abort();
				}
				results.Add(Result().ElapsedMilliseconds);
			}
			Console.WriteLine();
			csv.Append(testName);
			foreach (var r in results)
				csv.Append("," + r);
			csv.AppendLine();
		}

		public void Start(string testName = null) {
			if (!string.IsNullOrEmpty(testName))
				Console.Write(testName.PadRight(120));
			cOut = Console.Out;
			Console.SetOut(TextWriter.Null);
			sw = new Stopwatch();
			sw.Start();
		}

		public Stopwatch Result() {
			sw.Stop();
			Console.SetOut(cOut);
			Console.Write((sw.ElapsedMilliseconds + "ms").PadRight(12));
			return sw;
		}

		public void WriteResults(string path) {
			csv.Insert(0,
			"Build," +
#if DEBUG
			"Debug " +
#else
			"Release " +
#endif
			Assembly.GetAssembly(typeof(Game)).GetName().Version + "\r\n" +
			"\"\",\"\"\r\nTest Name,All Opts,CoW Disabled,Zone Cache Disabled,Hash Cache Disabled,Hash Equality Disabled,No Opts\r\n");
			File.WriteAllText(path, csv.ToString());
		}
	}

	internal struct Test
	{
		public string Name;
		public int Iterations;
		public Func<Game> SetupCode;
		public Action<Game, int> BenchmarkCode;

		public Test(string ln, Action<Game, int> benchmark, Func<Game> setup = null, int it = Benchmarks.DefaultIterations) {
			Name = ln;
			Iterations = it;
			SetupCode = setup ?? Benchmarks.Default_Setup;
			BenchmarkCode = benchmark;
		}
	}

	internal partial class Benchmarks
	{
		public Dictionary<string, Test> Tests;

		// Create and start a game with Player 1 as the first player and no decks
		public static Game NewEmptyGame() {
			var game = new Game(HeroClass.Druid, HeroClass.Druid, PowerHistory: true);
			Debug.Assert(game.Entities.Count == 6);
			game.Start(1);
			return game;
		}

		// Create and start a game with Player 1 as the first player with randomly filled decks
		public static Game NewPopulatedGame() {
			var cOut = Console.Out;
			Console.SetOut(TextWriter.Null);
			var game = new Game(HeroClass.Druid, HeroClass.Druid, PowerHistory: true);
			game.Player1.Deck.Fill();
			game.Player2.Deck.Fill();
			Debug.Assert(game.Entities.Count == 66);
			game.Start(1);
			Console.SetOut(cOut);
			return game;
		}

		// Create and start a game with Player 1 as the first player,
		// using MaxMinions per side of the board, of which NumBoomBots are Boom Bots
		// and the rest are the minion specified by the card name in FillMinion
		public static Game NewScenarioGame(int MaxMinions, int NumBoomBots, string FillMinion, bool FillDeck = true) {
			var cOut = Console.Out;
			Console.SetOut(TextWriter.Null);
			var game = new Game(HeroClass.Druid, HeroClass.Druid, PowerHistory: true);
			if (FillDeck) {
				game.Player1.Deck.Fill();
				game.Player2.Deck.Fill();
			}
			game.Start(1);

			for (int i = 0; i < MaxMinions - NumBoomBots; i++)
				game.CurrentPlayer.Give(FillMinion).Play();
			for (int i = 0; i < NumBoomBots; i++)
				game.CurrentPlayer.Give("Boom Bot").Play();
			game.BeginTurn();
			for (int i = 0; i < MaxMinions - NumBoomBots; i++)
				game.CurrentPlayer.Give(FillMinion).Play();
			for (int i = 0; i < NumBoomBots; i++)
				game.CurrentPlayer.Give("Boom Bot").Play();
			Console.SetOut(cOut);
			return game;
		}

		public void Run(string filter, int timeout) {
			var benchmark = new Supervisor(timeout);

			foreach (var kv in Tests)
				if (kv.Key.ToLower().Contains(filter)) {
					Console.Write(("Test [" + kv.Key + "]: ").PadRight(60));
					benchmark.Run(kv.Value);
				}

			var path = "benchmarks.csv";
			benchmark.WriteResults(path);
			Console.WriteLine("Benchmark results written to: " + path);
		}

		static void Main(string[] args) {
			string filter = string.Empty;
			int timeout = 7000;

			string usage = "Usage: benchmarks [--filter=text] [--timeout=milliseconds]...";

			foreach (string arg in args) {
				try {
					string name = arg.Substring(0, arg.IndexOf("=")).ToLower().Trim();
					string value = arg.Substring(name.Length + 1);
					switch (name) {
						case "--filter":
							filter += value.ToLower();
							break;
						case "--timeout":
							if (!int.TryParse(value, out timeout)) {
								Console.WriteLine(usage);
								return;
							}
							break;
						default:
							Console.WriteLine(usage);
							return;
					}
				} catch (Exception) {
					Console.WriteLine(usage);
					return;
				}
			}

			Console.WriteLine("Benchmarks for Brimstone build " + Assembly.GetAssembly(typeof(Game)).GetName().Version);
#if DEBUG
			Console.WriteLine("WARNING: Running in Debug mode. Benchmarks will perform worse than Release builds.");
#endif
			if (!string.IsNullOrEmpty(filter))
				Console.WriteLine("Running benchmarks using filter: " + filter);

			var b = new Benchmarks();
			b.LoadDefinitions();
			b.Run(filter, timeout);
		}
	}
}
