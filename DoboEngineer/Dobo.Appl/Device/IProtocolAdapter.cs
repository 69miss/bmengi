using Dobo.Appl.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dobo.Appl.Device;

public interface IProtocolAdapter:  IDisposable
{
    /// <summary>
    /// 连接配置字符串 (如 "COM1,9600" 或 "192.168.1.10:502")
    /// </summary>
    string ConnectionString { get; }

    /// <summary>
    /// 当前连接状态
    /// </summary>
    bool IsConnected { get; }

    string ProtocolName { get; }

    /// <summary>
    /// 连接状态改变事件
    /// </summary>
    event EventHandler<bool> ConnectionStateChanged;

    /// <summary>
    ///  收到主动上报数据事件
    /// </summary>
    event EventHandler<Tuple<string,object>> DataReceived;
    /// <summary>
    /// 打开连接
    /// </summary>
    Task<bool> ConnectAsync();

    /// <summary>
    /// 关闭连接
    /// </summary>
    Task DisconnectAsync();

    /// <summary>
    /// 读取单个数据
    /// </summary>
    /// <typeparam name="T">目标类型 (int, float, bool, etc.)</typeparam>
    /// <param name="address">地址标识 (如 "40001", "ns=2;s=Demo.Tag1", "CAN:0x123")</param>
    Task<T> ReadAsync<T>(string address);

    /// <summary>
    /// 写入单个数据
    /// </summary>
    Task<bool> WriteAsync<T>(string address, T value);
    /// <summary>
    /// (可选) 批量读取，用于提高Modbus等协议的效率
    /// </summary>
    [Obsolete("使用启示地址，读数组更符合实际实现")]
    Task<IDictionary<string, T>> ReadBatchAsync<T>(IEnumerable<string> addresses) { 
     throw new NotImplementedException();
    }

    
}
public interface IProtocolAdapter0 : IProtocolAdapter, IDisposable {
    Task<object> ReadAsync(string address, TypeCode typeCode);
    Task<bool> WriteAsync(string address, IConvertible value);

    Task<T[]> ReadBatchAsync<T>(string addresse, int len);
    Task<IDictionary<string, object>> ReadBatchAsync(IDictionary<string, TypeCode> dict);
    Task<bool> WriteBatchAsync(IDictionary<string, object> addresses, int batchNumber);
}

