using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Entities
{
    public class Blog
    {
        [Key]
        public int Id { get; set; }

        public string Title { get; set; } 

        public string Content { get; set; } 

        public string Excerpt { get; set; }

        public string CoverImageUrl { get; set; }

        public string PublicImageId { get; set; }

        public string? PdfUrl { get; set; }

        public string? PublicPdfId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    }
}
