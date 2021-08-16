using System;
using System.Security;
using System.Threading.Tasks;

namespace Tanzu.Toolkit.ViewModels
{
    public class LoginViewModel : AbstractViewModel, ILoginViewModel
    {
        public const string TargetEmptyMessage = "Invalid URI: The URI is empty.";
        public const string TargetInvalidFormatMessage = "Invalid URI: The format of the URI could not be determined.";
        private string _target;
        private string _username;
        private string _httpProxy;
        private bool _skipSsl;
        private bool _hasErrors;
        private string _errorMessage;
        private string _instanceName;

        public LoginViewModel(IServiceProvider services)
            : base(services)
        {
            SkipSsl = true;
        }

        public string InstanceName
        {
            get => _instanceName;

            set
            {
                _instanceName = value;
                RaisePropertyChangedEvent("InstanceName");
            }
        }

        public string Target
        {
            get => _target;

            set
            {
                _target = value;
                RaisePropertyChangedEvent("Target");
            }
        }

        public string Username
        {
            get => _username;

            set
            {
                _username = value;
                RaisePropertyChangedEvent("Username");
            }
        }

        public string HttpProxy
        {
            get => _httpProxy;

            set
            {
                _httpProxy = value;
                RaisePropertyChangedEvent("HttpProxy");
            }
        }

        public bool SkipSsl
        {
            get => _skipSsl;

            set
            {
                _skipSsl = value;
                RaisePropertyChangedEvent("SkipSsl");
            }
        }

        public bool HasErrors
        {
            get => _hasErrors;

            set
            {
                _hasErrors = value;
                RaisePropertyChangedEvent("HasErrors");
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;

            set
            {
                _errorMessage = value;
                if (!string.IsNullOrEmpty(value))
                {
                    HasErrors = true;
                }

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

            if (!VerifyTarget())
            {
                return;
            }

            var result = await CloudFoundryService.ConnectToCFAsync(Target, Username, GetPassword(), HttpProxy, SkipSsl);
            ErrorMessage = result.ErrorMessage;

            if (result.IsLoggedIn)
            {
                try
                {
                    CloudFoundryService.AddCloudFoundryInstance(InstanceName, Target);
                }
                catch (Exception e)
                {
                    ErrorMessage = e.Message;
                    HasErrors = true;
                }
            }

            if (!HasErrors)
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
            catch
            {
                ErrorMessage = TargetInvalidFormatMessage;
                return false;
            }

            return true;
        }
    }
}
