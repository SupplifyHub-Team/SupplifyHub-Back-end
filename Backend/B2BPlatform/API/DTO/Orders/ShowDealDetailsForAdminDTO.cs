using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace API.DTO.Orders
{
    public class ShowDealDetailsForAdminDTO
    { 
        [JsonPropertyName("dealDetailsId")]
        public int DealDetailsId { get; set; }

        [JsonPropertyName("companyName")]
        public string CompanyName { get; set; }

        [JsonPropertyName("companyEmail")]
        public string CompanyEmail { get; set; }

        [JsonPropertyName("companyPhone")]
        public string CompanyPhone { get; set; }

        

        [JsonPropertyName("dealDoneAt")]
        public DateTime DealDoneAt { get; set; }

        [JsonPropertyName("dateOfDelivered")]
        public DateTime DateOfDelivered { get; set; }

        [JsonPropertyName("items")]
        public List<DealItemToShowDto> Items { get; set; } = new List<DealItemToShowDto>();
    }


}
