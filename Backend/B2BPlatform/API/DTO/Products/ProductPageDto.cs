using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// Add these DTOs to your existing namespace
namespace API.DTO.Products
{
    public class ProductPageDto
    {
        

        [JsonPropertyName("allReviews")]
        public List<ReviewDto> AllReviews { get; set; } = new List<ReviewDto>();

        [JsonPropertyName("products")]
        public List<ProductToShowDto> Products { get; set; } = new List<ProductToShowDto>();
        [JsonPropertyName("stats")]
        public StatsDto Stats { get; set; } = new StatsDto();
    }

    public class StatsDto
    {
        [JsonPropertyName("numberOfViews")]
        public int NumberOfViews { get; set; }
    }
}