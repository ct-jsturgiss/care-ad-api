using System.Text.Json.Serialization;

namespace CareAdApi.Models
{
    public class AttributesUpdate
    {
        [JsonPropertyName("principal_name")]
        public string? PrincipalName { get; set; }

        [JsonPropertyName("employee_id")]
        public string? EmployeeId { get; set; }

        [JsonPropertyName("manager_principal_name")]
        public string? ManagerPrincipalName { get; set; }
    }
}
