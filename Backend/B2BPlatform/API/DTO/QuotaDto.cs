using System.Text.Json.Serialization;

namespace API.DTO
{
    public class QuotaDto
    {
        [JsonPropertyName("amount")]
        public int Amount { get; set; }
    }
}
