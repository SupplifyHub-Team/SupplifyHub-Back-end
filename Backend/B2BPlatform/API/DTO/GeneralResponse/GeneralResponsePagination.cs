using System.Text.Json.Serialization;

namespace API.DTO.GeneralResponse
{
    public class GeneralResponsePagination<T> : GeneralResponse<T>
    {
        [JsonPropertyName("meta")]

        public Meta Meta { get; set; }
    }


}
