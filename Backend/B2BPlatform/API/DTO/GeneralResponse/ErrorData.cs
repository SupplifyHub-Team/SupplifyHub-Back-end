using System.Text.Json.Serialization;

namespace API.DTO.GeneralResponse
{
    public class ErrorData
    {

        [JsonPropertyName("message")]
        public string Message { get; set; }
        [JsonPropertyName("details")]
        public Dictionary<string, string> details { get; set; }
    }


}
