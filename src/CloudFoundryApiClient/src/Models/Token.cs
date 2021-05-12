﻿namespace Tanzu.Toolkit.CloudFoundryApiClient.Models.Token
{
    public class Token
    {
        public string access_token { get; set; }
        public string token_type { get; set; }
        public string id_token { get; set; }
        public string refresh_token { get; set; }
        public int expires_in { get; set; }
        public string scope { get; set; }
        public string jti { get; set; }
    }

}