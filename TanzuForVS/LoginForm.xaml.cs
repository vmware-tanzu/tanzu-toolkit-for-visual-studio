namespace TanzuForVS
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Windows;
    using CloudFoundry.UAA;
  

    /// <summary>
    /// Interaction logic for LoginForm.xaml
    /// </summary>
    public partial class LoginForm : Window
    {   
        ICfApiClientFactory _cfApiClientFactory;
        public ToolWindowDataContext WindowDataContext = null;

        public LoginForm(ICfApiClientFactory cfApiClientFactory, ToolWindowDataContext dc)
        {
            InitializeComponent();

            _cfApiClientFactory = cfApiClientFactory;
            WindowDataContext = dc;
            this.DataContext = WindowDataContext;
        }

        /// <summary>
        /// Wrapper around async task ConnectToCFAsync to minimize logic within this
        /// `async void` click handler method (unhandled exceptions will crash the process)
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
                if (refreshToken.IsLoggedIn) this.Close();
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
