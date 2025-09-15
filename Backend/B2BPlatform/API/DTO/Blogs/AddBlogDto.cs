using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace API.DTO.Blogs;

public class AddBlogDto
{
    [Required(ErrorMessage = "العنوان مطلوب")]
    [MaxLength(200, ErrorMessage = "العنوان لا يجب أن يتجاوز 200 حرف")]
    [MinLength(5, ErrorMessage = "العنوان يجب أن يكون على الأقل 5 أحرف")]
    [JsonPropertyName("title")]
    public string Title { get; set; }

    [Required(ErrorMessage = "المحتوى مطلوب")]
    [MaxLength(100000, ErrorMessage = "المحتوى لا يجب أن يتجاوز 100000 حرف")]
    [MinLength(20, ErrorMessage = "المحتوى يجب أن يكون على الأقل 20 حرف")]
    [JsonPropertyName("content")]
    public string Content { get; set; }
    [Required(ErrorMessage ="الملخص مطلوب")]
    [MaxLength(1500, ErrorMessage = "الملخص لا يجب أن يتجاوز 1500 حرف")]
    [MinLength(10, ErrorMessage = "الملخص يجب أن يكون على الأقل 10 حرف")]
    [JsonPropertyName("excerpt")]
    public string Excerpt { get; set; }

    [Required(ErrorMessage = "يجب إدخال صورة الغلاف")]
    [JsonPropertyName("coverImage")]
    public IFormFile CoverImageFile { get; set; }

    [JsonPropertyName("pdfFile")]
    public IFormFile? PdfFile { get; set; }
}
