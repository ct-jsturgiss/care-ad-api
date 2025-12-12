using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CareAdApi.Configuration
{
    public class LocalConfigFile
    {
        private static byte[] m_secret = [];

        [JsonPropertyName("secret_key_path")]
        public string SecretKey { get; set; } = string.Empty;

        [JsonPropertyName("certificate")]
        public string Certificate { get; set; } = string.Empty;

        [JsonPropertyName("certificate_key")]
        public string CertificateKey { get { return Encrypt(PlainCertificateKey); } set { PlainCertificateKey = Decrypt(value); } }

        [JsonIgnore]
        public string PlainCertificateKey { get; set; } = string.Empty;

        [JsonPropertyName("header_key")]
        public string HeaderKey { get; set; } = string.Empty;

        private string Encrypt(string plainText)
        {
            byte[] input = Encoding.UTF8.GetBytes(plainText);
            byte[] output = [];
            byte[] iv = [];
            using (Aes aes = Aes.Create())
            {
                aes.Key = m_secret;
                iv = RandomNumberGenerator.GetBytes(16);
                aes.IV = iv;
                using (var encryptor = aes.CreateEncryptor())
                {
                    output = encryptor.TransformFinalBlock(input, 0, input.Length);
                }
            }

            return string.Join('|', Convert.ToBase64String(output), Convert.ToBase64String(iv));
        }

        private string Decrypt(string encrypted)
        {
            string[] parts = encrypted.Split('|');
            byte[] input = Convert.FromBase64String(parts[0]);
            byte[] iv = Convert.FromBase64String(parts[1]);
            byte[] output = [];
            using (Aes aes = Aes.Create())
            {
                aes.Key = m_secret;
                aes.IV = iv;
                using (var decryptor = aes.CreateDecryptor())
                {
                    output = decryptor.TransformFinalBlock(input, 0, input.Length);
                }
            }

            return Encoding.UTF8.GetString(output);
        }

        public static LocalConfigFile? LoadFile()
        {
            string filePath = GetFilePath();
            if (!File.Exists(filePath))
            {
                return null;
            }

            string keyPath = GetKeyPath();
            if (File.Exists(keyPath))
            {
                m_secret = File.ReadAllBytes(keyPath);
            }
            else
            {
                throw new FileNotFoundException("Could not find local encryption key.");
            }

            LocalConfigFile file = JsonSerializer.Deserialize<LocalConfigFile>(File.ReadAllBytes(filePath)) ?? new LocalConfigFile();

            return file;
        }

        public static void SaveFile(LocalConfigFile file)
        {
            using(FileStream fs = new FileStream(GetFilePath(), FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                JsonSerializer.Serialize(fs, file);
            }
        }

        private static string GetFilePath()
        {
            string file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Constants.LocalConfigFile);

            return file;
        }

        private static string GetKeyPath()
        {
            return Path.Combine(Path.GetDirectoryName(GetFilePath())!, Constants.KeyFileName);
        }
    }
}
