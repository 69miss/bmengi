using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using System;

namespace DoboEngineer.Pump;

public partial class Window1 : Window
{
    public Window1()
    {
        InitializeComponent();
        Closed += Window1_Closed;
        this.DataContext = new MainWindowViewModel();
    }

    private void Window1_Closed(object? sender, System.EventArgs e)
    {
        (this.DataContext as IDisposable)?.Dispose();
    }

    private void OnTitleBarPointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            this.BeginMoveDrag(e);
        }
    }
}