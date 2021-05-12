using System.Text.Json.Serialization;

namespace Tanzu.Toolkit.Services.CfCli.Models
{
    public abstract class ApiV2Response
    {
        [JsonPropertyName("next_url")]
        public abstract string NextUrl { get; set; }
        [JsonPropertyName("prev_url")]
        public abstract string PrevUrl { get; set; }
        [JsonPropertyName("total_pages")]
        public abstract int TotalPages { get; set; }
        [JsonPropertyName("total_results")]
        public abstract int TotalResults { get; set; }
    }
}
