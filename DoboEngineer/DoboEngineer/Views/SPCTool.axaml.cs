using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Dobo.Appl.SPC100;
using DoboEngineer.SPC;
using DoboEngineer.ViewModels;
using System;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace DoboEngineer;

public partial class SPCTool : Window
{
    public SPCTool()
    {
        DataContext = new SpcUtilityVm();
        InitializeComponent();
        Closed += SPCTool_Closed;
        btnBegin2.IsVisible = true;
        btnBegin1.IsVisible = !btnBegin2.IsVisible;
    }
    bool isRun;

    private void SPCTool_Closed(object? sender, System.EventArgs e)
    {
        ctsSpc.Cancel();
        command?.Dispose();
    }

    SPCTcpCommand command;
    private CancellationTokenSource ctsSpc;

    public bool IsRun { get => isRun; set => isRun = value; }

    private void btnBegin2_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        btnBegin2.IsVisible = false;
        btnBegin1.IsVisible = !btnBegin2.IsVisible;
        Connect();
        RunAsync();

    }
    private void btnBegin1_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ctsSpc?.Cancel();
        command.Dispose();
        command = null;
        btnBegin2.IsVisible = true;
        btnBegin1.IsVisible = !btnBegin2.IsVisible;

    }
    void Connect()
    {
        try
        {
            command?.Dispose();
            command = new SPCCommand();
            command.Connect();
            var re = command.ReadStatus();
            WriteLineText($"单片机状态：{re}");
        }
        catch (Exception ex)
        {
            WriteLineText("连接异常：" + ex);
        }

    }
    
    
    public async Task RunAsync()
    {
        ctsSpc = new CancellationTokenSource();
        var token = ctsSpc.Token;
        try
        {
            WriteLineText($"开始执行{ctsSpc.GetHashCode()}：");
            
            int runNum = 1;
            var startStr = $"[{DateTime.Now}--{ctsSpc.GetHashCode()}]";
            while (!token.IsCancellationRequested)
            {

                try
                {
                    await Task.Delay(1000, token);
                    command.ExecuteIOCommand(IOFunctionCode.OpenLensCover);
                    await Task.Delay(2100, token);
                    command.ExecuteIOCommand(IOFunctionCode.CloseLensCover);
                    await Task.Delay(2100, token);
                    command.ExecuteIOCommand(IOFunctionCode.MoveToWhitePosition);
                    await Task.Delay(2100, token);
                    WriteLineText($"循环次数{runNum++}--{startStr}");
                    if (runNum % 10 == 1)
                    {
                        var re = command.ReadStatus();
                        WriteLineText($"单片机状态：{re}");
                    }
                }
                catch (Exception ex)
                {
                    if (token.IsCancellationRequested)
                        break;
                    WriteLineText(startStr+" "+ex.ToString());
                    //重连
                    if (command?.Connected != true)
                    {
                        WriteLineText($"重连--{startStr}");
                        Connect();
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            WriteLineText("取消执行");
            command.Dispose();
            command = null;
        }
        WriteLineText("停止");
        btnBegin2.IsVisible = true;
        btnBegin1.IsVisible = !btnBegin2.IsVisible;
    }
    void WriteLineText(string txt)
    {
        Console.WriteLine($"{DateTime.Now} : {txt}");
        Dispatcher.UIThread.Invoke(() =>
        {
            if (tbTxt.Text?.Length>5000) {
                tbTxt.Text = tbTxt.Text.Substring(1000);
            }
            tbTxt?.Text += $"{DateTime.Now} : {txt}\r\n";
        });
    }

    
}