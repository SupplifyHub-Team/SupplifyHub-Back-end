using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace API.DTO.Blogs
{
    public class UpdateBlogDto
    {
        [MinLength(5, ErrorMessage = "العنوان يجب أن يكون على الأقل 5 أحرف")]
        [MaxLength(200, ErrorMessage = "العنوان لا يجب أن يتجاوز 200 حرف")]
        public string? Title { get; set; }
        [MinLength(20, ErrorMessage = "المحتوى يجب أن يكون على الأقل 20 أحرف")]
        [MaxLength(100000, ErrorMessage = "المحتوى لا يجب أن يتجاوز 100000 حرف")]
        public string? Content { get; set; }
        [MinLength(10, ErrorMessage = "الملخص يجب أن يكون على الأقل 10 أحرف")]
        [MaxLength(1500, ErrorMessage = "الملخص لا يجب أن يتجاوز 1500 حرف")]
        public string? Excerpt { get; set; }

        public IFormFile? CoverImage { get; set; }

        public IFormFile? PdfFile { get; set; }
    }
}
