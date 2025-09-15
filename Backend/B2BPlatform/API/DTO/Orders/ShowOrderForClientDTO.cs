using Enum;
using System.Text.Json.Serialization;

namespace API.DTO.Orders
{
    public class ShowOrderForClientDTO
    {
        [JsonPropertyName("orderId")]
        public int OrderId { get; set; }
        [JsonPropertyName("categoryId")]
        public int CategoryId { get; set; }

        [JsonPropertyName("descriptionAndQuantity")]
        public string DescriptionAndQuantity { get; set; }

        
        [JsonPropertyName("items")]
        public List<OrderItemToShowDto> Items { get; set; }

        [JsonPropertyName("requiredLocation")]
        public string RequiredLocation { get; set; }

        [JsonPropertyName("deadline")]
        public DateTime Deadline { get; set; }

        [JsonPropertyName("numSuppliersDesired")]
        public int NumSuppliersDesired { get; set; }

        [JsonPropertyName("contactPersonName")]
        public string ContactPersonName { get; set; }

        [JsonPropertyName("contactPersonPhone")]
        public string ContactPersonPhone { get; set; }
        [JsonPropertyName("dealStatus")]
        public DealStatus DealStatus { get; set; }
        [JsonPropertyName("OrderStatus")]
        public OrderStatus OrderStatus { get; set; }
    }
}
