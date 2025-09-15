using Entities;
using Enum;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
namespace API.DTO.Users
{
    public class UserRegisterDTO
    {
        [Required]
        public ClientType accountType { get; set; }
        [Required]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "اسم المستخدم يجب أن يتراوح بين 3 و 50 حرفًا")]
        [IsUniqueValidation<User>]
        public string UserName { get; set; }

        [Required]
        [EmailAddress(ErrorMessage = "شكل إيميل غير صحيح")]
        [StringLength(255)]
        [IsUniqueValidation<User>]
        public string email { get; set; }

        [Required]
        [MinLength(8, ErrorMessage = "كلمة المرور يجب ألا تقل عن 8 أحرف")]
        public string password { get; set; }

        [Required]
        [RegularExpression(@"^\d+$", ErrorMessage = "صيغة رقم الهاتف غير صحيحة.")]
        [StringLength(11, MinimumLength = 11, ErrorMessage = "يجب أن يتكون رقم الهاتف من 11 رقمًا بالضبط")]
        public string phoneNumber { get; set; }

        // Properties for supplier-specific data
        public IFormFile? textNumberPicture { get; set; }
        public List<int>? categoriesId { get; set; }
        public List<string>? locations { get; set; }

    }
}
