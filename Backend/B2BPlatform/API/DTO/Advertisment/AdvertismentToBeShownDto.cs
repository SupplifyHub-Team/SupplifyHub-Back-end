using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace API.DTO.Advertisment
{
    public class AdvertismentToBeShownDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        
        [JsonPropertyName("title")]
        public string Title { get; set; } 
        
        [JsonPropertyName("targetUrl")]
        public string? TargetUrl { get; set; } // URL to redirect when clicked
        
        [JsonPropertyName("imagUrl")]
        public string ImageUrl { get; set; } // store path/URL to image
        
        [JsonPropertyName("createdBy")]
        public string CompanyName { get; set; } 
        
        [JsonPropertyName("startDate")]
        public DateTime StartDate { get; set; } 
        
        [JsonPropertyName("endDate")]
        public DateTime EndDate { get; set; }   
        
        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }
    }
}
