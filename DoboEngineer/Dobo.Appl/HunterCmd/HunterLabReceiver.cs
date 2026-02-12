using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Dobo.Appl.HunterCmd;

public class HunterPacket
{
    public byte Class { get; set; }        // 0x01 (SOH) or 0x05 (ENQ)
    public int ParamLength { get; set; }   // 数据长度
    public int MsgNo { get; set; }         // 消息号
    public string Parameters { get; set; } // 数据内容
    public bool IsChecksumValid { get; set; }
    public string RawString { get; set; }  // 调试用
                                           
    public char CommandType { get; set; } // 响应对应的命令类型（如'#'=Capabilities）
    public string ResponseDataContent { get; set; } = string.Empty; // 解析后的响应数据
}

public class HunterLabReceiver
{
    private const byte PREFIX_BYTE = 0xFF;
    private const byte CLASS_SOH = 0x01;
    private const byte CLASS_ENQ = 0x05;

    // 协议结构长度定义
    // Prefix[1] + Class[1] + Count[3] + MsgNo[1] = 6 bytes
    private const int FULL_HEADER_LEN = 6;

    // 校验和长度
    private const int CHECKSUM_LEN = 4;

    // 内部缓存
    private List<byte> _buffer = new List<byte>();

    /// <summary>
    /// 接收数据入口
    /// </summary>
    public void AppendData(byte[] newData, int length)
    {
        if (length > 0)
        {
            _buffer.AddRange(newData.Take(length));
        }
    }

    List<byte> bufferRemoveRecord=new List<byte>();
    void BufferRemoveAt(int index)
    {
        bufferRemoveRecord.Add(_buffer[index]);
        _buffer.RemoveAt(0);
    }
   public Action<byte[]> BufferDiscardRecord;
    /// <summary>
    /// 尝试分包
    /// </summary>
    public HunterPacket TryGetNextPacket()
    {
        // 1. 寻找合法的双字节包头 (00 01 或 00 05)
        // 缓存里至少要有2个字节才能判断是不是包头
        while (_buffer.Count >= 2)
        {
            // 检查第一个字节是否是 0x00
            if (_buffer[0] == PREFIX_BYTE)
            {
                // 检查第二个字节是否是合法的 Class (0x01 或 0x05)
                byte secondByte = _buffer[1];
                if (secondByte == CLASS_SOH || secondByte == CLASS_ENQ)
                {
                    // 找到了合法的包头，跳出循环进入下一步解析
                    if (bufferRemoveRecord.Count > 0)
                    {
                        BufferDiscardRecord?.Invoke(bufferRemoveRecord.ToArray());
                        bufferRemoveRecord.Clear();
                    }
                    break;
                }
            }

            // 如果不匹配，移除第一个字节，继续往后滑窗寻找
            // 例如数据是: 00 00 01 ... 
            // 第一次循环: [0]=00, [1]=00 (不匹配) -> 移除第一个00
            // 第二次循环: [0]=00, [1]=01 (匹配!) -> break
            BufferRemoveAt(0);
        }

        // 2. 再次检查长度，如果数据被移除完了，或者还没收够头部长度
        if (_buffer.Count < FULL_HEADER_LEN)
        {
            return null; // 等待更多数据
        }

        // 3. 解析长度字段 Count
        // 结构: [00] [Class] [Count1] [Count2] [Count3] [MsgNo] ...
        // 索引:  0     1        2        3        4        5
        // 所以 Count 从索引 2 开始，长度 3
        int paramPayloadLength = 0;
        try
        {
            string countHex = Encoding.ASCII.GetString(_buffer.ToArray(), 2, 3);
            paramPayloadLength = Convert.ToInt32(countHex, 16);
        }
        catch
        {
            // 解析长度失败（说明这不是个正经包头），移除 0x00，下次重新找
            BufferRemoveAt(0);
            return null;
        }

        // 4. 计算包的总长度
        // Total = Prefix(1) + Class(1) + Count(3) + MsgNo(1) + Body(N) + Checksum(4)
        // 也就是 = FULL_HEADER_LEN(6) + N + 4
        int totalPacketLength = FULL_HEADER_LEN + paramPayloadLength + CHECKSUM_LEN;

        // 5. 检查缓存是否够长
        if (_buffer.Count < totalPacketLength)
        {
            return null; // 数据没收全
        }

        // --- 提取完整包 ---
        byte[] packetBytes = _buffer.GetRange(0, totalPacketLength).ToArray();

        // 从缓存移除
        _buffer.RemoveRange(0, totalPacketLength);

        // 6. 解析对象
        return ParsePacket(packetBytes, paramPayloadLength);
    }

    private HunterPacket ParsePacket(byte[] rawData, int paramLen)
    {
        try
        {
            // rawData 结构: [00] [Class] [Cnt] [Cnt] [Cnt] [MsgNo] [Data...] [Check...]

            var packet = new HunterPacket
            {
                Class = rawData[1], // Index 1 is Class
                ParamLength = paramLen,
                MsgNo = rawData[5] - '0', // Index 5 is MsgNo
                                          // Data 开始于 Index 6
                Parameters = Encoding.ASCII.GetString(rawData, 6, paramLen),
                RawString = Encoding.ASCII.GetString(rawData) // 注意：0x00转String可能显示不出来
               
            };
            packet.CommandType = packet.Parameters[0];
            packet.ResponseDataContent = packet.Parameters.Remove(0, 1);
            // --- 校验和计算 ---
            // 文档: "sum modulo 16-bit of characters in header and parameters blocks"
            // 通常这意味着包含 Class, Count, MsgNo, Data。
            // *大概率* 不包含开头的 0x00 填充符。
            // 计算范围：从 Index 1 (Class) 开始，一直到 Checksum 之前

            int checksumStartIndex = rawData.Length - CHECKSUM_LEN; // 校验和字段的起始位置
            string receivedChecksum = Encoding.ASCII.GetString(rawData, checksumStartIndex, CHECKSUM_LEN);

            int sum = 0;
            // 跳过 Index 0 (Prefix)，从 Index 1 开始累加
            for (int i = 1; i < checksumStartIndex; i++)
            {
                sum += rawData[i];
            }
            string calculatedChecksum = (sum % 65536).ToString("X4");

            packet.IsChecksumValid = (receivedChecksum == calculatedChecksum);

            return packet;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Error] Parse failed: {ex.Message}");
            return null;
        }
    }

   public static  async Task RealTcpLoop(Stream stream)
    {
        var receiver = new HunterLabReceiver();
        byte[] buf = new byte[4096];

        while (true)
        {
            // 1. 异步读取，不阻塞
            int bytesRead = await stream.ReadAsync(buf, 0, buf.Length);
            if (bytesRead == 0) break; // 断开连接

            // 2. 将数据塞入处理器
            receiver.AppendData(buf, bytesRead);

            // 3. 循环尝试取出所有完整的包
            HunterPacket packet;
            while ((packet = receiver.TryGetNextPacket()) != null)
            {
                // 4. 业务逻辑处理
                if (packet.IsChecksumValid)
                {
                    Console.WriteLine($"收到消息: MsgNo={packet.MsgNo}, Data={packet.Parameters}");
                }
            }
        }
    }
}

