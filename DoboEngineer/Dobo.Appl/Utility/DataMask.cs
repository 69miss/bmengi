using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Security.Cryptography;
using System.Text;
namespace Dobo.Appl.Utility
{



    public  class DataMask
    {
        readonly string maskKey;
        public DataMask(string key) {

            maskKey = key;
        }
        public   string Encrypt(string plainText)
        {
            return Encrypt(plainText,maskKey);
        }
        public string Decrypt(string plainText)
        {
            return Decrypt(plainText, maskKey);
        }
        /// <summary>
        /// 加密字符串
        /// </summary>
        /// <param name="plainText">要加密的明文</param>
        /// <param name="password">加密密码</param>
        /// <returns>长度不变的密文</returns>
        public static string Encrypt(string plainText, string password)
        {
            if (string.IsNullOrEmpty(plainText)) return plainText;

            // 1. 使用密码生成一个确定的种子
            int seed = GetStableHashCode(password);
            Random random = new Random(seed);

            // 2. 将字符串转换为字符数组以便逐个处理
            char[] chars = plainText.ToCharArray();

            // 3. 对每个字符进行变换
            for (int i = 0; i < chars.Length; i++)
            {
                // 生成一个随机数（0~65535之间，覆盖char的范围）
                int randomValue = random.Next(0, char.MaxValue + 1);
                // 使用XOR运算加密当前字符
                chars[i] = (char)(chars[i] ^ randomValue);
            }

            return new string(chars);
        }

        /// <summary>
        /// 解密字符串
        /// </summary>
        /// <param name="cipherText">要解密的密文</param>
        /// <param name="password">解密密码（必须与加密密码一致）</param>
        /// <returns>原始明文</returns>
        public static string Decrypt(string cipherText, string password)
        {
            // 解密过程与加密完全对称！
            return Encrypt(cipherText, password);
        }

        /// <summary>
        /// 从一个字符串生成稳定的哈希值作为随机数种子
        /// </summary>
        private static int GetStableHashCode(string value)
        {
            unchecked
            {
                int hash = 17;
                foreach (char c in value)
                {
                    hash = hash * 31 + c;
                }
                return hash;
            }
        }
    }
}

