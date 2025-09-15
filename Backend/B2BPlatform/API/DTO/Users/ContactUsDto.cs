using System.ComponentModel.DataAnnotations;

namespace API.DTO.Users
{
    public class ContactUsDto
    {
        [Required]
        [MaxLength(40)]
        public string? Name { get; set; }
        [Required]
        [MaxLength(100)]
        public string? Email { get; set; }
        [Required]
        [MaxLength(1000)]
        public string? QueryText { get; set; }


    }
}
