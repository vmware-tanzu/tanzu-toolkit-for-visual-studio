using System;
using System.Security;
using System.Threading.Tasks;

namespace TanzuForVS.ViewModels
{
    public class LoginDialogViewModel : AbstractViewModel, ILoginDialogViewModel
    {
        public const string TargetEmptyMessage = "Invalid URI: The URI is empty.";
        public const string TargetInvalidFormatMessage = "Invalid URI: The format of the URI could not be determined.";
        private string target;
        private string username;
        private string httpProxy;
        private bool skipSsl;
        private bool hasErrors;
        private string errorMessage;

        public LoginDialogViewModel(IServiceProvider services)
            : base(services)
        {
        }

        public string Target
        {
            get => target;

            set
            {
                this.target = value;
                this.RaisePropertyChangedEvent("Target");
            }
        }

        public string Username
        {
            get => username;

            set
            {
                this.username = value;
                this.RaisePropertyChangedEvent("Username");
            }
        }

        public string HttpProxy
        {
            get => httpProxy;

            set
            {
                this.httpProxy = value;
                this.RaisePropertyChangedEvent("HttpProxy");
            }
        }

        public bool SkipSsl
        {
            get => skipSsl;

            set
            {
                this.skipSsl = value;
                this.RaisePropertyChangedEvent("SkipSsl");
            }
        }

        public bool HasErrors
        {
            get => hasErrors;

            set
            {
                this.hasErrors = value;
                this.RaisePropertyChangedEvent("HasErrors");
            }
        }

        public string ErrorMessage
        {
            get => this.errorMessage;

            set
            {
                this.errorMessage = value;
                if (!string.IsNullOrEmpty(value)) HasErrors = true;
                this.RaisePropertyChangedEvent("ErrorMessage");
            }
        }

        public Func<SecureString> GetPassword { get; set; }

        public bool CanConnectToCloudFoundry(object arg)
        {
            return true;
        }


       
        public async Task ConnectToCloudFoundry(object arg)
        {
            HasErrors = false;
            // TODO: Validate all parameters are valid
            if (!VerifyTarget())
            {
                return;
            }

            try
            {
                var result = await CloudFoundryService.ConnectToCFAsync(Target, Username, GetPassword(), HttpProxy, SkipSsl);
                ErrorMessage = result.ErrorMessage;
                IsLoggedIn = result.IsLoggedIn;
            }
            finally
            {
                DialogService.CloseDialog(arg, true);
            }
        }

        private bool VerifyTarget()
        {
            if (string.IsNullOrEmpty(Target))
            {
                ErrorMessage = TargetEmptyMessage;
                return false;
            }

            try
            {
                var uri = new Uri(Target);
            } 
            catch (Exception e)
            {
                ErrorMessage = TargetInvalidFormatMessage;
                return false;
            }

            return true;
        }

    }
}

