using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace API.DTO.Plans
{
    public class AdminUpdatePlanDto
    {
        [Required(ErrorMessage = " اسم الخطة مطلوب  ")]
        [JsonPropertyName("planName")]
        public string PlanName { get; set; }

        [Required(ErrorMessage = " سعر الخطة مطلوب  ")]
        [Column(TypeName = "decimal(18,3)")]
        [JsonPropertyName("price")]
        [Range(0, int.MaxValue)]
        public decimal Price { get; set; }
        [Required(ErrorMessage = " مدة الخطة مطلوبة  ")]
        [JsonPropertyName("description")]
        public string Description { get; set; }
        [Required(ErrorMessage = " مدة الخطة مطلوبة  ")]
        [JsonPropertyName("duration")]
        [Range(1, 12)]
        public int Duration { get; set; }
        [Required(ErrorMessage = " الميزات والعيوب مطلوبة  ")]
        [JsonPropertyName("cons")]
        [MinLength(1, ErrorMessage = "يجب إدخال عيب واحد على الأقل")]
        public List<string> Cons { get; set; }
        [Required(ErrorMessage = " الميزات والعيوب مطلوبة  ")]
        [JsonPropertyName("pros")]
        [MinLength(1, ErrorMessage = "يجب إدخال ميزة واحدة على الأقل")]
        public List<string> Pros { get; set; }
    }
}

