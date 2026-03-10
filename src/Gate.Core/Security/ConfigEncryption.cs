using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Linq;

namespace Gate.Security
{
    /// <summary>
    /// 配置加密工具
    /// </summary>
    public static class ConfigEncryption
    {
        // 默认密钥派生参数
        private const int Iterations = 100000;
        private const int KeySize = 32; // 256 bits
        private const int SaltSize = 16;
        
        /// <summary>
        /// 使用主密码加密配置
        /// </summary>
        public static string Encrypt(string plainText, string password)
        {
            if (string.IsNullOrEmpty(plainText))
                throw new ArgumentException("明文不能为空", nameof(plainText));
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("密码不能为空", nameof(password));
            
            // 生成随机盐
            var salt = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            
            // 派生密钥 (netstandard2.0 compatible)
            var deriveBytes = new Rfc2898DeriveBytes(password, salt, Iterations);
            var key = deriveBytes.GetBytes(KeySize);
            
            // 加密
            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.GenerateIV();
                
                using (var encryptor = aes.CreateEncryptor())
                {
                    var plainBytes = Encoding.UTF8.GetBytes(plainText);
                    var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                    
                    // 组合: 盐 + IV + 密文
                    var result = new byte[salt.Length + aes.IV.Length + encryptedBytes.Length];
                    Buffer.BlockCopy(salt, 0, result, 0, salt.Length);
                    Buffer.BlockCopy(aes.IV, 0, result, salt.Length, aes.IV.Length);
                    Buffer.BlockCopy(encryptedBytes, 0, result, salt.Length + aes.IV.Length, encryptedBytes.Length);
                    
                    return Convert.ToBase64String(result);
                }
            }
        }
        
        /// <summary>
        /// 使用主密码解密配置
        /// </summary>
        public static string Decrypt(string encryptedText, string password)
        {
            if (string.IsNullOrEmpty(encryptedText))
                throw new ArgumentException("密文不能为空", nameof(encryptedText));
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("密码不能为空", nameof(password));
            
            var fullCipher = Convert.FromBase64String(encryptedText);
            
            // 提取盐
            var salt = new byte[SaltSize];
            Buffer.BlockCopy(fullCipher, 0, salt, 0, salt.Length);
            
            // 提取 IV
            var iv = new byte[16];
            Buffer.BlockCopy(fullCipher, salt.Length, iv, 0, iv.Length);
            
            // 提取密文
            var cipherLength = fullCipher.Length - salt.Length - iv.Length;
            var cipher = new byte[cipherLength];
            Buffer.BlockCopy(fullCipher, salt.Length + iv.Length, cipher, 0, cipherLength);
            
            // 派生密钥
            var deriveBytes = new Rfc2898DeriveBytes(password, salt, Iterations);
            var key = deriveBytes.GetBytes(KeySize);
            
            // 解密
            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                
                using (var decryptor = aes.CreateDecryptor())
                {
                    var plainBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
                    return Encoding.UTF8.GetString(plainBytes);
                }
            }
        }
        
        /// <summary>
        /// 验证密码是否正确
        /// </summary>
        public static bool VerifyPassword(string encryptedText, string password)
        {
            try
            {
                Decrypt(encryptedText, password);
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// 加密文件
        /// </summary>
        public static bool EncryptFile(string inputPath, string outputPath, string password)
        {
            try
            {
                var plainText = File.ReadAllText(inputPath);
                var encrypted = Encrypt(plainText, password);
                File.WriteAllText(outputPath, encrypted);
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// 解密文件
        /// </summary>
        public static bool DecryptFile(string inputPath, string outputPath, string password)
        {
            try
            {
                var encrypted = File.ReadAllText(inputPath);
                var decrypted = Decrypt(encrypted, password);
                File.WriteAllText(outputPath, decrypted);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
    
    /// <summary>
    /// 审计日志条目
    /// </summary>
    public class AuditLogEntry
    {
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Action { get; set; } = "";
        public string User { get; set; } = Environment.UserName;
        public string Details { get; set; } = "";
        public string IpAddress { get; set; }
        public bool Success { get; set; }
    }
    
    /// <summary>
    /// 审计日志管理器
    /// </summary>
    public static class AuditLogger
    {
        private static readonly string AuditLogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Gate", "audit.log");
        
        /// <summary>
        /// 记录操作
        /// </summary>
        public static void Log(string action, string details, bool success = true)
        {
            try
            {
                var entry = new AuditLogEntry
                {
                    Action = action,
                    Details = details,
                    Success = success
                };
                
                var logLine = string.Format("[{0:yyyy-MM-dd HH:mm:ss}] {1} by {2} - {3} - {4}",
                    entry.Timestamp, entry.Action, entry.User, 
                    entry.Success ? "SUCCESS" : "FAILURE", entry.Details);
                
                var dir = Path.GetDirectoryName(AuditLogPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                
                File.AppendAllText(AuditLogPath, logLine + Environment.NewLine);
            }
            catch
            {
                // 审计日志失败不应影响主流程
            }
        }
        
        /// <summary>
        /// 读取审计日志
        /// </summary>
        public static string[] ReadLogs(int lastN = 100)
        {
            try
            {
                if (!File.Exists(AuditLogPath))
                    return new string[0];
                
                var lines = File.ReadAllLines(AuditLogPath);
                if (lines.Length <= lastN)
                    return lines;
                
                var start = lines.Length - lastN;
                var result = new string[lastN];
                Array.Copy(lines, start, result, 0, lastN);
                return result;
            }
            catch
            {
                return new string[0];
            }
        }
    }
}