using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace Dobo.Appl.HunterCmd;




    // ==========================================
    // 1. 基础数据类型转换 (根据文档 "Numerical Data Types")
    // ==========================================
    public static class HunterDataConverter
    {
        // 浮点数: IEEE 754 32-bit, 8 hex characters, MSB first
        public static float HexToFloat(string hex)
        {
            if (hex.Length != 8) throw new ArgumentException("Float hex string must be 8 characters.");
            uint num = uint.Parse(hex, NumberStyles.HexNumber);
            byte[] bytes = BitConverter.GetBytes(num);
            // 架构如果是小端序，需要翻转，因为文档暗示是大端序(Network Byte Order)传输的字符串
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
            return BitConverter.ToSingle(bytes, 0);
        }

        public static string FloatToHex(float value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
            return BitConverter.ToString(bytes).Replace("-", "");
        }

        // 整数转换
        public static int HexToInt(string hex)
        {
            return int.Parse(hex, NumberStyles.HexNumber);
        }

        public static string IntToHex(int value, int length)
        {
            return value.ToString("X" + length);
        }

        // 字符串: 12-bit length followed by chars
        public static string ParseString(string data, ref int currentIndex)
        {
            // 读取长度 (3 chars -> 12 bits)
            string lenHex = data.Substring(currentIndex, 3);
            int length = HexToInt(lenHex);
            currentIndex += 3;

            // 读取内容
            string content = data.Substring(currentIndex, length);
            currentIndex += length;
            return content;
        }
    }

    // ==========================================
    // 2. 消息结构封装
    // ==========================================
    public class MessagePacket
    {
        public const char SOH = '\x01'; // Data/Control
        public const char ENQ = '\x05'; // Ack/Nak

        public char Class { get; set; }
        public int Count { get; set; } // Length of Parameters block or Error Code
        public int MsgNo { get; set; } // 1-7
        public char Type { get; set; } // Command identifier
        public string Data { get; set; } // Payload ASCII Hex

        public bool IsValid { get; set; } = false;

        // 构建发送消息
        public string BuildMessage()
        {
            StringBuilder sb = new StringBuilder();

            // Header
            sb.Append(Class);

            // Parameters Block (Type + Data)
            string parameters = Type + Data;

            // Count logic: Length of parameters block
            sb.Append(HunterDataConverter.IntToHex(parameters.Length, 3));
            sb.Append(MsgNo.ToString()); // 1 char

            // Append Parameters
            sb.Append(parameters);

            // Calculate Checksum: sum modulo 16-bit of characters in header and parameters
            // Header chars (Class + Count + MsgNo) + Parameters chars
            string contentToCheck = sb.ToString();
            int sum = 0;
            foreach (char c in contentToCheck) sum += c;
            string checksum = (sum % 65536).ToString("X4"); // 4 chars

            sb.Append(checksum);

            return sb.ToString();
        }
    }

    // ==========================================
    // 3. 测量结果模型 (针对 'H' 命令响应)
    // ==========================================
    public class PhotometricDataResponse
    {
        public string StatusHex { get; set; }
        public List<string> StatusFlags { get; private set; } = new List<string>();
        public List<float> SpectralData { get; set; } = new List<float>(); // 31 points (400-700nm)
        public float Distance { get; set; }

        public PhotometricDataResponse(string payload)
        {
            // 解析 payload (对应 PARAMETERS block 的 data 部分)
            // 根据文档 "STAP - Single measurement"
            // Layout: STATUS[4] + DATA[248] + DISTANCE[8]

            if (payload.Length < 4) return;

            int ptr = 0;

            // 1. STATUS [4 chars]
            StatusHex = payload.Substring(ptr, 4);
            ParseStatus(StatusHex);
            ptr += 4;

            // 如果 Status 是 FFFF，说明没有数据
            if (StatusHex == "FFFF") return;

            // 2. DATA [248 chars] -> 31 floats (8 chars each)
            // 400nm to 700nm @ 10nm interval = 31 points
            if (payload.Length >= ptr + 248)
            {
                for (int i = 0; i < 31; i++)
                {
                    string floatHex = payload.Substring(ptr, 8);
                    SpectralData.Add(HunterDataConverter.HexToFloat(floatHex));
                    ptr += 8;
                }
            }

            // 3. DISTANCE [8 chars]
            if (payload.Length >= ptr + 8)
            {
                string distHex = payload.Substring(ptr, 8);
                Distance = HunterDataConverter.HexToFloat(distHex);
            }
        }

        private void ParseStatus(string hex)
        {
            int status = int.Parse(hex, NumberStyles.HexNumber);
            if (status == 0xFFFF) StatusFlags.Add("Photometric data not available");
            if ((status & 0x4000) != 0) StatusFlags.Add("Dark scan fail");
            if ((status & 0x2000) != 0) StatusFlags.Add("Signal scan fail");
            if ((status & 0x1000) != 0) StatusFlags.Add("Monitor signal low");
            if ((status & 0x0002) != 0) StatusFlags.Add("Range too close");
            if ((status & 0x0001) != 0) StatusFlags.Add("Range too far");
            if (StatusFlags.Count == 0 && status != 0) StatusFlags.Add("OK");
        }
    }

    // ==========================================
    // 4. TCP 客户端
    // ==========================================
    public class SpectraTrendClient : IDisposable
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private int _msgNoCounter = 1;
        private const int ReadTimeoutMs = 3000;

        public bool Connect(string ip, int port)
        {
            try
            {
                _client = new TcpClient();
                _client.Connect(ip, port);
                _stream = _client.GetStream();
                _stream.ReadTimeout = ReadTimeoutMs;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Connection failed: {ex.Message}");
                return false;
            }
        }

        public void Disconnect()
        {
            _stream?.Close();
            _client?.Close();
        }

        /// <summary>
        /// 发送获取光度数据的命令 ('H')
        /// </summary>
        public PhotometricDataResponse RequestSingleMeasurement()
        {
            // 构建 'H' 命令
            // 根据文档 "HOST - Single measurement":
            // Parameters: 'H'[1] + DISTANCE[8] + INTERVAL[4] + TRIGGER[1] + STATIONARY[1]
            // 这里我们演示最简单的单次测量，根据文档 STAP 返回的数据结构推断发送什么。
            // 文档中 "HOST Single measurement" 表格显示需要发送具体的参数。
            // 假设我们发送默认值：Distance=0, Interval=0, Trigger=0, Stationary=1 (Stop belt)

            string distHex = HunterDataConverter.FloatToHex(0.0f); // 8 chars
            string intervalHex = HunterDataConverter.IntToHex(0, 4); // 4 chars
            string triggerHex = "0"; // 1 char
            string stationaryHex = "1"; // 1 char

            string cmdData = distHex + intervalHex + triggerHex + stationaryHex;

            MessagePacket request = new MessagePacket
            {
                Class = MessagePacket.SOH,
                MsgNo = GetNextMsgNo(),
                Type = 'H', // Type is part of parameters block in doc structure
                Data = cmdData
            };

            MessagePacket response = SendAndReceive(request);

            if (response != null && response.Class == MessagePacket.SOH && response.Type == 'H')
            {
                return new PhotometricDataResponse(response.Data);
            }
            else if (response != null && response.Class == MessagePacket.ENQ)
            {
                Console.WriteLine($"Received ENQ (Error/Ack). Code: {response.Count}");
                return null;
            }

            return null;
        }

        private int GetNextMsgNo()
        {
            int no = _msgNoCounter++;
            if (_msgNoCounter > 7) _msgNoCounter = 1;
            return no;
        }

        private MessagePacket SendAndReceive(MessagePacket request)
        {
            try
            {
                // 1. 发送
                string rawMsg = request.BuildMessage();
                byte[] buffer = Encoding.ASCII.GetBytes(rawMsg);

                // 清空读取缓冲区残留
                while (_stream.DataAvailable) _stream.ReadByte();

                _stream.Write(buffer, 0, buffer.Length);
                Console.WriteLine($"TX: {rawMsg}");

                // 2. 接收
                // 读取 Header (Class[1] + Count[3] + MsgNo[1] = 5 bytes)
                byte[] headerBuf = ReadExactBytes(5);
                if (headerBuf == null) return null;

                char msgClass = (char)headerBuf[0];
                string countHex = Encoding.ASCII.GetString(headerBuf, 1, 3);
                int count = int.Parse(countHex, NumberStyles.HexNumber);
                int msgNo = headerBuf[4] - '0';

                // 计算剩余需要读取的长度
                // 如果是 SOH: 剩余长度 = Count (Parameters) + 4 (Checksum)
                // 如果是 ENQ: Count 是 Error Code，剩余长度 = 4 (Checksum) ? 
                // 文档中 ENQ 的 Count 定义为 Error Code，但 Checksum 定义是对 "Header and Parameters" 求和。
                // ENQ 没有 Parameters block。通常 ENQ 消息结构可能不同，但根据文档通用格式：
                // [Class][Count][MsgNo]... [Checksum]

                int remainingBytes = 0;
                string parametersBlock = "";

                if (msgClass == MessagePacket.SOH)
                {
                    remainingBytes = count + 4; // Parameters + Checksum
                }
                else
                {
                    // ENQ 只有 Header 和 Checksum? 文档说 Checksum 是 header+params 的和。
                    // 假设 ENQ 后面紧跟 Checksum
                    remainingBytes = 4;
                }

                byte[] bodyBuf = ReadExactBytes(remainingBytes);
                if (bodyBuf == null) return null;

                string fullMsgStr = Encoding.ASCII.GetString(headerBuf) + Encoding.ASCII.GetString(bodyBuf);
                Console.WriteLine($"RX: {fullMsgStr}");

                // 校验和验证
                string receivedChecksum = Encoding.ASCII.GetString(bodyBuf, bodyBuf.Length - 4, 4);
                string msgToCheck = fullMsgStr.Substring(0, fullMsgStr.Length - 4);

                int sum = 0;
                foreach (char c in msgToCheck) sum += c;
                string calcChecksum = (sum % 65536).ToString("X4");

                if (receivedChecksum != calcChecksum)
                {
                    Console.WriteLine($"Checksum Error: Exp {calcChecksum}, Got {receivedChecksum}");
                    return null;
                }

                // 解析结果
                MessagePacket response = new MessagePacket
                {
                    Class = msgClass,
                    Count = count,
                    MsgNo = msgNo,
                    IsValid = true
                };

                if (msgClass == MessagePacket.SOH)
                {
                    // Parameters block = Type[1] + Data[...]
                    string paramStr = Encoding.ASCII.GetString(bodyBuf, 0, count);
                    response.Type = paramStr[0];
                    if (paramStr.Length > 1)
                    {
                        response.Data = paramStr.Substring(1);
                    }
                    else
                    {
                        response.Data = "";
                    }
                }

                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Comms Error: {ex.Message}");
                return null;
            }
        }

        private byte[] ReadExactBytes(int length)
        {
            byte[] buffer = new byte[length];
            int offset = 0;
            int remaining = length;

            while (remaining > 0)
            {
                int read = _stream.Read(buffer, offset, remaining);
                if (read == 0) return null; // End of stream
                offset += read;
                remaining -= read;
            }
            return buffer;
        }

        public void Dispose()
        {
            Disconnect();
        }
    }

    // ==========================================
    // 5. 主程序示例
    // ==========================================
   public class Program
    {
       public static void Main2(string[] args)
        {
            string ipAddress = "192.168.0.55"; // 替换为实际设备IP
            int port = 10001; // 替换为实际端口 (通常是 10001, 502 等)

            Console.WriteLine($"Connecting to {ipAddress}:{port}...");

            using (var client = new SpectraTrendClient())
            {
                // 注意：在没有真实设备的情况下，这里会超时
                if (client.Connect(ipAddress, port))
                {
                    Console.WriteLine("Connected.");

                    // 请求一次测量
                    Console.WriteLine("Requesting Measurement...");
                    var result = client.RequestSingleMeasurement();

                    if (result != null)
                    {
                        Console.WriteLine("--- Measurement Result ---");
                        Console.WriteLine($"Status Hex: {result.StatusHex}");
                        Console.WriteLine($"Flags: {string.Join(", ", result.StatusFlags)}");
                        Console.WriteLine($"Distance: {result.Distance}");

                        if (result.SpectralData.Count > 0)
                        {
                            Console.WriteLine($"Spectral Data (First 5):");
                            for (int i = 0; i < Math.Min(5, result.SpectralData.Count); i++)
                            {
                                Console.WriteLine($"  {400 + i * 10}nm: {result.SpectralData[i]:F2}%");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Failed to get valid measurement.");
                    }
                }
                else
                {
                    Console.WriteLine("Could not connect. (This is expected if no device is present)");

                    // --- 模拟测试 ---
                    Console.WriteLine("\n--- Running Simulation Test ---");
                    TestParsingLogic();
                }
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        // 这是一个用于验证解析逻辑的测试方法，不需要物理设备
        static void TestParsingLogic()
        {
            // 构造一个模拟的响应消息
            // HEADER: SOH(1), Count(0x105=261), MsgNo(1)
            // PARAMS: Type('H'), Status(0000), Data(31个1.0f), Distance(10.5f)

            StringBuilder sb = new StringBuilder();
            sb.Append("H"); // Type
            sb.Append("0000"); // Status OK

            // Data: 31个浮点数 (1.0 = 3F800000)
            for (int i = 0; i < 31; i++) sb.Append("3F800000");

            // Distance: 10.5 (0x41280000)
            sb.Append("41280000");

            string paramsBlock = sb.ToString();
            string countHex = HunterDataConverter.IntToHex(paramsBlock.Length, 3);

            string rawMsgPart = $"\x01{countHex}1{paramsBlock}";

            // 计算校验和
            int sum = 0;
            foreach (char c in rawMsgPart) sum += c;
            string checksum = (sum % 65536).ToString("X4");

            string fullMsg = rawMsgPart + checksum;

            Console.WriteLine($"Simulated Raw Message: {fullMsg.Substring(0, 20)}...[truncated]");

            // 手动解析
            PhotometricDataResponse response = new PhotometricDataResponse(paramsBlock.Substring(1)); // 去掉 Type 'H'

            Console.WriteLine($"Parsed Distance: {response.Distance} (Expected 10.5)");
            Console.WriteLine($"Parsed Spectral[0]: {response.SpectralData[0]} (Expected 1.0)");
        }
    }

