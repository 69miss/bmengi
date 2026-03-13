using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.DependencyInjection;
using Dobo.Appl.HunterCmd;
using Dobo.Appl.Utility;
using DoboEngineer.code;
using DoboEngineer.SPC;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace DoboEngineer;

public partial class StandardSet : Window
{

    public StandardSet()
    {
        DataContext = this;
        InitializeComponent();
        Height = 400;
        Width = 600;
        htCommand = new HTCommand("192.168.0.55", 10001);
        btnBegin2.IsVisible = true;
        btnBegin.IsVisible = false;
    }
    public Lang L { get; set; } = Lang.d;
    SPCCommand spcTcpCommand = new SPCCommand();
    HTCommand htCommand;
    bool isBeginStandard = false;
    public string Txt { get; set; } = "tttttttttt2";
    MsgBox msgBox;
    private async void Button_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        //var result = await DialogHost.Show("确认删除吗？", "dlg");
        //if (result == null)
        //    return;
        try
        {


            spcTcpCommand.Connect();
            spcTcpCommand.ReadStatus();
            var re = await htCommand.ResetTcp();
            if (re)
            {
                htCommand.RemotoMode(true);
            }

            spcTcpCommand.StateSetAction = StateHandleAsync;
            spcTcpCommand.SpcStartPollingAsync();
            isBeginStandard = true;
            btnBegin.IsEnabled = !isBeginStandard;
            WriteLineText("准备校准,请插入外部白校准盒,并触发校准按钮!");
            msgBox = new MsgBox();
            msgBox.ShowCancel = false;
            msgBox.ContentTxt = """
            准备校准：
            1、请将白校准盒插入颜色传感器下端盖处
            2、完成步骤1后,请触发校准按钮
            注意：校准完成前请勿移动校准盒
            """;
            await msgBox.ShowDialog(this);
        }
        catch (Exception)
        {
            WriteLineText("连接失败！");
        }
        //workService.StateHandle
        //请插入外部白板后点击确认开关
        //白板校准
        //黑板校准
        //校准结束
    }
    void WriteLineText(string txt)
    {
        Dispatcher.UIThread.Invoke(() =>
      {
          tbTxt.Text += txt + "\r\n";
      });
    }

    async void StateHandleAsync(Dobo.Appl.SPC100.SpcStateInfo stateInfo)
    {
        if (!isBeginStandard)
            return;
        if (!stateInfo.SubInAuxSwitch)
            return;
        try
        {
            isBeginStandard = false;
            msgBox?.Close();
            var calc = new XYZCalc(XYZCalc.CmfType.R400_700_10);
            WriteLineText(DateTime.Now + " 开始校准...");
            spcTcpCommand.ExecuteIOCommand(Dobo.Appl.SPC100.IOFunctionCode.OpenLensCover);
            await Task.Delay(2000);
            if (true)
            {
                WriteLineText("标定前外部白测试中...");
                var re = htCommand.PhotometricDataSingle(98);
                var xyz = calc.CalcXyzByR(re.Data.Data.Select(p => (double)p).ToArray());
                var lab = calc.XyzToLab(xyz[0], xyz[1], xyz[2]);
                WriteLineText($"标定前外部白测试完成 Lab:{lab[0]:F2},{lab[1]:F2},{lab[2]:F2}");
                await Task.Delay(1000);
            }
            WriteLineText("外部白校准-98");
            htCommand.Standardization(false, 98);
            await Task.Delay(1000);
            WriteLineText("外部白校准完成");
            spcTcpCommand.ExecuteIOCommand(Dobo.Appl.SPC100.IOFunctionCode.CloseLensCover);
            await Task.Delay(2000);
            WriteLineText("校准黑-68");
            htCommand.Standardization(true, 68);
            await Task.Delay(1000);
            WriteLineText("校准黑完成");
            await Task.Delay(1000);
            //
            if (true)
            {
                WriteLineText("准备外部白测试...");
                spcTcpCommand.ExecuteIOCommand(Dobo.Appl.SPC100.IOFunctionCode.OpenLensCover);
                await Task.Delay(2000);
                WriteLineText("外部白测试中...");
                var re = htCommand.PhotometricDataSingle(98);
                await Task.Delay(1000);
                spcTcpCommand.ExecuteIOCommand(Dobo.Appl.SPC100.IOFunctionCode.CloseLensCover);
                var xyz = calc.CalcXyzByR(re.Data.Data.Select(p => (double)p).ToArray());
                var lab = calc.XyzToLab(xyz[0], xyz[1], xyz[2]);
                WriteLineText($"外部白测试完成 Lab:{lab[0]:F2},{lab[1]:F2},{lab[2]:F2}");
                //if (lab[0] > 80 && lab[0] < 100)
                await MsgBox.Show(this, "提示", "校准完成");
                //else
                //    await MsgBox.Show(this, "提示", "请擦拭内部白板后重新校准",false);
            }
        }
        catch (Exception ex)
        {
            WriteLineText("校准失败，请稍后重试！" + ex.Message);
        }
        finally
        {
            isBeginStandard = false;
            btnBegin.IsEnabled = !isBeginStandard;
        }
    }

    private async void btnBegin2_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        try
        {
            spcTcpCommand.Connect();
            spcTcpCommand.ReadStatus();
            var re = await htCommand.ResetTcp();
            if (re)
            {
                htCommand.RemotoMode(true);
                
            }else
                throw new ArgumentException("连接失败");
            //spcTcpCommand.StateSetAction = StateHandleAsync;
            //spcTcpCommand.SpcStartPollingAsync();
            isBeginStandard = true;
            btnBegin.IsEnabled = !isBeginStandard;
            WriteLineText("准备校准,请插入外部白校准盒!");
            var contentTxt = """
            准备校准：
            1、请将白校准盒插入颜色传感器下端盖处
            2、完成步骤1后,请点击确认按钮
            注意：校准完成前请勿移动校准盒
            """;
            //await msgBox.ShowDialog<int>(this);
            var dlgRe = await MsgBox.Show(this, "提示", contentTxt);
            if (dlgRe == 1)
                StateHandleAsync(new Dobo.Appl.SPC100.SpcStateInfo() { SubInAuxSwitch = true });
        }
        catch (Exception)
        {
            WriteLineText("连接失败！");
        }
    }
}