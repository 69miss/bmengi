using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DoboEngineer.Pump;

public partial class CmdTest : Window
{
    public CmdTest()
    {
        DataContext=new CmdTestVm();
        InitializeComponent();
    }
}