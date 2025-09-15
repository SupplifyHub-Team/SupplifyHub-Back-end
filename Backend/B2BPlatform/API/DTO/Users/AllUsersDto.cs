using System.Text.Json.Serialization;

namespace API.DTO.Users
{
    public class AllUsersDto
    {
        [JsonPropertyName("userId")]
        public int Id { get; set; }

        [JsonPropertyName("companyName")]
        public string Name { get; set; }
        [JsonPropertyName("userName")]
        public string UserName { get; set; }
        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }
        [JsonPropertyName("role")]
        public string Role { get; set; }
        [JsonPropertyName("categoryNames")]
        public List<string> CategoryNames { get; set; }
        [JsonPropertyName("joinDate")]
        public DateTime JoinDate { get; set; }
        [JsonPropertyName("locations")]
        public List<string> Locations { get; set; }
        [JsonPropertyName("email")]
        public string Email { get; set; }
        [JsonPropertyName("phoneNumber")]
        public string PhoneNumber { get; set; }
    }
}
