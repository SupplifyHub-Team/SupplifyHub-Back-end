using System.Text.Json.Serialization;

namespace API.DTO.Blogs
{
    public class PostPaginationDto:PostBasicDataDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("slug")]
        public string Slug { get; set; }
        [JsonPropertyName("excerpt")]
        public string Excerpt { get; set; }

    }

}
