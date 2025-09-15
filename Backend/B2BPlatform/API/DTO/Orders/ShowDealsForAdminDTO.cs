using System.Text.Json.Serialization;

namespace API.DTO.Orders
{
    public class ShowDealsForAdminDTO
    {
        [JsonPropertyName("dealId")]
        public int DealId { get; set; }
        [JsonPropertyName("supplierDealDetails")]
        public ShowDealDetailsForAdminDTO SupplierDealDetails { get; set; }
        [JsonPropertyName("clientDealDetails")]
        public ShowDealDetailsForAdminDTO ClientDealDetails { get; set; }
    }


}
