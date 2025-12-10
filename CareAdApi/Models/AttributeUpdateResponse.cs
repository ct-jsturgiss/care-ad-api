using System.Text.Json.Serialization;

namespace CareAdApi.Models
{
    public class AttributeUpdateResponse
    {
        [JsonPropertyName("success")]
        public List<string> Success { get; set; } = new List<string>();

        [JsonPropertyName("errors")]
        public List<ProcessError> Errors { get; set; } = new List<ProcessError>();
    }
}
