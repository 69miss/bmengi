using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using PumpsSystem.AvaloniaUI;
using PumpsSystem.Controls;
using System.ComponentModel;
using System.Threading.Tasks;

namespace PumpsSystem.Pump2;

public partial class PumpCfgWnd : WindowCommBase<ISupportInitialize>
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
