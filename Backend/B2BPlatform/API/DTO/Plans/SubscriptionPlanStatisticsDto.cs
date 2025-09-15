using System.Text.Json.Serialization;

namespace API.DTO.Plans
{
    public class SubscriptionPlanStatisticsDto
    {
        [JsonPropertyName("planName")]
        public string PlanName { get; set; }
        [JsonPropertyName("totalSubscribers")]
        public int TotalCount { get; set; }
        [JsonPropertyName("newSubscribersThisMonth")]
        public int NewThisMonth { get; set; }
        [JsonPropertyName("newSubscriberPercentage")]
        public double NewSubscriberPercentage { get; set; }
    }
}
