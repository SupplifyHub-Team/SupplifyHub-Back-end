using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace API.DTO.Orders
{
    public class OrderItemToAddDto
    {
        [Required]
        [MaxLength(200, ErrorMessage = "أقصى عدد من الحروف 200")]
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "برجاء إدخال قيمة صحيحة للكمية")]
        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }
    }
}
