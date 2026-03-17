using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace PumpsSystem.Pump;

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