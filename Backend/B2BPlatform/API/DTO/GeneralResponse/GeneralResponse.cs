using System.Text.Json.Serialization;

namespace API.DTO.GeneralResponse
{
    public class GeneralResponse<T>
    {
        [JsonPropertyName("data")]
        public List<T> Data { get; set; } = new List<T>();

    }


}
