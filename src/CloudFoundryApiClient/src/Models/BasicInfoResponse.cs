namespace Tanzu.Toolkit.CloudFoundryApiClient.Models.BasicInfoResponse
{
    public class BasicInfoResponse
    {
        public Links Links { get; set; }
    }

    public class Links
    {
        public HypertextReference Self { get; set; }
        public object Bits_service { get; set; }
        public Cloud_Controller_V2 Cloud_controller_v2 { get; set; }
        public Cloud_Controller_V3 Cloud_controller_v3 { get; set; }
        public HypertextReference Network_policy_v0 { get; set; }
        public HypertextReference Network_policy_v1 { get; set; }
        public HypertextReference Login { get; set; }
        public HypertextReference Uaa { get; set; }
        public object Credhub { get; set; }
        public HypertextReference Routing { get; set; }
        public HypertextReference Logging { get; set; }
        public HypertextReference Log_cache { get; set; }
        public HypertextReference Log_stream { get; set; }
        public App_Ssh App_ssh { get; set; }
    }

    public class Cloud_Controller_V2
    {
        public string Href { get; set; }
        public Meta Meta { get; set; }
    }

    public class Meta
    {
        public string Version { get; set; }
    }

    public class Cloud_Controller_V3
    {
        public string Href { get; set; }
        public Meta1 Meta { get; set; }
    }

    public class Meta1
    {
        public string Version { get; set; }
    }

    public class App_Ssh
    {
        public string Href { get; set; }
        public Meta2 Meta { get; set; }
    }

    public class Meta2
    {
        public string Host_key_fingerprint { get; set; }
        public string Oauth_client { get; set; }
    }
}
