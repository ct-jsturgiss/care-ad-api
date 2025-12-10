using System.Text.Json.Serialization;

namespace CareAdApi.Models
{
    public class AttributeUpdateRequest
    {
        public List<AttributesUpdate> Updates { get; set; } = new List<AttributesUpdate>();
    }
}
