using System.Text.Json.Serialization;

namespace API.DTO.Plans
{
    public class PlanToShowDto : AdminAddPlanDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }
        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }

    }
}
