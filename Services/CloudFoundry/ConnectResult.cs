namespace TanzuForVS.Services.CloudFoundry
{
    public class ConnectResult
    {
        public ConnectResult(bool loggedIn, string errorMessage, string token)
        {
            IsLoggedIn = loggedIn;
            ErrorMessage = errorMessage;
            Token = token;
        }

        public bool IsLoggedIn { get; }
        public string ErrorMessage { get; }
        public string Token { get; set; }
    }
}
