using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using PumpsSystem.Controls;
using System.ComponentModel;
using System.Threading.Tasks;

namespace PumpsSystem.Pump2;

public partial class PumpCfgWnd : WindowVm<ISupportInitialize>
{
    
    public PumpCfgWnd(ISupportInitialize vm=null)
    {
        InitializeComponent();
        this.Loaded += PumpCfgWnd_Loaded;
         
        DataContextVal =vm?? new PumpCfgViewModel() {MsgBoxShowFun=MsgBoxShow };
    }

    private void PumpCfgWnd_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        DataContextVal.BeginInit();
    }
    async Task<int> MsgBoxShow(string msg)
    {
        var box = new MsgBox();
        return await MsgBox.Show(this, "提示消息", msg, false);
    }
}
public abstract class WindowVm<T>: Window   where T:class
{
    public virtual T DataContextVal { get { return DataContext as T; } set { DataContext = value; } }
}