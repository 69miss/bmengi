using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using PumpsSystem.Controls;
using System.Threading.Tasks;

namespace PumpsSystem.Pump;

public partial class PumpCfgWnd : WindowVm<PumpCfgViewModel>
{
    public PumpCfgWnd()
    {
        InitializeComponent();
        this.Loaded += PumpCfgWnd_Loaded;
        DataContextVal = new PumpCfgViewModel() {MsgBoxShowFun=MsgBoxShow };
    }

    private void PumpCfgWnd_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        DataContextVal.Init();
    }
    async Task<int> MsgBoxShow(string msg)
    {
        var box = new MsgBox();
        return await MsgBox.Show(this, "提示消息", msg, false);
    }
}
public abstract class WindowVm<T>: Window
{
    public virtual T DataContextVal { get { return (T)DataContext; } set { DataContext = value; } }
}