using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Dobo.Appl.Utility
{
    public class CertifyTool
    {
       public class CertifyInfo
        {
            public string Machine { get; set; }
            public string User { get; set; }
            public long Expiry { get; set; }

            public string ToUnion() {
                return $"{Machine}|{Expiry}|{User}";
            }

            public static string GetMachineCode()
            {
                return SystemInfo.MD5Encrypt($"MC: {SystemInfo.CpuSn()} + {SystemInfo.BaseBoardSn()} + {SystemInfo.BiosSn()} + {SystemInfo.SystemUUID()}");
            }
        }

        public CertifyTool(string publicKey, string privateKey)
        {
            this.publicKey = publicKey;
            this.privateKey = privateKey;
        }
        readonly string privateKey;
        readonly string publicKey;

        /// <summary>
        /// 私钥签名
        /// </summary>
        /// <param name="machineCode"></param>
        /// <param name="privateKey"></param>
        /// <param name="expiryDate"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public  string GenerateLicense(CertifyInfo conent)
        {
           return SignData(privateKey, conent.ToUnion());
        }

        /// <summary>
        /// 公钥验证
        /// </summary>
        /// <param name="license">sha256|content</param>
        /// <param name="publicKey"></param>
        /// <returns></returns>
        public  bool VerifyLicense(string license)
        {
            try
            {
                var info = license.Split('|');
                return VerifySignData(publicKey, info[0], info[1]);
            }
            catch
            {
                return false;
            }
        }
      public  static string SignData(string privateKey, string data)
        {
            using RSA rsa = RSA.Create();

            rsa.FromXmlString(privateKey); // 加载私钥
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);
            // 使用SHA256哈希算法和PKCS#1签名填充模式创建签名
            byte[] signature = rsa.SignData(dataBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return Convert.ToBase64String(signature);

        }
       public static bool VerifySignData(string publicKey, string originalData, string signature)
        {
            using RSA rsa = RSA.Create();

            rsa.FromXmlString(publicKey); // 加载公钥
            byte[] dataBytes = Encoding.UTF8.GetBytes(originalData);
            byte[] signatureBytes = Convert.FromBase64String(signature);
            // 验证签名
            return rsa.VerifyData(dataBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
       public static byte[] Encrypt(string publicKey, string plainText)
        {
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                // 加载公钥
                rsa.FromXmlString(publicKey);
                byte[] dataBytes = Encoding.UTF8.GetBytes(plainText);
                // 执行加密操作，这里使用PKCS#1 v1.5填充
                byte[] encryptedData = rsa.Encrypt(dataBytes, false);
                return encryptedData;
            }
        }
       public static string Decrypt(string privateKey, byte[] encryptedData)
        {
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                // 加载私钥
                rsa.FromXmlString(privateKey);
                // 执行解密操作
                byte[] decryptedBytes = rsa.Decrypt(encryptedData, false);
                return Encoding.UTF8.GetString(decryptedBytes);
            }
        }
        /// <summary>
        /// 生成RSA密钥对
        /// </summary>
        /// <returns></returns>
        public static (string publicKey, string privateKey) GenerateKeyPair()
        {
            using var rsa = new RSACryptoServiceProvider(2048);
            return (rsa.ToXmlString(false), rsa.ToXmlString(true));
        }
    }
}
