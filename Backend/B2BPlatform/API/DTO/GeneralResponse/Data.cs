using System.Text.Json.Serialization;

namespace API.DTO.GeneralResponse
{
    public class Data
    {
        [JsonPropertyName("message")]
        public string Message { get; set; }
    }


}
