using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentModbus;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DoboEngineer.Pump;

    public partial class PumpAdapter: ObservableObject
{
    //状态更新、命令发送 地址，类型，值

    private ModbusTcpClient _client;
    private CancellationTokenSource _cts;

    public PumpAdapter()
    {
        _client = new ModbusTcpClient();

        // 初始化4个泵
        // 泵1基地址 40005 (根据图片推算：频率反馈40005)
        //Pumps.Add(new PumpViewModel(this, "泵1区域", 4)); // 4代表40005 (0-based index)
        //Pumps.Add(new PumpViewModel(this, "泵2区域", 11)); // 40012
        //Pumps.Add(new PumpViewModel(this, "泵3区域", 18)); // 40019
        //Pumps.Add(new PumpViewModel(this, "泵4区域", 25)); // 40026
    }

    // --- 连接设置 ---
    [ObservableProperty] private string _ipAddress = "192.168.2.8";
    [ObservableProperty] private int _port = 502;
    [ObservableProperty] private bool _isConnected;

    // --- 全局参数 ---
    [ObservableProperty] private ushort _heartBeat;       // 40001
    //[ObservableProperty] private ushort _statusFeedback;  // 40002
    //[ObservableProperty] private ushort _pumpControlWord; // 40003
    //[ObservableProperty] private ushort _controlMode;     // 40004

    // --- 泵列表 ---
    //public ObservableCollection<PumpViewModel> Pumps { get; } = new();

    // --- 日志 ---
    [ObservableProperty] private string _logText = "";
    public void AddLog(string msg)
    {
        var time = DateTime.Now.ToString("HH:mm:ss");
        // 确保在UI线程更新
        Dispatcher.UIThread.Post(() =>
        {
            LogText = $"{time} [PumpAdapter] {msg}";// + LogText;
            Console.WriteLine(LogText);
            if (LogText.Length > 2000) LogText = LogText.Substring(0, 2000); // 限制长度
        });
    }

    // --- 连接/断开逻辑 ---
    [RelayCommand]
    private void ToggleConnection()
    {
        if (IsConnected)
        {
            Disconnect();
        }
        else
        {
            Connect();
        }
    }

    [ObservableProperty]  ObservableCollection<DataItem> dataItems;

    private void Connect()
    {
        try
        {
            AddLog($"正在连接 {IpAddress}:{Port} ...");
            _client.Connect(new IPEndPoint(IPAddress.Parse(IpAddress), Port));
            IsConnected = true;

            AddLog("设备连接成功！");

            // 启动轮询
            _cts = new CancellationTokenSource();
            _ = PollingLoop(_cts.Token);
        }
        catch (Exception ex)
        {
            AddLog($"连接失败: {ex.Message}");
            IsConnected = false;
        }
    }

    private void Disconnect()
    {
        _cts?.Cancel();
        _client.Disconnect();
        IsConnected = false;
        AddLog("已断开连接");
    }
    public Action<short[]> HandleData;
    public void HandleData2(short[] rawData) {

        // 1. 更新全局参数 (UI线程更新)
        //Dispatcher.UIThread.Post(() =>
        //{
        //    //HeartBeat = (ushort)shortData[0];       // 40001
        //    //StatusFeedback = (ushort)shortData[1];  // 40002
        //                                            // ControlWord 和 ControlMode 通常是输入框，
        //                                            // 只有在初始化时才从PLC读，否则会覆盖用户正在输入的值。
        //                                            // 这里仅作演示：
        //                                            // PumpControlWord = (ushort)shortData[2]; 
        //                                            // ControlMode = (ushort)shortData[3];
        //});
        // 2. 分发数据给 4 个泵
        // 泵1数据从 index 4 开始 (40005)
        // 每个泵占用 7 个寄存器 (反馈3 + 设定4)
        //for (int i = 0; i < 4; i++)
        //{
        //    var pump = Pumps[i];
        //    int offset = 4 + (i * 7); // 4, 11, 18, 25

        //    // 切片传递数据
        //    var pumpSlice = shortData.Slice(offset, 7);

        //    Dispatcher.UIThread.Post(() =>
        //    {
        //        pump.UpdateFromData(pumpSlice);
        //    });
        //}
    }
    private async Task PollingLoop(CancellationToken token)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(1000));

        while (await timer.WaitForNextTickAsync(token))
        {
            try
            {
                if (!_client.IsConnected) continue;
                var rawData = _client.ReadHoldingRegisters<short>(0, 0, 32).ToArray();

                HandleData?.Invoke(rawData);
                // 将 bytes 转为 shorts (假设PLC是16位有符号或无符号)
                var shortData = rawData;//.GetLittleEndian<short>(); 
            }
            catch (Exception ex)
            {
                AddLog($"读取异常: {ex.Message}");
            }
        }
    }

    // --- 写入方法 (暴露给 PumpViewModel 使用) ---
    public async Task<string> WriteRegistersAsync(int startAddress,IEnumerable<IConvertible> data)
    {
        if (!IsConnected) return "未连接!";

        // 切到后台线程写，防止卡顿
        return await Task.Run<string>(() =>
          {
              try
              {
                 // _client.WriteMultipleRegisters(0, startAddress, data.ToArray());
              }
              catch (Exception ex)
              {
                  AddLog($"写入失败: {ex.Message}");
                  return ex.Message;
              }
              return null;
          });
    }

    // --- 写入全局控制参数 ---
    [RelayCommand]
    private async Task WriteGlobalParams(DataItem param)
    {
        // 写入 40003, 40004
      //  var data = new ushort[] { PumpControlWord, ControlMode };
        await WriteRegistersAsync(param.Address, [param.EditVal]); // 地址2对应40003
        AddLog("全局控制参数写入成功");
    }
}
public partial class DataItem : ObservableObject,IDataItemBase
{

    [ObservableProperty] ushort address;
    [ObservableProperty] string name;
    [ObservableProperty] IConvertible value;
    [ObservableProperty] bool canWrite;
    [ObservableProperty] string remark;
    [ObservableProperty] IConvertible editVal;
    public byte? FmtRadix;
    [ObservableProperty] string valueFmt;
    [ObservableProperty] string editValFmt;
    //public IConvertible EditVal1 { get => editVal; set {
    //        if (value is string str)
    //        { 

    //        }
    //        editVal = value; } 
    //}

    public DataItem(byte? fmtRadix = null)
    {
        FmtRadix = fmtRadix ?? 10;
        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(Value))
            {
                if (FmtRadix == null || FmtRadix == 10)
                    ValueFmt = Value + "";
                else if (Value is IFormattable formattable)
                {

                    ValueFmt = FmtRadix switch
                    {
                        2 => formattable.ToString("B", null),
                        16 => formattable.ToString("X", null)
                    };
                }
                else ValueFmt = Value + "";

            }
            if (e.PropertyName == nameof(EditValFmt))
            {
                EditVal = Convert.ToInt16(EditValFmt, FmtRadix.Value);
            }
            if (e.PropertyName == nameof(EditVal))
            {
                if (EditVal is IFormattable formattable)
                {

                    EditValFmt = FmtRadix switch
                    {
                        2 => formattable.ToString("B", null),
                        10 => EditVal + "",
                        16 => formattable.ToString("X", null),
                    };
                }
                else EditValFmt = EditVal + "";
            }
        };
    }
}

