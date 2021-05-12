namespace Tanzu.Toolkit.CloudFoundryApiClient.Models.Token
{
    public class Token
    {
        public string Access_token { get; set; }
        public string Token_type { get; set; }
        public string Id_token { get; set; }
        public string Refresh_token { get; set; }
        public int Expires_in { get; set; }
        public string Scope { get; set; }
        public string Jti { get; set; }
    }
}