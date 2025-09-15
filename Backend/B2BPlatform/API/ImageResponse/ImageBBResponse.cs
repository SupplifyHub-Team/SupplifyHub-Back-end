using System.Text.Json.Serialization;

namespace API.ImageResponse
{
    public class ImageBBResponse
    {
        [JsonPropertyName("data")]
        public ImageBBData Data { get; set; }
    }
}
