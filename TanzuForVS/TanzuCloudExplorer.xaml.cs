namespace TanzuForVS
{
    using System;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Interaction logic for TanzuCloudExplorer.
    /// </summary>
    public partial class TanzuCloudExplorer : UserControl
    {
        ICfApiClientFactory _cfApiClientFactory;
        public LoginForm LoginForm = null;
        private ToolWindowDataContext _dataContext = new ToolWindowDataContext()
        { ErrorMessage = null, HasErrors = false, IsLoggedIn = false };

        public TanzuCloudExplorer(ICfApiClientFactory cfApiClientFactory)
        {
            this.InitializeComponent();

            this.DataContext = _dataContext;
            _cfApiClientFactory = cfApiClientFactory;
        }

        /// <summary>
        /// Opens LoginForm as a Dialog window
        /// </summary>
        private async void OpenLoginWindow_Click(object sender, RoutedEventArgs e)
        {
            LoginForm = new LoginForm(_cfApiClientFactory, _dataContext);

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