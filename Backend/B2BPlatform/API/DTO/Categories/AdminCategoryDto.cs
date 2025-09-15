using System.Text.Json.Serialization;

namespace API.DTO.Categories
{
    public class AdminCategoryDto
    {
        [JsonPropertyName("categoryId")]
        public int CategoryId { get; set; }
        [JsonPropertyName("categoryName")]
        public string CategoryName { get; set; }
        [JsonPropertyName("numberOfAssociatedSuppliers")]
        public int NumberOfAssociatedSuppliers { get; set; }
        [JsonPropertyName("numberOfAssociatedClients")]
        public int NumberOfAssociatedClients { get; set; }
        [JsonPropertyName("imageURL")]
        public string ImageURL { get; set; }
    }
}
