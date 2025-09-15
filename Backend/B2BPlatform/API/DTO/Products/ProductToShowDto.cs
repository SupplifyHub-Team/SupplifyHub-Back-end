using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace API.DTO.Products
{
    public class ProductToShowDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("price")]
        public double Price { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("companyName")]
        public string CompanyName { get; set; }
        [JsonPropertyName("productImageURl")]
        public string ProductImageURl { get; set; }
        [JsonPropertyName("offer")]
        public int Offer { get; set; }
        [JsonPropertyName("isSpecial")]
        public bool IsSpecial {get; set;}
    }
}
