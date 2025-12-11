using System.Text.Json;
using System.Text.Json.Serialization;

namespace CareAdApi.Configuration
{
    public class LocalConfigFile
    {
        [JsonPropertyName("header_key")]
        public string HeaderKey { get; set; } = string.Empty;

        public static LocalConfigFile? LoadFile()
        {
            string file = GetFilePath();
            if (!File.Exists(file))
            {
                return null;
            }

            return JsonSerializer.Deserialize<LocalConfigFile>(File.ReadAllBytes(file)) ?? new LocalConfigFile();
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
    }
}
