using System.Text.Json.Serialization;

namespace Tanzu.Toolkit.CloudFoundryApiClient.Models.AppsResponse
{
    public class AppsResponse
    {
        [JsonPropertyName("pagination")]
        public Pagination Pagination { get; set; }

        [JsonPropertyName("resources")]
        public App[] Apps { get; set; }
    }

    public class App
    {
        [JsonPropertyName("guid")]
        public string Guid { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("state")]
        public string State { get; set; }
        
        [JsonPropertyName("lifecycle")]
        public Lifecycle Lifecycle { get; set; }
    }

    public class Lifecycle
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }
        
        /** NOTE:
         * Cloud Controller v3.116.0 supports 3 valid lifecycle "type" values:
         * "buildpack"
         * "docker"
         * "kpack"
         * 
         * The following "Data" object is structured according to the "buildpack" 
         * type; deserialization may not succeed for different types.
         */

        [JsonPropertyName("data")]
        public Data Data { get; set; }
    }

    public class Data
    {
        [JsonPropertyName("buildpacks")]
        public string[] Buildpacks { get; set; }

        [JsonPropertyName("stack")]
        public string Stack { get; set; }
    }
}