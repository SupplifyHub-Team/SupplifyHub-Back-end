using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace API.DTO.Categories
{
    public class SuggestCategoryDto
    {
        [Required]
        [JsonPropertyName("name")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "اسم الفئة يجب ان يكون أكثر من حرفين وأقل من 100 حرف")]

        public string Name { get; set; }
    }
}
