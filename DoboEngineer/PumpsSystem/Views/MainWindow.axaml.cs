using Avalonia.Controls;
using PumpsSystem.Pump;

namespace PumpsSystem.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            new Window1().ShowDialog(this);
        }
    }
}