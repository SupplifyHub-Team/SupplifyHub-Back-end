using Enum;
using System.Text.Json.Serialization;

namespace API.DTO.Orders;
public class ClientOrderDto
{
    [JsonPropertyName("OrderId")]
    public int OrderId { get; set; }
    
    [JsonPropertyName("companyName")]
    public string Name { get; set; }
    
    [JsonPropertyName("email")]
    public string Email { get; set; }
    
    [JsonPropertyName("category")]
    public string Category { get; set; }
    
    [JsonPropertyName("orderItems")]
    public List<OrderItemToShowDto> Items { get; set; }

    [JsonPropertyName("offerNumbers")]
    public int OfferNumbers { get; set; }
    
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
    
    [JsonPropertyName("orderStatus")]
    public string OrderStatus { get; set; }
    
    [JsonPropertyName("deadline")]
    public DateTime Deadline { get; set; }
}