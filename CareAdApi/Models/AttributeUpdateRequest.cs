using System.Text.Json.Serialization;

namespace CareAdApi.Models
{
    public class AttributeUpdateRequest
    {
        [JsonPropertyName("updates")]
        public List<AttributesUpdate> Updates { get; set; } = new List<AttributesUpdate>();
    }
}
