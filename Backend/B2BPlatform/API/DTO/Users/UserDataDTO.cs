using Enum;
using System.Text.Json.Serialization;

namespace API.DTO.Users
{
    public class UserDataDTO
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("email")]
        public string Email { get; set; }
        [JsonPropertyName("role")]
        public RoleName Role { get; set; }
    }


}
