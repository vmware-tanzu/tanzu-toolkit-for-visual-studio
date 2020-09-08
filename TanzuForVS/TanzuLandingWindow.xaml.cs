namespace TanzuForVS
{
    using System;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Interaction logic for TanzuLandingWindow.
    /// </summary>
    public partial class CloudFoundryExplorer : UserControl
    {
        ICfApiClientFactory _cfApiClientFactory;
        public LoginWindowDataContext WindowDataContext = new LoginWindowDataContext() { ErrorMessage = null, HasErrors = false };
        public LoginForm LoginForm = null;

        public CloudFoundryExplorer(ICfApiClientFactory cfApiClientFactory)
        {
            this.InitializeComponent();

            this.DataContext = WindowDataContext;
            _cfApiClientFactory = cfApiClientFactory;
        }

        /// <summary>
        /// Opens LoginForm as a Dialog window
        /// </summary>
        private async void OpenLoginWindow_Click(object sender, RoutedEventArgs e)
        {
            LoginForm = new LoginForm(_cfApiClientFactory);

            // `Form.ShowDialog()` causes unit tests to hang, so we only call it if we're not running mstest
            if (!IsRunningMSTest())
            {
                LoginForm.ShowDialog();
            } 
        }
        
        private bool IsRunningMSTest()
        {
            return AppDomain.CurrentDomain.GetAssemblies().Any((x) => x.FullName.ToLower().Contains("mstest"));
        }
    }
}