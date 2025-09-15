using System.Text.Json.Serialization;

namespace API.DTO.Suppliers
{
    public class SupplierQuotaDto
    {
        [JsonPropertyName("products")]
        public string Products { get; set; }
        [JsonPropertyName("ads")]
        public string Ads { get; set; }
        [JsonPropertyName("orders")]
        public string Orders { get; set; }
        [JsonPropertyName("specialProducts")]
        public string SpecialProducts { get; set; }
    }
}
