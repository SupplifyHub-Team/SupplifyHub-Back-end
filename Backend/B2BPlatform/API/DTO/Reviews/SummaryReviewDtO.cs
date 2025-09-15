using System.Text.Json.Serialization;

public class SummaryReviewDtO
{
    [JsonPropertyName("totalRatings")]
    public int TotalRatings { get; set; }
    [JsonPropertyName("Distribution")]
    public int[] Distribution { get; set; } = new int[5];
}