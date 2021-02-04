using System;
using System.Security;
using System.Threading.Tasks;

namespace Tanzu.Toolkit.VisualStudio.ViewModels
{
    public class AddCloudDialogViewModel : AbstractViewModel, IAddCloudDialogViewModel
    {
        public const string TargetEmptyMessage = "Invalid URI: The URI is empty.";
        public const string TargetInvalidFormatMessage = "Invalid URI: The format of the URI could not be determined.";
        private string target;
        private string username;
        private string httpProxy;
        private bool skipSsl;
        private bool hasErrors;
        private string errorMessage;
        private string instanceName;


        public AddCloudDialogViewModel(IServiceProvider services)
            : base(services)
        {
            SkipSsl = true;
        }

        public string InstanceName
        {
            get => instanceName;

            set
            {
                instanceName = value;
                RaisePropertyChangedEvent("InstanceName");
            }
        }

        public string Target
        {
            get => target;

            set
            {
                target = value;
                RaisePropertyChangedEvent("Target");
            }
        }

        public string Username
        {
            get => username;

            set
            {
                username = value;
                RaisePropertyChangedEvent("Username");
            }
        }

        public string HttpProxy
        {
            get => httpProxy;

            set
            {
                httpProxy = value;
                RaisePropertyChangedEvent("HttpProxy");
            }
        }

        public bool SkipSsl
        {
            get => skipSsl;

            set
            {
                skipSsl = value;
                RaisePropertyChangedEvent("SkipSsl");
            }
        }

        public bool HasErrors
        {
            get => hasErrors;

            set
            {
                hasErrors = value;
                RaisePropertyChangedEvent("HasErrors");
            }
        }

        public string ErrorMessage
        {
            get => errorMessage;

            set
            {
                errorMessage = value;
                if (!string.IsNullOrEmpty(value)) HasErrors = true;
                RaisePropertyChangedEvent("ErrorMessage");
            }
        }

        public Func<SecureString> GetPassword { get; set; }

        public bool CanAddCloudFoundryInstance(object arg)
        {
            return true;
        }

        public async Task AddCloudFoundryInstance(object arg)
        {
            HasErrors = false;

            if (!VerifyTarget()) return;

            var result = await CloudFoundryService.ConnectToCFAsync(Target, Username, GetPassword(), HttpProxy, SkipSsl);
            ErrorMessage = result.ErrorMessage;

            if (result.IsLoggedIn)
            {
                try
                {
                    CloudFoundryService.AddCloudFoundryInstance(InstanceName, Target, result.Token);
                }
                catch (Exception e)
                {
                    ErrorMessage = e.Message;
                    HasErrors = true;
                }
            }

            if (!HasErrors) DialogService.CloseDialog(arg, true);
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
            catch
            {
                ErrorMessage = TargetInvalidFormatMessage;
                return false;
            }

            return true;
        }

    }
}

