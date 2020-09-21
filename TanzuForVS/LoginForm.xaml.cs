namespace TanzuForVS
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Windows;
    using TanzuForVS.CloudFoundryApiClient;


    /// <summary>
    /// Interaction logic for LoginForm.xaml
    /// </summary>
    public partial class LoginForm : Window
    {
        public ToolWindowDataContext WindowDataContext = null;
        private ICfApiClient _cfApiClient = null;

        public LoginForm(ICfApiClient apiClient, ToolWindowDataContext dc)
        {
            InitializeComponent();

            _cfApiClient = apiClient;
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
            try
            {
                WindowDataContext.HasErrors = false;
                WindowDataContext.IsLoggedIn = false;
                string errorMessage = "failed to login";

                if (target == "")
                {
                    throw new Exception("Empty target URI");
                }
                else if (!Uri.IsWellFormedUriString(target, UriKind.Absolute))
                {
                    throw new Exception("Invalid target URI");
                }


                var result = await _cfApiClient.LoginAsync(target, username, password);

                if (result == null)
                {
                    throw new Exception(errorMessage);
                }
                else
                {
                    WindowDataContext.IsLoggedIn = true;
                    this.Close();
                }

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
