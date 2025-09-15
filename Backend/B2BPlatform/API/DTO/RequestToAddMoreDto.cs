using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace API.DTO
{
    public class RequestToAddMoreDto
    {
        
        [JsonPropertyName("requestId")]
        public Guid RequestId { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("email")]
        public string Email { get; set; }
        [JsonPropertyName("phone")]
        public string Phone { get; set; }
        [JsonPropertyName("amount")]
        public int Amount { get; set; }
    }
}
