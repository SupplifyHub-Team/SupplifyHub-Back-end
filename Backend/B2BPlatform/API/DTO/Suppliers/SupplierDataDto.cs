using System.Text.Json.Serialization;

namespace API.DTO.Suppliers
{
    public class SupplierDataDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("email")]
        public string Email { get; set; }
        [JsonPropertyName("phoneNumber")]
        public string PhoneNumber { get; set; }
        [JsonPropertyName("logoUrl")]
        public string LogoURL { get; set; }
        [JsonPropertyName("description")]
        public string Description { get; set; }
        [JsonPropertyName("locations")]
        public List<string> Locations { get; set; }
        [JsonPropertyName("countOfOrderAccepted")]
        public int CountOfOrderAccepted { get; set; }
        [JsonPropertyName("categories")]
        public List<string> Categories { get; set; }
        [JsonPropertyName("averageRating")]
        public double AverageRating { get; set; }
        [JsonPropertyName("productCount")]
        public int ProductCount { get; set; }
    }
}
