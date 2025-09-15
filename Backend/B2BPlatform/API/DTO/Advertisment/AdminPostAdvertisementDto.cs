using API.ValidationAttributes;
using System.Text.Json.Serialization;

namespace API.DTO.Advertisment
{
    public class AdminPostAdvertisementDto:AdvertismentPostDto
    {
        [FutureDate]
        [JsonPropertyName("endDate")]
        public DateTime EndDate { get; set; }
    }
}
