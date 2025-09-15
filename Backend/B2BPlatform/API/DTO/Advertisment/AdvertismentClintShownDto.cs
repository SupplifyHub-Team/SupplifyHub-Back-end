using System.Text.Json.Serialization;

namespace API.DTO.Advertisment
{
    public class AdvertismentClintShownDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        
        [JsonPropertyName("targetUrl")]
        public string? TargetUrl { get; set; }
        
        [JsonPropertyName("imageUrl")]
        public string ImageUrl { get; set; } 

    }
}
