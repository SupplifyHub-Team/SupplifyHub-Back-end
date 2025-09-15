using System.Text.Json.Serialization;

namespace API.DTO
{
    public class GeneralStatisticsDto
    {
        [JsonPropertyName("totalOrders")]
        public int TotalOrders { get; set; }
        [JsonPropertyName("totalUsers")]
        public int TotalUsers { get; set; }
        [JsonPropertyName("totalCategories")]
        public int TotalCategories { get; set; }
        [JsonPropertyName("newOrdersThisMonth")]
        public int NewOrdersThisMonth { get; set; }
        [JsonPropertyName("newUsersThisMonth")]
        public int NewUsersThisMonth { get; set; }
        [JsonPropertyName("newCategoriesThisMonth")]
        public int NewCategoriesThisMonth { get; set; }
    }
}
