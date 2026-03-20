using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using PumpsSystem.Controls;
using PumpsSystem.Module;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace PumpsSystem.Pump;

public partial class Window1 : Window
{
    MainWindowViewModel mainWindowViewModel;
    public Window1()
    {
        mainWindowViewModel = new() { MsgBoxShowFun = MsgBoxShow };
        this.DataContext = mainWindowViewModel;
        InitializeComponent();
        Closed += Window1_Closed;

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
    async Task<int> MsgBoxShow(string msg)
    {
        var box = new MsgBox();
        return await MsgBox.Show(this, "Message", msg, false);
    }

    private void MenuItem_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (mainWindowViewModel.cmd == null)
            return;
        var vWin = new ValList();
        foreach (var item in mainWindowViewModel.cmd.Items)
        {
            vWin.VM.DataItems.Add(item);
        }
        vWin.Show();

    }

    private void MenuItem_Click_1(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        new PumpCfgWnd().ShowDialog(this);
    }

    private void MenuItem_Click_2(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        //var lang =PumpModule.Default.GetService<PumpLang>();
        //if(lang==null)
        //    return;
        //if (CultureInfo.CurrentCulture.Name != "en")
        //    lang.ChangeLanguage(CultureInfo.GetCultureInfo("zh-cn"));
        //else
        //    lang.ChangeLanguage(CultureInfo.GetCultureInfo("en"));

    }
}