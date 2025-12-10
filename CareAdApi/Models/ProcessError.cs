using System.Text.Json.Serialization;

namespace CareAdApi.Models
{
    public class ProcessError
    {
        [JsonPropertyName("error_type")]
        public ErrorType ErrorType { get; set; } = ErrorType.None;

        [JsonPropertyName("user_principal")]
        public string UserPrincipalName { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public string[] Messages { get; set; } = [];
    }

    public enum ErrorType
    {
        None = 0,
        Unknown = 1,
        User = 2,
    }
}
