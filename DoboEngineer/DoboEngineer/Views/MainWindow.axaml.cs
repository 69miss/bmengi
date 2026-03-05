using Avalonia.Controls;
using DoboEngineer.code;
using DoboEngineer.Pump;
using System;

namespace DoboEngineer.Views
{
    public partial class MainWindow : Window
    {
        public Lang L { get; set; } = Lang.d;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var win = new StandardSet();
            win.Show();
        }

        private void Button_Click_1(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            L.Main.Title = DateTime.Now + "";
            L.OnAllChanged();
        }

        private void Button_Click_2(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            new CmdTest().ShowDialog(this);
        }

        private void Button_Click_3(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            new Window1().Show();
        }

        private void Button_Click_4(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            new PumpCfgWnd().Show();    
        }
    }
}