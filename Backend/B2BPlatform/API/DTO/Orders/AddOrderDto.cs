using API.ValidationAttributes;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace API.DTO.Orders
{
    public class AddOrderDto
    {
        [Required(ErrorMessage = "حقل مطلوب")]
        [JsonPropertyName("categoryId")]
        public int CategoryId { get; set; }

        //[Required(ErrorMessage = "حقل مطلوب"), MaxLength(1000,ErrorMessage ="أقصى عدد من الحروف 1000")]
        //[JsonPropertyName("description")]
        //public string DescriptionAndQuantity { get; set; }
        
        [Required(ErrorMessage = "حقل مطلوب")]
        [JsonPropertyName("items")]
        public List<OrderItemToAddDto> Items { get; set; }


        //[Required(ErrorMessage = "حقل مطلوب"), Range(1, int.MaxValue, ErrorMessage = "برجاء إدخال قيمة صحيحة للكمية")]
        //[JsonPropertyName("quantity")]
        //public int Quantity { get; set; }

        [Required(ErrorMessage = "حقل مطلوب"), MaxLength(200, ErrorMessage = "أقصى عدد من الحروف 200")]
        [JsonPropertyName("requiredLocation")]
        public string RequiredLocation { get; set; }

        [Required(ErrorMessage = "حقل مطلوب")]
        [FutureDate]
        [JsonPropertyName("deadline")]
        public DateTime Deadline { get; set; }

        [Required(ErrorMessage = "حقل مطلوب"), Range(1, 50, ErrorMessage = "أدخل قيمة بين 1 و 50")]
        [JsonPropertyName("numSuppliersDesired")]
        public int NumSuppliersDesired { get; set; }

        [Required(ErrorMessage = "حقل مطلوب"), MaxLength(100, ErrorMessage = "أقصى عدد من الحروف 100")]
        [JsonPropertyName("contactPersonName")]
        public string ContactPersonName { get; set; }

        [Required(ErrorMessage ="حقل مطلوب"), Phone(ErrorMessage ="رقم التواصل غير صحيح"), MaxLength(20)]
        [JsonPropertyName("contactPersonPhone")]
        public string ContactPersonPhone { get; set; }

    }


}
