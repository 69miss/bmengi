using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DoboEngineer.Pump;

public partial class PumpCfgWnd : Window
{
    public PumpCfgWnd()
    {
        InitializeComponent();
        DataContext = new PumpCfgViewModel();
    }
}