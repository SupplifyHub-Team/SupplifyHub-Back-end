using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace API.DTO.Advertisment
{
    public class AdvertismentPostDto
    {
        [Required(ErrorMessage = "العنوان مطلوب")]
        [JsonPropertyName("title")]
        public string Title { get; set; } 
        
        [Required(ErrorMessage = "يجب ادخال الصورة")]
        [JsonPropertyName("image")]
        public IFormFile ImageFile { get; set; } 
        
        [JsonPropertyName("targetURL")]
        public string? TargetUrl { get; set; }
        
    }
}
