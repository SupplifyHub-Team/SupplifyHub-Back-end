using System.Text.Json.Serialization;

namespace API.DTO.Suppliers
{
    public class SupplierEditDto
    {
        [JsonPropertyName("description")]
        public string Description { get; set; }
        [JsonPropertyName("locations")]
        public List<string> Locations { get; set; }
        [JsonPropertyName("CategoryIds")]
        public List<int> CategoriesId { get; set; }

    }
}
