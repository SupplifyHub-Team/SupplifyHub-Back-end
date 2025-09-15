using System.Text.Json.Serialization;

namespace API.DTO.Blogs
{
    public class PostBasicDataDto
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }
        [JsonPropertyName("coverImageUrl")]
        public string CoverImageUrl { get; set; }        
        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }
    }

}
