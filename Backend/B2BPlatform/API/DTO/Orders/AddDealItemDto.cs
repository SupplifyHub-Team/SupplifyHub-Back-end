
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace API.DTO.Orders
{
    public class AddDealItemDto
    {
        [Required]
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        [Required]
        [Range(1, double.MaxValue)]
        [JsonPropertyName("price")]
        public double Price { get; set; }
    }
}
