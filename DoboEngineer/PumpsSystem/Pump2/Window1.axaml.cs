using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using PumpsSystem.AvaloniaUI;
using PumpsSystem.Controls;
using PumpsSystem.Module;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace PumpsSystem.Pump2;

public partial class Window1 : WindowCommBase<Window1VM>
{
    Window1VM mainWindowViewModel;
    public Window1()
    {
        mainWindowViewModel = new() { MsgBoxShowFun = MsgBoxShow };
        this.DataContext = mainWindowViewModel;
        InitializeComponent();  
        Closed += Window1_Closed;
        WeakReferenceMessenger.Default.Register<string,string>(this, BusEventName.Main_ShowNotification, (p, p1) => {
            ShowNotification(p1, true);
        });


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
            vWin.VM.PumpCmd = mainWindowViewModel.cmd;
        }
        vWin.Show();

    }
    
    private void MenuItem_Click_1(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var dlg = new PumpCfgWnd(new PumpCfgViewModel());

        dlg.ShowDialog(this);
    }

    private void MenuItem_Click_2(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        isLogin = false;

    }
    bool isLogin=false;
    private async void Button_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
#if DEBUG
        isLogin = true;
#endif
        if (isLogin)
            return;
        btnSys.Flyout.Hide();
        var re = await ConfirmBox.ShowAsync(this, "Operations Management", "Please enter the administrative password", "*Password");
        if (re!=null&&re.TryGetValue("*Password", out var  pwd)&&pwd== "dobo123456")
        {
            isLogin = true;
            btnSys.Flyout.ShowAt(btnSys);
        }

    }
}