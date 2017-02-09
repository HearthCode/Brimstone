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
			tbPowerHistory.ScrollToEnd();
		}

		private void btnStepQueue_Click(object sender, RoutedEventArgs e) {
			if (App.Game == null)
				MessageBox.Show("No game script loaded", "Error", MessageBoxButton.OK);
			else {
				UpdateDisplay();
				if (App.Game.State != GameState.COMPLETE)
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
			while (i < 5 && App.Game.State != GameState.COMPLETE) {
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
