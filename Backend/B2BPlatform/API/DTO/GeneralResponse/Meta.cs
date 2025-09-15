using System.Text.Json.Serialization;

namespace API.DTO.GeneralResponse
{
    public class Meta
    {
        [JsonPropertyName("currentPage")]
        public int CurrentPage { get; set; }
        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; }
        [JsonPropertyName("totalItems")]
        public int TotalItems { get; set; }
        [JsonPropertyName("totalPages")]
        public int TotalPages { get; set; }

    }


}
