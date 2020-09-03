namespace TanzuForVS
{
    using CloudFoundry.UAA;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Interaction logic for CFLoginToolWindowControl.
    /// </summary>
    public partial class CFLoginToolWindowControl : UserControl
    {
        ICfApiClientFactory _cfApiClientFactory;
        public ErrorResource WindowDataContext = new ErrorResource() { ErrorMessage = null, HasErrors = false };

        /// <summary>
        /// Initializes a new instance of the <see cref="CFLoginToolWindowControl"/> class.
        /// </summary>
        public CFLoginToolWindowControl(ICfApiClientFactory cfApiClientFactory)
        {
            this.InitializeComponent();

            this.DataContext = WindowDataContext;
            _cfApiClientFactory = cfApiClientFactory;
        }

        /// <summary>
        /// Wrapper around async task ConnectToCFAsync
        /// </summary>
        private async void LoginBtn_Click(object sender, RoutedEventArgs e)
        {
            await ConnectToCFAsync(tbUrl.Text, tbUsername.Text, pbPassword.Password, "", true);
        }

        /// <summary>
        /// Tries to create a new Cloud Foundry API client & login to the target using the credentials provided.
        /// Exceptions are caught & applied to the DataContext to be displayed on the tool window.
        /// </summary>
        public async Task ConnectToCFAsync(string target, string username, string password, string httpProxy, bool skipSsl)
        {
            WindowDataContext.HasErrors = false;
            WindowDataContext.IsLoggedIn = false;

            try
            {
                Uri targetUri = new Uri(target);
                Uri httpProxyUri = null; // TODO: un-hardcode this later

                CloudCredentials credentials = new CloudCredentials();
                credentials.User = username;
                credentials.Password = password;

                IUAA cfApiV2Client = _cfApiClientFactory.CreateCfApiV2Client(targetUri, httpProxyUri, skipSsl);

                AuthenticationContext refreshToken = await cfApiV2Client.Login(credentials);
                WindowDataContext.IsLoggedIn = refreshToken.IsLoggedIn;
            } 
            catch (Exception ex)
            {
                var errorMessages = new List<string>();
                ErrorFormatter.FormatExceptionMessage(ex, errorMessages);
                WindowDataContext.ErrorMessage = string.Join(Environment.NewLine, errorMessages.ToArray());
                WindowDataContext.HasErrors = true;
            }
        }
    }
}