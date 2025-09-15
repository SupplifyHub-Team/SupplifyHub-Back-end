using Enum;
using System.Text.Json.Serialization;

namespace API.DTO.Orders
{
    public class ShowDealForSupplierDTO
    {
        [JsonPropertyName("dealId")]
        public int DealId { get; set; }
        
        [JsonPropertyName("items")]
        public List<DealItemToShowDto> Items { get; set; }

        [JsonPropertyName("CompanyName")]
        public string CompanyName { get; set; }
        [JsonPropertyName("CompanyEmail")]
        public string CompanyEmail { get; set; }

        [JsonPropertyName("CompanyPhone")]
        public string CompanyPhone { get; set; }
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
