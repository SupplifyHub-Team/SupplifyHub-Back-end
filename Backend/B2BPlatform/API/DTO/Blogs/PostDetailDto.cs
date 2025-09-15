using System.Text.Json.Serialization;

namespace API.DTO.Blogs
{
    public class PostDetailDto:PostBasicDataDto
    {
        [JsonPropertyName("content")]
        public string Content { get; set; }

        [JsonPropertyName("pdfUrl")]
        public string PdfUrl { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }

    }

}
