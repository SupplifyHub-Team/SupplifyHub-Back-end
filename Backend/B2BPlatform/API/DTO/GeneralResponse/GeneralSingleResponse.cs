using System.Text.Json.Serialization;

namespace API.DTO.GeneralResponse
{
    public class GeneralSingleResponse<T>
    {
        [JsonPropertyName("data")]
        public T Data { get; set; } = default!;
    }
}
