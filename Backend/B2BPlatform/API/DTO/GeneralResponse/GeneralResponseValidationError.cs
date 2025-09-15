using System.Text.Json.Serialization;

namespace API.DTO.GeneralResponse
{

    public class GeneralResponseValidationError
    {
        [JsonPropertyName("data")]
        public ErrorData Data { get; set; }

    }
}
