using System.Text.Json.Serialization;

namespace API.DTO.Orders
{
    public class OrderItemToShowDto:OrderItemToAddDto
    {
        [JsonPropertyName("id")]
        public int ItemId { get; set; }
        
    }
}
