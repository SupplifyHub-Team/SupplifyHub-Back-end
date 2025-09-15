using System.Text.Json.Serialization;

namespace API.DTO.Suppliers
{
    public class SupplierProfileDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("email")]
        public string Email { get; set; }
        [JsonPropertyName("phoneNumber")]
        public string PhoneNumber { get; set; }
        [JsonPropertyName("logoURL")]
        public string LogoURL { get; set; }
        [JsonPropertyName("description")]
        public string Description { get; set; }
        [JsonPropertyName("locations")]
        public List<string> Locations { get; set; }
        [JsonPropertyName("categories")]
        public List<int> Categories { get; set; }
        [JsonPropertyName("planName")]
        public string PlanName  { get; set; }
        [JsonPropertyName("subscriptionStartDate")]
        public DateTime SubscriptionStartDate  { get; set; }
        [JsonPropertyName("subscriptionEndDate")]
        public DateTime SubscriptionEndDate  { get; set; }
        [JsonPropertyName("averageRating")]
        public double AverageRating { get; set; }
        [JsonPropertyName("productCount")]
        public int ProductCount { get; set; }
    }
}
