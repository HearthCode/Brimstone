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

		private void btnStepQueue_Click(object sender, RoutedEventArgs e) {
			tbActionQueue.Text = App.Game.ActionQueue.ToString();
			tbActionResultStack.Text = App.Game.ActionQueue.StackToString();
			tbPowerHistory.Text = App.Game.PowerHistory.ToString();
			svPowerHistory.ScrollToEnd();
			App.QueueRead.Set();
		}

		private void Window_Loaded(object sender, RoutedEventArgs e) {
			App.StartGame();
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
			App.GameThread.Abort();
		}
	}
}
