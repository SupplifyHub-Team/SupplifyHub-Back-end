using System.Text.Json.Serialization;

namespace API.DTO.Plans
{
    public class UnconfirmedSubscriptionDto
    {
        [JsonPropertyName("planId")]
        public int PlanId { get; set; }
        [JsonPropertyName("supplierId")]
        public int SupplierId { get; set; }
        [JsonPropertyName("planName")]
        public string PlanName { get; set; }
        [JsonPropertyName("supplierName")]
        public string SupplierName { get; set; }
        [JsonPropertyName("email")]
        public string Email { get; set; }
        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }
        [JsonPropertyName("joinDate")]
        public DateTime JoinDate { get; set; }
        [JsonPropertyName("duration")]
        public int Duration { get; set; }

    }
}
