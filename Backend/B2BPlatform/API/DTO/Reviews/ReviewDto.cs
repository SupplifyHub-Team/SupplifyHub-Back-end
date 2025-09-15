using System.Text.Json.Serialization;

public class ReviewDto
{
    [JsonPropertyName("reviewerName")]
    public string ReviewerName { get; set; }

    [JsonPropertyName("comment")]
    public string Comment { get; set; }

    [JsonPropertyName("rating")]
    public int Rating { get; set; }
}
