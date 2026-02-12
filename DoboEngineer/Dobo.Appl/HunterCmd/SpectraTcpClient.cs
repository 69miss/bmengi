using Dobo.Appl.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dobo.Appl.HunterCmd;
public class SpectraTcpClient : IDisposable
{
    private TcpClient _tcpClient;
    private NetworkStream _networkStream;
    private StreamReader _streamReader;
    private StreamWriter _streamWriter;
    int msgNum = 0;
    // 传感器TCP配置（需根据实际设备IP和端口修改）
    public string SensorIp { get; }
    public int SensorPort { get; }

    // 消息缓冲区大小（根据协议最大消息长度调整，文档中最大为Photometric Data的~300字符）
    private const int ReceiveBufferSize = 1024;
    CancellationTokenSource listenLoopCTS;
    Tuple<int, ManualResetEventSlim, HunterPacket[]> nowMsgWaitEvent;
    /// <summary>
    /// 初始化TCP客户端
    /// </summary>
    /// <param name="sensorIp">传感器TCP IP地址</param>
    /// <param name="sensorPort">传感器TCP端口号</param>
    public SpectraTcpClient(string sensorIp = "192.168.0.55", int sensorPort = 10001)
    {
        SensorIp = sensorIp ?? throw new ArgumentNullException(nameof(sensorIp));
        if (sensorPort < 1 || sensorPort > 65535)
            throw new ArgumentOutOfRangeException(nameof(sensorPort), "端口号必须在1-65535之间");
        
        SensorPort = sensorPort;
        _tcpClient = new TcpClient();
    }

