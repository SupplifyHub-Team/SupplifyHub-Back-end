using System.Text.Json.Serialization;

namespace API.DTO.GeneralResponse
{
    public class GeneralResponseError
    {
        [JsonPropertyName("data")]
        public Data Data { get; set; }
    }


}
