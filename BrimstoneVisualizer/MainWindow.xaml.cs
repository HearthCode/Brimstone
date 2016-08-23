using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Brimstone;
using System.Reflection;

namespace BrimstoneVisualizer
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow() {
			InitializeComponent();
		}

		public void UpdateDisplay() {
			tbActionQueue.Text = App.Game.ActionQueue.ToString();
			tbActionResultStack.Text = App.Game.ActionQueue.StackToString();
			tbPowerHistory.Text = App.Game.PowerHistory.ToString();
			tbPlayer1Hand.Text = App.Game.Player1.Hand.ToString();
			tbPlayer2Hand.Text = App.Game.Player2.Hand.ToString();
			tbPlayer1Board.Text = App.Game.Player1.Board.ToString();
			tbPlayer2Board.Text = App.Game.Player2.Board.ToString();
			svPowerHistory.ScrollToEnd();
		}

		private void btnStepQueue_Click(object sender, RoutedEventArgs e) {
			// TODO: Don't crash when the queue is empty

			if (App.Game == null)
				MessageBox.Show("No game script loaded", "Error", MessageBoxButton.OK);
			else {
				UpdateDisplay();
				App.QueueRead.Set();
			}
		}

		private void btnStepQueue5_Click(object sender, RoutedEventArgs e) {
			if (App.Game == null) {
				MessageBox.Show("No game script loaded", "Error", MessageBoxButton.OK);
				return;
			}

			int i = 0;

			EventHandler<QueueActionEventArgs> waiter = (o, ea) => {
				i++;
			};
			App.Game.ActionQueue.OnActionStarting += waiter;
			while (i < 5) {
				App.QueueRead.Set();
				System.Threading.Thread.Sleep(10);
			}
			App.Game.ActionQueue.OnActionStarting -= waiter;
			UpdateDisplay();
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
			App.EndGame();
		}

		private void Button_Click(object sender, RoutedEventArgs e) {
			Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

			dlg.DefaultExt = ".dll";
			dlg.Filter = "Brimstone Scripts (*.dll)|*.dll";

			bool? result = dlg.ShowDialog();

			if (result == true) {
				string scriptDll = dlg.FileName;

				try {
					var asm = Assembly.LoadFile(scriptDll);
					var type = asm.GetType("BrimstoneGameScript.BrimstoneGame");
					App.Script = Activator.CreateInstance(type) as IBrimstoneGame;
					App.EndGame();
					App.StartGame();
					App.GameStarted.WaitOne();
					UpdateDisplay();
				}
				catch (Exception ex) {
					MessageBox.Show("Could not load game script: " + ex.Message, "Error", MessageBoxButton.OK);
					return;
				}
			}
		}
	}
}
