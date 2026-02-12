using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Dobo.Appl.Utility
{
    public class SystemInfo
    {
        public static string GetMachineCode()
        {
            return MD5Encrypt($"MC: {SystemInfo.CpuSn()} + {SystemInfo.BaseBoardSn()} + {SystemInfo.BiosSn()} + {SystemInfo.SystemUUID()}");
        }
        public static string MD5Encrypt(string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
              
                return BitConverter.ToString(hashBytes).Replace("-", "");
            }
        }
        public static string SystemUUID() {
            return GetWmiProperty("Win32_ComputerSystemProduct", "UUID");
        }
        public static string BiosSn() {
            return GetWmiProperty("Win32_BIOS", "SerialNumber");
        }
        public static string CpuSn()
        {
            return GetWmiProperty("Win32_Processor", "ProcessorId");
        }
        public static string BaseBoardSn() {
            return GetWmiProperty("Win32_BaseBoard", "SerialNumber");
        }
        /// <summary>
        /// 3. 获取硬盘物理序列号 (取第一个物理硬盘)
        /// </summary>
        private static string GetDiskSerialNumber()
        {
            // Win32_DiskDrive 获取的是物理硬盘，Win32_LogicalDisk 获取的是分区
            // 我们需要物理硬盘的 SerialNumber，这通常比 Model 更唯一
            return GetWmiProperty("Win32_DiskDrive", "SerialNumber");
        }
        private static string GetWmiProperty(string className, string propertyName)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher($"SELECT {propertyName} FROM {className}"))
                using (var results = searcher.Get())
                {
                    foreach (ManagementObject obj in results)
                    {
                        return obj[propertyName]?.ToString()?.Trim() ?? string.Empty;
                    }
                }
            }
            catch (ManagementException mex)
            {
                // 记录日志或处理特定异常
                Console.WriteLine($"WMI查询失败 ({className}.{propertyName}): {mex.Message}");
            }
            catch (Exception ex)
            {
                // 处理其他异常
                Console.WriteLine($"获取WMI属性时发生错误 ({className}.{propertyName}): {ex.Message}");
            }
            return string.Empty;
        }
    }
}
