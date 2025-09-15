using System.Text.Json.Serialization;
using API.DTO.Users;

namespace API.DTO.GeneralResponse
{
    public class GeneralResponseLogin
    {
        [JsonPropertyName("token")]
        public string Token { get; set; }
        [JsonPropertyName("user")]
        public UserDataDTO User { get; set; }
    }


}
