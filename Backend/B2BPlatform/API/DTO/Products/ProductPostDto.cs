using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace API.DTO.Products
{
    public class ProductPostDto
    {
        [Required( ErrorMessage = "اسم المنتج مطلوب")]
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [Required]
        [JsonPropertyName("price")]
        [Range(0.01, double.MaxValue, ErrorMessage = "قيمة غير صالحة للسعر. يجب أن تكون عدداً صحيحاً موجباً")]
        public double Price { get; set; }

        [Required(ErrorMessage = "الوصف حقل مطلوب")]
        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("productImageURl")]
        [Required(ErrorMessage = "صورة المنتج مطلوبة")]
        public IFormFile ProductImage { get; set; }
        [JsonPropertyName("offer")]
        public int? Offer { get; set; }
        [JsonPropertyName("isSpecial")]
        public bool? IsSpecial { get; set; }
    }
}
