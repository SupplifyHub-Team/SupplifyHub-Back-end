using System.Text.Json.Serialization;

namespace API.ImageResponse
{
    public class ImageBBData
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }
    }
}
