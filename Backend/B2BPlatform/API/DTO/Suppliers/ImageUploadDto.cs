using System.Text.Json.Serialization;

namespace API.DTO.Suppliers
{
    public class ImageUploadDto
    {
        [JsonPropertyName("logoUrl")]
        public IFormFile Logo { get; set; }
    }
}
