using System;
using System.Collections.Generic;
using System.Text;

namespace TanzuForVS.CloudFoundryApiClient
{

    public class BasicInfoResponse
    {
        public Links links { get; set; }
    }

    public class Links
    {
        public Self self { get; set; }
        public object bits_service { get; set; }
        public Cloud_Controller_V2 cloud_controller_v2 { get; set; }
        public Cloud_Controller_V3 cloud_controller_v3 { get; set; }
        public Network_Policy_V0 network_policy_v0 { get; set; }
        public Network_Policy_V1 network_policy_v1 { get; set; }
        public Login login { get; set; }
        public Uaa uaa { get; set; }
        public object credhub { get; set; }
        public Routing routing { get; set; }
        public Logging logging { get; set; }
        public Log_Cache log_cache { get; set; }
        public Log_Stream log_stream { get; set; }
        public App_Ssh app_ssh { get; set; }
    }

    public class Self
    {
        public string href { get; set; }
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

    public class Network_Policy_V0
    {
        public string href { get; set; }
    }

    public class Network_Policy_V1
    {
        public string href { get; set; }
    }

    public class Login
    {
        public string href { get; set; }
    }

    public class Uaa
    {
        public string href { get; set; }
    }

    public class Routing
    {
        public string href { get; set; }
    }

    public class Logging
    {
        public string href { get; set; }
    }

    public class Log_Cache
    {
        public string href { get; set; }
    }

    public class Log_Stream
    {
        public string href { get; set; }
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
