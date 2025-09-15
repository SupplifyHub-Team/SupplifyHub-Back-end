using System.Text.Json.Serialization;

namespace API.DTO.Suppliers
{
    public class SubscribedSupplierDto
    {
        [JsonPropertyName("userId")]
        public int UserId { get; set; }
        [JsonPropertyName("companyName")]
        public string CompanyName { get; set; }
        [JsonPropertyName("email")]
        public string Email { get; set; }
        [JsonPropertyName("planName")]
        public string PlanName { get; set; }
        [JsonPropertyName("paymentStatus")]
        public string PaymentStatus { get; set; }
        [JsonPropertyName("startPlanDate")]
        public DateTime? StartJoinPlanDate { get; set; }
        [JsonPropertyName("endPlanDate")]
        public DateTime? EndPlanDate { get; set; }
        [JsonPropertyName("joinDate")]
        public DateTime? JoinDate { get; set; }
        [JsonPropertyName("ordersCompleted")]
        public int OrdersCompleted { get; set; }


    }
}
