using System.Text.Json.Serialization;

namespace API.DTO.Orders;
public class OrderStatisticsDto
{
    [JsonPropertyName("status")]
    public string Status { get; set; }
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }
    [JsonPropertyName("newThisMonth")]
    public int NewThisMonth { get; set; }
}