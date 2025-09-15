using System.Text.Json.Serialization;

namespace API.DTO.Users;
public class UserStatisticsDto
{
    [JsonPropertyName("category")]
    public string Category { get; set; }
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }
    [JsonPropertyName("newThisMonth")]
    public int NewThisMonth { get; set; }
    [JsonPropertyName("newUserPercentage")]
    public double NewUserPercentage { get; set; }
}