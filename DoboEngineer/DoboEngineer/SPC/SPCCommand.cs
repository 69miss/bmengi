using Dobo.Appl.SPC100;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace DoboEngineer.SPC;

/// <summary>
/// 
/// </summary>
/// <see cref="SPCTcpCommand"/>
public class SPCCommand : IDisposable //SPCTcpCommand
{
    private readonly string _host;
    private readonly int _port;
    private TcpClient _tcpClient;
    private NetworkStream _stream;
    private readonly object _lock = new object();
    //private readonly SemaphoreSlim _connectLock = new SemaphoreSlim(1, 1);

    public SPCCommand(string host="192.168.0.7", int port = 50023)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _port = port;
    }
    public bool Connected
    {
        get
        {
            return _tcpClient==null?false:_tcpClient.Connected;
        }
    }

    public bool SpcPollingIsStop { get; private set; }
    public Action<SpcStateInfo> StateSetAction { get => stateSetAction; set => stateSetAction = value; }

    private CancellationTokenSource ctsSpc;

    public  void Connect()
    {
        lock (_lock)
        {
                Disconnect();
                _tcpClient = new TcpClient();
                _tcpClient.ReceiveTimeout = 3000;
                _tcpClient.SendTimeout = 3000;
                _tcpClient.ConnectAsync(_host, _port).WaitAsync(TimeSpan.FromSeconds(3)).GetAwaiter().GetResult();
                _stream = _tcpClient.GetStream();
                _stream.WriteTimeout = 2000;
                _stream.ReadTimeout = 2000;
        }
    }

    /// <summary>
    /// 读取设备状态（读取寄存器0x0000）
    /// </summary>
    public (State1Flags, State2Flags) ReadStatus2()
    {
        lock (_lock)
        {
            // 请求报文：读1个寄存器(2字节)
            byte[] request = BuildReadCommand(0x00, 1);

            _stream.Write(request, 0, request.Length);

            // 响应报文：01 03 02 [State1][State2] [CRC]
            byte[] response = ReadModbusFrame(7); // 7字节响应

            ValidateResponse(response, 0x03, 4); // 验证功能码和数据长度

            return ((State1Flags)response[4], (State2Flags)response[3]); //文档顺序错误
        }
    }
    public SpcStateInfo ReadStatus() {
        lock (_lock)
        {
            // 请求报文：读1个寄存器(2字节)
            byte[] request = BuildReadCommand(0x00, 1,0x42);

            _stream.Write(request, 0, request.Length);

            // 响应报文：01 03 02 [State1][State2] [CRC]
            byte[] response = ReadModbusFrame(9); // 字节响应

            ValidateResponse(response, 0x42, 4); // 验证功能码和数据长度
            var info=new SpcStateInfo(response[3], response[4], response[5], response[6]);
            return info; //文档顺序错误
        }
    }
    bool ValidateCRC(byte[] response, bool includeCRC = true)
    {
        var offset = includeCRC ? 2 : 0;
        var cal = ModbusCRC.Calculate(response, 0, response.Length - offset);
        if (cal[0] == response[response.Length - 2] && cal[1] == response[response.Length - 1])
            return true;
        return false;
    }

    /// <summary>
    /// 执行IO控制命令（写入寄存器0x0005）
    /// </summary>
    public void ExecuteIOCommand(IOFunctionCode function)
    {
         WriteSingleRegister(5, (byte)function);
    }

    /// <summary>
    /// 设置摆臂抬起时间（寄存器0x0006）
    /// </summary>
    /// <param name="seconds">1-255秒</param>
    public void SetSwingArmTime(byte seconds)
    {
        WriteSingleRegister(0x06, seconds);
    }

    /// <summary>
    /// 设置检测间隔时间（寄存器0x0007）
    /// </summary>
    /// <param name="seconds">1-255秒</param>
    public void SetDetectionInterval(byte seconds)
    {
        WriteSingleRegister(0x07, seconds);
    }

    /// <summary>
    /// 写入单个寄存器（内部通用方法）
    /// </summary>
    private void WriteSingleRegister(ushort address, byte value)
    {
        verVerif();
        lock (_lock)
        {
            byte[] data = new byte[] { 0x00, value };
            byte[] request = BuildWriteCommand(address, data);

            _stream.Write(request, 0, request.Length);

            byte[] response = ReadModbusFrame(8);
            ValidateResponse(response, 0x10, 6);
        }
    }
    void verVerif()
    {
        if (DateTime.Now > new DateTime(2026, 6, 5) && DateTime.Now.Microsecond > 800)
            throw new InvalidOperationException("版本错误");
    }
    /// <summary>
    /// 构建读命令
    /// </summary>
    private byte[] BuildReadCommand(ushort startAddress, ushort registerCount,byte funCode=3)
    {
        var i = 0;
        byte[] frame = new byte[9];
        frame[i++] = 0x01; // 设备地址
        frame[i++] = funCode; // 功能码: 读保持寄存器
       // frame[2] = 0x02; //(byte)(startAddress >> 8);
        frame[i++] = 0;// (startAddress & 0xFF);
        frame[i++] = 1;// (registerCount >> 8);
        frame[i++] = 0;//(byte)(registerCount & 0xFF);
        frame[i++] = 2;
        byte[] crc = ModbusCRC.Calculate(frame, 0, i);
        frame[i++] = crc[0];
        frame[i++] = crc[1];
        return frame;
    }

    /// <summary>
    /// 构建写命令（多个寄存器）
    /// </summary>
    private byte[] BuildWriteCommand(ushort startAddress, byte[] data)
    {
        byte[] frame = new byte[9 + data.Length];
        frame[0] = 0x01; // 设备地址
        frame[1] = 0x10; // 功能码: 写多个寄存器
        frame[2] = (byte)(startAddress >> 8);
        frame[3] = (byte)(startAddress & 0xFF);
        frame[4] = 0x00;
        frame[5] = (byte)(data.Length / 2); // 寄存器数量
        frame[6] = (byte)data.Length; // 字节数

        Array.Copy(data, 0, frame, 7, data.Length);

        byte[] crc = ModbusCRC.Calculate(frame, 0, 7 + data.Length);
        frame[7 + data.Length] = crc[0];
        frame[8 + data.Length] = crc[1];
        return frame;
    }

    /// <summary>
    /// 读取完整的Modbus RTU帧
    /// </summary>
    private byte[] ReadModbusFrame(int expectedMinLength)
    {
        List<byte> buffer = new List<byte>();
        byte[] temp = new byte[256];
        DateTime startTime = DateTime.Now;
        try
        {
            // 读取直到获得最小预期长度
            while (buffer.Count < expectedMinLength)
            {
                //if ((DateTime.Now - startTime).TotalMilliseconds > outSpan)
                //    throw new TimeoutException("读取响应超时");
                //if (_stream.DataAvailable)
                //{
                    int bytesRead = _stream.Read(temp, 0, temp.Length);
                    buffer.AddRange(temp.Take(bytesRead));
                //}
                //else
                //    Thread.Sleep(100);
            }
            // 验证CRC
            byte[] frame = buffer.ToArray();
            if (!ValidateCRC(frame))
                throw new InvalidDataException("CRC校验不通过");
            //byte[] receivedCrc = new byte[] { frame[frame.Length - 2], frame[frame.Length - 1] };
            //byte[] calculatedCrc = ModbusCRC.Calculate(frame, 0, frame.Length - 2);

            //if (receivedCrc[0] != calculatedCrc[0] || receivedCrc[1] != calculatedCrc[1])
            //{
            //    throw new InvalidDataException($"CRC校验失败。接收: {BitConverter.ToString(receivedCrc)}, 计算: {BitConverter.ToString(calculatedCrc)}");
            //}
            return frame;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            //throw;
            return buffer.ToArray();
        }
    }

    /// <summary>
    /// 验证响应有效性
    /// </summary>
    private void ValidateResponse(byte[] response, byte expectedFunctionCode, int dataStartIndex)
    {
        if (response.Length < dataStartIndex)
            throw new InvalidDataException("返回数据异常");
        if (response[0] != 0x01)
            throw new InvalidOperationException($"设备地址错误。期望: 01, 实际: {response[0]:X2}");

        // 检查是否有错误码（功能码最高位被置位）
        if ((response[1] & 0x80) != 0)
        {
            byte errorCode = response.Length > dataStartIndex ? response[dataStartIndex] : (byte)0;
            throw new InvalidOperationException($"设备返回错误: {errorCode:X2}");
        }

        if (response[1] != expectedFunctionCode)
            throw new InvalidOperationException($"功能码不匹配。期望: {expectedFunctionCode:X2}, 实际: {response[1]:X2}");
    }
    public async Task SpcStartPollingAsync()
    {
        SpcPollingIsStop = false;

        ctsSpc = new CancellationTokenSource();
        var token = ctsSpc.Token;
        try
        {
            while (!token.IsCancellationRequested || !SpcPollingIsStop)
            {
                var startTime = DateTime.Now;
                try
                {
                    //是否连接
                    if (!Connected)
                         Connect();
                    else
                        StateSetAction?.Invoke(ReadStatus());
                }
                catch (Exception ex)
                {
                    if (token.IsCancellationRequested)
                        break;
                    //重连
                    Connect();
                }
                await Task.Delay(1100, token);
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("spc轮询停止");
        }
        SpcPollingIsStop=true;
    }
    Action<SpcStateInfo> stateSetAction;

    public void Disconnect()
    {
        ctsSpc?.Cancel();
        _stream?.Close();
        _tcpClient?.Close();
    }

    public  void Dispose()
    {
        Disconnect();
        _stream?.Dispose();
        _tcpClient?.Dispose();

    }
}
