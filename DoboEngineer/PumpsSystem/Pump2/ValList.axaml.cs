using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace PumpsSystem.Pump2;

public partial class ValList : Window
{
    public ValListVM VM { get; set; }
    public ValList()
    {
        InitializeComponent();
        VM = new ValListVM();
        DataContext = VM;
    }
}