namespace Tanzu.Toolkit.CloudFoundryApiClient.Models.BasicInfoResponse
{

    public class BasicInfoResponse
    {
        public Links links { get; set; }
    }

    public class Links
    {
        public Href self { get; set; }
        public object bits_service { get; set; }
        public Cloud_Controller_V2 cloud_controller_v2 { get; set; }
        public Cloud_Controller_V3 cloud_controller_v3 { get; set; }
        public Href network_policy_v0 { get; set; }
        public Href network_policy_v1 { get; set; }
        public Href login { get; set; }
        public Href uaa { get; set; }
        public object credhub { get; set; }
        public Href routing { get; set; }
        public Href logging { get; set; }
        public Href log_cache { get; set; }
        public Href log_stream { get; set; }
        public App_Ssh app_ssh { get; set; }
    }

    public class Cloud_Controller_V2
    {
        public string href { get; set; }
        public Meta meta { get; set; }
    }

    public class Meta
    {
        public string version { get; set; }
    }

    public class Cloud_Controller_V3
    {
        public string href { get; set; }
        public Meta1 meta { get; set; }
    }

    public class Meta1
    {
        public string version { get; set; }
    }

    public class App_Ssh
    {
        public string href { get; set; }
        public Meta2 meta { get; set; }
    }

    public class Meta2
    {
        public string host_key_fingerprint { get; set; }
        public string oauth_client { get; set; }
    }

}
