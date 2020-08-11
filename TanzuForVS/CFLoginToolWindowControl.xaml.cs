namespace TanzuForVS
{
    using CloudFoundry.CloudController.V2.Client;
    using CloudFoundry.CloudController.V2.Client.Data;
    using CloudFoundry.UAA;
    using System;
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Interaction logic for ToolWindow1Control.
    /// </summary>
    public partial class CloudFoundryToolWindowControl : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ToolWindow1Control"/> class.
        /// </summary>
        public CloudFoundryToolWindowControl()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Login to a CF instance and print app names to the console
        /// </summary>
        private async void ConnectToCFAsync(object sender, RoutedEventArgs e)
        {
            Uri target = new Uri(this.tbUrl.Text); // TODO: handle a poorly formatted URI here!
            Uri httpProxy = null;
            bool skipSsl = (bool)this.cbIgnoreSSK.IsChecked;

            CloudFoundryClient v3client = new CloudFoundryClient(target, new System.Threading.CancellationToken(), httpProxy, skipSsl);
            AuthenticationContext refreshToken = null;

            CloudCredentials credentials = new CloudCredentials();
            credentials.User = this.tbUsername.Text;
            credentials.Password = this.pbPassword.Password;

            try
            {
                refreshToken = await v3client.Login(credentials);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            PagedResponseCollection<ListAllAppsResponse> apps = await v3client.Apps.ListAllApps();
            foreach (ListAllAppsResponse app in apps)
            {
                Console.WriteLine(app.Name);
            }
        }

    }
}