using Enum;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace API.DTO.Categories
{
    public class AddCategoryDto
    {
        [Required(ErrorMessage = "اسم الفئة مطلوب")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "اسم الفئة يجب ان يكون أكثر من حرفين وأقل من 100 حرف")]
        [JsonPropertyName("categoryName")]
        public string CategoryName { get; set; }

        [Required(ErrorMessage = "الصورة مطلوبة")]
        [JsonPropertyName("photo")]
        public IFormFile Photo { get; set; }
    }



}
