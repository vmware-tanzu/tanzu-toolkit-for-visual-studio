namespace TanzuForVS
{
    using CloudFoundry.CloudController.V2.Client;
    using CloudFoundry.CloudController.V2.Client.Data;
    using CloudFoundry.UAA;
    using System;
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Interaction logic for CFLoginToolWindowControl.
    /// </summary>
    public partial class CFLoginToolWindowControl : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CFLoginToolWindowControl"/> class.
        /// </summary>
        public CFLoginToolWindowControl()
        {
            this.InitializeComponent();
            this.DataContext = new ErrorResource();
        }

        /// <summary>
        /// Login to a CF instance and print app names to the console
        /// </summary>
        private async void ConnectToCFAsync(object sender, RoutedEventArgs e)
        {

            var errorResource = this.DataContext as ErrorResource;
            if (errorResource == null)
            {
                throw new InvalidOperationException("Invalid DataContext");
            }
            errorResource.HasErrors = false;

            try
            {
                Uri target = new Uri(this.tbUrl.Text); 
                Uri httpProxy = null;
                bool skipSsl = true;

                CloudFoundryClient v3client = new CloudFoundryClient(target, new System.Threading.CancellationToken(), httpProxy, skipSsl);
                AuthenticationContext refreshToken = null;

                CloudCredentials credentials = new CloudCredentials();
                credentials.User = this.tbUsername.Text;
                credentials.Password = this.pbPassword.Password;

                refreshToken = await v3client.Login(credentials);
           
                PagedResponseCollection<ListAllAppsResponse> apps = await v3client.Apps.ListAllApps();
                foreach (ListAllAppsResponse app in apps)
                {
                    Console.WriteLine(app.Name);
                }
            }
            catch (Exception ex)
            {
                var errorMessages = new List<string>();
                ErrorFormatter.FormatExceptionMessage(ex, errorMessages);
                errorResource.ErrorMessage = string.Join(Environment.NewLine, errorMessages.ToArray());
                errorResource.HasErrors = true;
            }
        }

    }
}