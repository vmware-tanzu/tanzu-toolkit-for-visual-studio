using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;
using TanzuForVS.CloudFoundryApiClient;


namespace TanzuForVS.Services.CloudFoundry
{
    public class CloudFoundryService : ICloudFoundryService
    {
        public string LoginFailureMessage { get; }  = "Login failed.";
        private static ICfApiClient _cfApiClient;

        public CloudFoundryService(IServiceProvider services)
        {
            _cfApiClient = services.GetRequiredService<ICfApiClient>();
        }

        public bool IsLoggedIn { get; set; } = false;


        public async Task<ConnectResult> ConnectToCFAsync(string target, string username, SecureString password, string httpProxy, bool skipSsl)
        {
            if (string.IsNullOrEmpty(target)) throw new ArgumentException(nameof(target));

            if (string.IsNullOrEmpty(username)) throw new ArgumentException(nameof(username));

            if (password == null) throw new ArgumentNullException(nameof(password));

            try
            {
                // TODO: don't let password be passed around as a regular string
                // TODO: test that errors that may have been thrown in CfApiClient get passed
                //       through to this level & get loaded into the ConnectResult.ErrorMessage
                string passwordStr = new System.Net.NetworkCredential(string.Empty, password).Password;

                string AccessToken = await _cfApiClient.LoginAsync(target, username, passwordStr);

                if (!string.IsNullOrEmpty(AccessToken)) return new ConnectResult(true, null);
                throw new Exception(LoginFailureMessage);
            }
            catch (Exception e)
            {
                var errorMessages = new List<string>();
                FormatExceptionMessage(e, errorMessages);
                var errorMessage = string.Join(Environment.NewLine, errorMessages.ToArray());
                return new ConnectResult(false, errorMessage);
            }
        }

        public static void FormatExceptionMessage(Exception ex, List<string> message)
        {
            var aex = ex as AggregateException;
            if (aex != null)
            {
                foreach (Exception iex in aex.InnerExceptions)
                {
                    FormatExceptionMessage(iex, message);
                }
            }
            else
            {
                message.Add(ex.Message);

                if (ex.InnerException != null)
                {
                    FormatExceptionMessage(ex.InnerException, message);
                }
            }
        }
    }
}
