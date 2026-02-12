using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dobo.Appl.HunterCmd;
public class SpectraParser
    {
        readonly string responseString;
        readonly StringReader responseSr;
        readonly StringWriter commandSW;

        public SpectraParser(string respStr = null)
        {
            responseString = respStr;
            if (respStr == null)
                commandSW = new StringWriter();
            else
                responseSr = new StringReader(responseString);
        }
        public static SpectraParser Read(string respStr)
        {
            return new SpectraParser(respStr);
        }
        public static SpectraParser Write() { 
        return new SpectraParser() {};
        }
        public bool isRead { get => responseSr != null; }
        void writeCmdChar(string chars)
        {
            commandSW.Write(chars);
        }
       public string readCmdChar(int count)
        {
            var re = new char[count];
            responseSr.Read(re, 0, count);
           
            var str= new string(re);
            pos += count;
            //Console.WriteLine($"位置:{pos}:{str}");
            return str;
        }
        int pos = 0;
        public char readCmdChar()
        {
            //Console.WriteLine(++pos);
            return (char)responseSr.Read();
        }
        //CommandString
        // ResponseString
        //BooleanItem
        //NibbleItem
        //ByteItem
        //ShortItem
        //IntegerItem
        //UnsignedItem
        //LongItem

        /// <summary>
        /// 0 and 15,1字符串
        /// </summary>
        public sbyte NibbleItem { get => HexAsciiToNum<sbyte>(readCmdChar(1));
            set => writeCmdChar(value.ToString("X1"));
        }
        /// <summary>
        /// 0 and 255,2字符串
        /// </summary>
        public byte ByteItem { 
            get => HexAsciiToNum<byte>(readCmdChar(2));
            set => writeCmdChar(value.ToString("X2"));
        }
        /// <summary>
        /// 0 and 4095,3字符串
        /// </summary>
        public short ShortItem
        {
            get => HexAsciiToNum<short>(readCmdChar(3));
            set => writeCmdChar(value.ToString("X3"));
        }
        /// <summary>
        /// 有符号32767，4字符
        /// </summary>
        public short IntegerItem { 
            get { return HexAsciiToNum<short>(readCmdChar(4),false); }
            set { writeCmdChar(Int16ToHexAscii(value)); }
        }
        /// <summary>
        /// 0 and 65535，4字符
        /// </summary>
        public ushort UnsignedItem
        {
            get { return HexAsciiToNum<ushort>(readCmdChar(4)); }
            set { writeCmdChar(UInt16ToHexAscii(value)); }
        }
        /// <summary>
        /// 浮点，8字符
        /// </summary>
        public float SingleItem
        {
            get
            {
                return HexAsciiToFloat(readCmdChar(8));
            }
            set
            {
                writeCmdChar(FloatToHexAscii(value));
            }
        }
        public int LongItem {
            get { return HexAsciiToNum<int>(readCmdChar(8)); }
            set { writeCmdChar(BaseToHexAscii(value)); }
        }
        //LiteralItem
        //StringItem
        public string StringItem
        {
            get
            {
                var len = HexAsciiToNum<short>(readCmdChar(3));
                return readCmdChar(len);
            }
            set
            {
                writeCmdChar(EncodeString(value));
            }
        }
        public float[] get_SingleArray(short size)
        {
            var arr = new float[size];
            for (int i = 0; i < size; i++)
            {
                arr[i] = SingleItem;
            }
            return arr;
        }
        
        
        /// <summary>
        /// 16位整数转换为协议16进制ASCII字符串（大端，每4位1个字符）
        /// </summary>
        public static string Int16ToHexAscii(short value,byte? strLen=null)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes); // 转为大端序
           
            return BitConverter.ToString(bytes).Replace("-", "").ToUpper();
        }
        /// <summary>
        /// 16位整数转换为协议16进制ASCII字符串（大端，每4位1个字符）
        /// </summary>
        public static string UInt16ToHexAscii(ushort value, byte? strLen = null)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes); // 转为大端序

            return BitConverter.ToString(bytes).Replace("-", "").ToUpper();
        }
        public static string Int32ToHexAscii(int value, byte? strLen = null)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes); // 转为大端序

            return BitConverter.ToString(bytes).Replace("-", "").ToUpper();
        }
        public static string BaseToHexAscii(IConvertible value, byte? len=null)
        {

            switch (value.GetTypeCode())
            {
                //case TypeCode.Empty:
                //    break;
                //case TypeCode.Object:
                //    break;
                //case TypeCode.DBNull:
                //    break;
                case TypeCode.Boolean:
                    return (bool)value ? "1" : "0";
                case TypeCode.Char:
                    return value + "";
                case TypeCode.SByte:
                    len = len ?? 1;
                    return ((sbyte)value).ToString("X" + len);
                case TypeCode.Byte:
                    len = len ?? 2;
                    return ((byte)value).ToString("X" + len);
                case TypeCode.Int16:
                    len = len ?? 3;
                    return ((byte)value.ToByte(null)).ToString("X" + len);
                case TypeCode.UInt16:
                    len = len ?? 4;
                    return UInt16ToHexAscii((ushort)value);
                case TypeCode.Int32:
                    len = len ?? 8;
                    return Int32ToHexAscii((int)value);
                //case TypeCode.UInt32:
                //    break;
                //case TypeCode.Int64:
                //    break;
                //case TypeCode.UInt64:
                //    break;
                case TypeCode.Single:
                    len = len ?? 8;
                    return FloatToHexAscii((float)value);
                //case TypeCode.Double:
                //    break;
                //case TypeCode.Decimal:
                //    break;
                //case TypeCode.DateTime:
                //break;
                case TypeCode.String:
                    return EncodeString((string)value);
                default:
                    throw new ArgumentException("不支持的数据类型");
            }
        }
        public static T HexAsciiToNum<T>(string hexAscii, bool isUnsigned = true) where T : struct {
            return (T)HexAsciiToNum(hexAscii,isUnsigned);
        }
        public static IConvertible HexAsciiToNum(string hexAscii,bool isUnsigned=true)
        {
           if(hexAscii.Length==1)
                return sbyte.Parse(hexAscii, System.Globalization.NumberStyles.HexNumber);
           if(hexAscii.Length==2)
                return byte.Parse(hexAscii, System.Globalization.NumberStyles.HexNumber);
           if(hexAscii.Length==3)
                return short.Parse(hexAscii, System.Globalization.NumberStyles.HexNumber);
            if (hexAscii.Length == 4)
            {
                var bytes = new byte[2];
                for (int i = 0; i < bytes.Length; i++)
                {
                    bytes[i] = Convert.ToByte(hexAscii.Substring(i * 2, 2), 16);
                }
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(bytes); // 转为C#默认小端序
                if (isUnsigned)
                    return BitConverter.ToUInt16(bytes, 0);
                return BitConverter.ToInt16(bytes, 0);
            }
            if (hexAscii.Length == 8) {
                byte[] bytes = new byte[4];
                for (int i = 0; i < 4; i++)
                {
                    bytes[i] = Convert.ToByte(hexAscii.Substring(i * 2, 2), 16);
                }
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(bytes); // 转为C#默认小端序
                return BitConverter.ToInt32(bytes, 0);
            }
            throw new ArgumentException("字符串长度无法转换为整形");
        }
        /// <summary>
        /// 协议16进制ASCII字符串转换为32位整数
        /// </summary>
        public static int HexAsciiToInt32(string hexAscii)
        {
            if (hexAscii.Length != 8)
                throw new ArgumentException("32位整数的16进制ASCII字符串必须为8个字符", nameof(hexAscii));

            byte[] bytes = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                bytes[i] = Convert.ToByte(hexAscii.Substring(i * 2, 2), 16);
            }
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes); // 转为C#默认小端序
            return BitConverter.ToInt32(bytes, 0);
        }

        /// <summary>
        /// 32位IEEE浮点数转换为协议16进制ASCII字符串
        /// </summary>
        public static string FloatToHexAscii(float value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return BitConverter.ToString(bytes).Replace("-", "").ToUpper();
        }

        /// <summary>
        /// 协议16进制ASCII字符串转换为32位IEEE浮点数
        /// </summary>
        public static float HexAsciiToFloat(string hexAscii)
        {
            //return (float)ParseFloatFromAscii(hexAscii);
            if (hexAscii.Length != 8)
                throw new ArgumentException("32位浮点数的16进制ASCII字符串必须为8个字符", nameof(hexAscii));

            byte[] bytes = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                bytes[i] = Convert.ToByte(hexAscii.Substring(i * 2, 2), 16);
            }
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return BitConverter.ToSingle(bytes, 0);
        }
        /// <summary>
        /// 将传输的ASCII十六进制字符串解析为32位IEEE浮点数
        /// </summary>
        /// <param name="asciiHex">传输的ASCII字符串（长度必须为8）</param>
        /// <returns>解析后的浮点数（double类型，兼容高精度需求）</returns>
        /// <exception cref="ArgumentNullException">输入字符串为null</exception>
        /// <exception cref="ArgumentOutOfRangeException">输入字符串长度不为8</exception>
        /// <exception cref="ArgumentException">输入包含无效十六进制字符</exception>
        public static double ParseFloatFromAscii(string asciiHex)
        {
            // 1. 输入合法性校验（文档要求：32位浮点数对应8个ASCII字符）
            if (asciiHex == null)
                throw new ArgumentNullException(nameof(asciiHex), "输入ASCII字符串不能为null");
            if (asciiHex.Length != 8)
                throw new ArgumentOutOfRangeException(
                    nameof(asciiHex),
                    $"输入ASCII字符串长度必须为8（32位浮点数=8个nibble），当前长度：{asciiHex.Length}"
                );

            // 2. ASCII字符串转十六进制字节数组（文档规则：每个nibble对应1个ASCII字符，高位nibble先传）
            byte[] floatBytes = new byte[4]; // 32位浮点数=4字节
            for (int i = 0; i < 8; i += 2)
            {
                // 每2个ASCII字符组成1个字节（第1个字符=高4位，第2个=低4位）
                byte highNibble = ConvertAsciiToNibble(asciiHex[i]);
                byte lowNibble = ConvertAsciiToNibble(asciiHex[i + 1]);
                floatBytes[i / 2] = (byte)((highNibble << 4) | lowNibble);
            }

            // 3. 处理字节序（文档为大端传输，需适配系统字节序）
            if (BitConverter.IsLittleEndian)
            {
                // 小端系统需反转字节数组，确保解析为正确的32位数值
                Array.Reverse(floatBytes);
            }

            // 4. 字节数组转32位无符号整数（用于位运算解析IEEE格式）
            uint floatBits = BitConverter.ToUInt32(floatBytes, 0);

            // 5. 解析IEEE 32位浮点数的三大组成部分（文档定义：符号位+指数位+尾数位）
            bool isNegative = (floatBits >> 31) != 0; // 符号位：第31位（0=正，1=负）
            uint exponentBits = (floatBits >> 23) & 0xFF; // 指数位：第30-23位（8位）
            uint mantissaBits = floatBits & 0x7FFFFF; // 尾数位：第22-0位（23位）

            // 6. 计算最终浮点数（含特殊情况处理：无穷大、NaN、非归一化数）
            double result;
            if (exponentBits == 0xFF)
            {
                // 特殊情况1：指数全1 → 无穷大或NaN
                result = mantissaBits == 0
                    ? (isNegative ? double.NegativeInfinity : double.PositiveInfinity)
                    : double.NaN;
            }
            else if (exponentBits == 0)
            {
                // 特殊情况2：指数全0 → 非归一化数（无隐含1）
                double mantissa = (double)mantissaBits / (1 << 23); // 2^23 = 8388608
                result = (isNegative ? -1 : 1) * mantissa * Math.Pow(2, -126); // 偏移量为-126（127-1）
            }
            else
            {
                // 正常情况：归一化数（隐含整数部分1）
                double mantissa = 1.0 + (double)mantissaBits / (1 << 23); // 1.xxxx（xxxx为尾数位）
                int exponent = (int)exponentBits - 127; // 指数=原始指数-偏移127
                result = (isNegative ? -1 : 1) * mantissa * Math.Pow(2, exponent);
            }

            return result;
        }
        /// <summary>
        /// 辅助方法：将单个ASCII字符转为4位十六进制值（nibble）
        /// </summary>
        private static byte ConvertAsciiToNibble(char c)
        {
            char upperC = char.ToUpper(c);
            if (upperC >= '0' && upperC <= '9')
                return (byte)(upperC - '0');
            if (upperC >= 'A' && upperC <= 'F')
                return (byte)(upperC - 'A' + 10);

            throw new ArgumentException($"无效的十六进制ASCII字符：'{c}'", nameof(c));
        }
        /// <summary>
        /// 字符串按协议编码（前缀12位长度（3个16进制字符），再跟字符串内容）
        /// </summary>
        public static string EncodeString(string content)
        {
            if (content == null)
                content = string.Empty;

            int length = content.Length;
            if (length > 4095) // 12位最大表示4095
                throw new ArgumentException("字符串长度不能超过4095字符", nameof(content));

            // 12位长度转为3个16进制字符（不足补0）
            string lengthHex = length.ToString("X3").PadLeft(3, '0');
            return lengthHex + content;
        }
        /// <summary>
        /// 协议编码字符串转换为C#字符串（解析3位16进制长度前缀+内容）
        /// </summary>
        /// <param name="encodedString">协议编码的字符串（格式：3位16进制长度前缀 + 字符串内容）</param>
        /// <returns>解码后的原始字符串</returns>
        /// <exception cref="ArgumentException">当输入格式无效时抛出</exception>
        public static string DecodeString(string encodedString)
        {
            // 1. 验证输入基础有效性（至少包含3位长度前缀）
            if (encodedString == null)
                throw new ArgumentNullException(nameof(encodedString), "输入字符串不能为null");
            if (encodedString.Length < 3)
                throw new ArgumentException("协议编码字符串长度至少为3（3位长度前缀）", nameof(encodedString));

            // 2. 解析3位16进制长度前缀（12位，范围0-4095）
            string lengthHex = encodedString.Substring(0, 3);
            if (!int.TryParse(lengthHex, System.Globalization.NumberStyles.HexNumber, null, out int contentLength))
                throw new ArgumentException($"无效的16进制长度前缀：{lengthHex}", nameof(encodedString));

            // 3. 验证长度合理性（12位最大为4095）
            if (contentLength < 0 || contentLength > 4095)
                throw new ArgumentException($"字符串长度超出范围（0-4095），实际：{contentLength}", nameof(encodedString));

            // 4. 验证总长度是否足够（3位前缀 + 内容长度）
            if (encodedString.Length < 3 + contentLength)
                throw new ArgumentException($"字符串内容不完整，期望长度{3 + contentLength}，实际{encodedString.Length}", nameof(encodedString));

            // 5. 提取并返回内容
            return encodedString.Substring(3, contentLength);
        }

        /// <summary>
        /// 计算协议校验和（Header+Parameters块所有字符的16位模和）
        /// </summary>
        public static string CalculateChecksum(string headerAndParams)
        {
            ushort checksum = 0;
            foreach (char c in headerAndParams)
            {
                checksum += (ushort)c;
            }
            // 转为4位16进制ASCII字符串（不足补0）
            return checksum.ToString("X4").PadLeft(4, '0');
        }
    }