   public byte NextMsgNo()
    {
        return (byte)(++msgNum % 7 + 1);
    }
    public bool IsConnect()
    {

        return _tcpClient.Connected;
    }
    /// <summary>
    /// 连接传感器TCP服务
    /// </summary>
    public async Task<bool> ConnectAsync()
    {
        try
        {
            if (_tcpClient.Connected)
                return true;

            // 连接TCP服务器（超时5秒）
            await _tcpClient.ConnectAsync(SensorIp, SensorPort).WaitAsync(TimeSpan.FromSeconds(5));
            if (!_tcpClient.Connected)
            {
                InfoOutput("TCP连接失败：无法建立连接");
                return false;
            }

            // 初始化流读写器（ASCII编码，匹配协议）
            _networkStream = _tcpClient.GetStream();
            _streamReader = new StreamReader(_networkStream, Encoding.ASCII, false, ReceiveBufferSize);
            _streamWriter = new StreamWriter(_networkStream, Encoding.ASCII) { AutoFlush = true };

            InfoOutput($"TCP连接成功：{SensorIp}:{SensorPort}");
            listenLoopCTS?.Cancel();
            listenLoopCTS = new CancellationTokenSource();
            //ListenLoopAsync(listenLoopCTS.Token );
            hunterLabReceiver.BufferDiscardRecord = p => Console.WriteLine("移除无效字节："+p);
             Task.Run(() => ListenLoopAsync(listenLoopCTS.Token), listenLoopCTS.Token);
            return true;
        }
        catch (TimeoutException)
        {
            InfoOutput("TCP连接超时（5秒）");
            return false;
        }
        catch (SocketException ex)
        {
            InfoOutput($"TCP连接错误（Socket）：{ex.SocketErrorCode} - {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            InfoOutput($"TCP连接异常：{ex.Message}");
            return false;
        }
    }
    void InfoOutput(string msg)
    {
        Console.WriteLine(msg);
    }
    /// <summary>
    /// 发送协议命令（SOH类型：数据/控制指令）
    /// </summary>
    /// <param name="msgNo">消息编号（1-7，需与响应匹配）</param>
    /// <param name="commandType">命令标识（如'#'=Capabilities, 'R'=Start/Stop Run）</param>
    /// <param name="commandData">命令参数（协议格式字符串）</param>
     void SendCommand(string commandData, char? commandType = null, byte? msgNo = null)
    {
        ValidateConnection();
        msgNo = msgNo ?? NextMsgNo();
        // 1. 校验参数合法性
        if (msgNo < 1 || msgNo > 7)
            throw new ArgumentOutOfRangeException(nameof(msgNo), "消息编号必须为1-7");
        if (commandType != null && !char.IsAscii(commandType.Value))
            throw new ArgumentException("命令类型必须为ASCII字符", nameof(commandType));
        commandData ??= string.Empty;

        // 2. 构建Parameters块：type(1字符) + data(可变长度)
        string parametersBlock = $"{commandType}{commandData}";

        string fullMessage = BulidCmdStr(msgNo, parametersBlock);

        // 6. 发送消息（自动刷新，确保即时发送）
        _streamWriter.Write(fullMessage);
        InfoOutput($"已发送命令（MSG{msgNo}）：{fullMessage}");
    }

    private static string BulidCmdStr(byte? msgNo, string parametersBlock)
    {
        // 3. 构建Header块：class(SOH=0x01) + count(参数长度，3位数字) + msgno(1字符)
        string headerBlock = $"{(char)0x01}{SpectraParser.BaseToHexAscii((short)parametersBlock.Length)}{msgNo}";

        // 4. 计算校验和（Header + Parameters）
        string checksum = CalculateChecksum(headerBlock + parametersBlock);

        // 5. 组装完整消息（Header + Parameters + Checksum）
        string fullMessage = headerBlock + parametersBlock + checksum;
        return fullMessage;
    }
    private readonly SemaphoreSlim _asyncLock = new SemaphoreSlim(1, 1); 
    public HunterPacket SendCommand(string command)
    {
        //await _asyncLock.WaitAsync();
        try
        {
            lock (this)
            {
                var msgNum = NextMsgNo();
                SendCommand(command, null, msgNum);
                return ReceiveResponse(msgNum);
            }
        }
        finally
        {
           // _asyncLock.Release();
        }
    }

    /// <summary>
    /// 读取超时设置（单位：毫秒）
    /// </summary>
    public int ReceiveTimeout { get; set; } = 5000; 

    public HunterPacket ReceiveResponse(byte expectedMsgNo)
    {
        try
        {
            nowMsgWaitEvent = Tuple.Create((int)expectedMsgNo, new ManualResetEventSlim(), new HunterPacket[1]);
            Console.WriteLine(DateTime.Now + ":set-->nowMsgWaitEvent");
            Console.WriteLine($"{DateTime.Now}--{Thread.CurrentThread.ManagedThreadId}:set-->nowMsgWaitEvent后");
            if (nowMsgWaitEvent.Item2.Wait(ReceiveTimeout)
                && expectedMsgNo == nowMsgWaitEvent.Item1
                && nowMsgWaitEvent.Item3[0] != null)
            {
                var rp = nowMsgWaitEvent.Item3[0];
                return rp;
            }
            else
            {
                throw new TimeoutException("等待命令响应超时，消息号:" + expectedMsgNo);
            }
            //return new ResponseData() { IsSuccess=false };
        }
        finally
        {
            nowMsgWaitEvent.Item2.Dispose();
            nowMsgWaitEvent = null;
        }
    }

    HunterLabReceiver hunterLabReceiver=new HunterLabReceiver();
     
    public async Task ListenLoopAsync(CancellationToken token)
    {
        NetworkStream stream = _networkStream;
        //byte[] buffer = new byte[1024];

        byte[] buf = new byte[4096];

        while (!token.IsCancellationRequested)
        {
            Console.WriteLine($"{DateTime.Now}--{Thread.CurrentThread.ManagedThreadId}:await stream.ReadAsync前");
            // 1. 异步读取，不阻塞
            int bytesRead = await stream.ReadAsync(buf, 0, buf.Length);
            Console.WriteLine($"{DateTime.Now}--{Thread.CurrentThread.ManagedThreadId}:await stream.ReadAsync后");
            if (bytesRead == 0)
            {
                Console.WriteLine(DateTime.Now + "断开循环");
                break; // 断开连接
            }

            // 2. 将数据塞入处理器
            hunterLabReceiver.AppendData(buf, bytesRead);

            // 3. 循环尝试取出所有完整的包
            HunterPacket packet;
            while ((packet = hunterLabReceiver.TryGetNextPacket()) != null)
            {

                // 4. 业务逻辑处理
                if (packet.IsChecksumValid)
                {
                    Console.WriteLine(DateTime.Now+":find-->nowMsgWaitEvent");
                    if (nowMsgWaitEvent != null&&packet.MsgNo == nowMsgWaitEvent.Item1)
                    {
                        nowMsgWaitEvent.Item3[0] = packet;
                        nowMsgWaitEvent.Item2.Set();
                    }
                    else
                    {
                        OnDataReceived?.Invoke(Tuple.Create("HT.Tcp_AsynchData", packet));
                    }
                    Console.WriteLine($"收到数据: MsgNo={packet.MsgNo}, Data={packet.Parameters}");
                }
                else
                    OnDataReceived?.Invoke(Tuple.Create("HT.Tcp_Message", packet));
                Console.WriteLine($"收到原始数据: MsgNo={packet.MsgNo}, Data={packet.RawString}");
            }
        }
        Console.WriteLine(DateTime.Now + "获取循环停止");
    }
   public Action<Tuple<string, HunterPacket>> OnDataReceived;
    /// <summary>
    /// 断开TCP连接
    /// </summary>
    public void Disconnect()
    {
        try
        {
            listenLoopCTS?.Cancel();
            listenLoopCTS?.Dispose();
            _streamReader?.Dispose();
            _streamWriter?.Dispose();
            _networkStream?.Dispose();
            _tcpClient?.Close();
            InfoOutput("TCP连接已断开");
        }
        catch (Exception ex)
        {
            InfoOutput($"断开连接异常：{ex.Message}");
        }
        finally
        {
            _tcpClient = new TcpClient(); // 重置客户端，支持重新连接
        }
    }
    /// <summary>
    /// 计算协议校验和（Header+Parameters块所有字符的16位模和）
    /// </summary>
    public static string CalculateChecksum(string headerAndParams)
    {
        ushort checksum = 0;
        foreach (char c in headerAndParams)
        {
            checksum += c;
        }
        // 转为4位16进制ASCII字符串（不足补0）
        return checksum.ToString("X4").PadLeft(4, '0');
    }
    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        Disconnect();
        _tcpClient?.Dispose();
    }

    /// <summary>
    /// 校验TCP连接状态
    /// </summary>
    private void ValidateConnection()
    {
        if (!_tcpClient.Connected || _networkStream == null)
            throw new InvalidOperationException("TCP连接未建立或已断开，请先调用ConnectAsync()");
    }
}


