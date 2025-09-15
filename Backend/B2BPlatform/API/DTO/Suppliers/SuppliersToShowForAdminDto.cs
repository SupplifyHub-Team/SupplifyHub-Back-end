using System.Text.Json.Serialization;

namespace API.DTO.Suppliers
{
    public class SuppliersToShowForAdminDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("phone")]
        public string Phone { get; set; }
        [JsonPropertyName("email")]
        public string Email { get; set; }
        [JsonPropertyName("logoURL")]
        public string LogoURL { get; set; }
        [JsonPropertyName("pdfURL")]
        public string TaxNumberURL { get; set; }
        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }
        [JsonPropertyName("categories")]
        public List<string> Categories { get; set; }

    }
}
