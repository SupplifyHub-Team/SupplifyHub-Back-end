using System.Text.Json.Serialization;

namespace API.DTO.Suppliers
{
    public class SupplierForHomePageDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("companyName")]
        public string CompanyName { get; set; }
        [JsonPropertyName("email")]
        public string Email { get; set; }
        [JsonPropertyName("categoryNames")]
        public List<string> CategoryNames { get; set; } 

        [JsonPropertyName("phoneNumber")]
        public string PhoneNumber { get; set; }
        [JsonPropertyName("logoUrl")]
        public string LogoUrl { get; set; }
        [JsonPropertyName("locations")]
        public List<string> Locations { get; set; } = new List<string>();
        [JsonPropertyName("planName")]
        public string PlanName { get; set; }
        [JsonPropertyName("joinDate")]
        public DateTime? JoinedAt { get; set; }
    }
}
