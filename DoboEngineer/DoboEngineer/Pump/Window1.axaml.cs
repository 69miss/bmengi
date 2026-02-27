using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace DoboEngineer.Pump;

public partial class Window1 : Window
{
    public Window1()
    {
        InitializeComponent();
        this.DataContext = new MainWindowViewModel();
    }
    private void OnTitleBarPointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            this.BeginMoveDrag(e);
        }
    }
}