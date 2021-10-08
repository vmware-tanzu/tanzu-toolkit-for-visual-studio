using Microsoft.Extensions.DependencyInjection;
using System;
using System.Security;
using System.Threading.Tasks;
using Tanzu.Toolkit.Models;

namespace Tanzu.Toolkit.ViewModels
{
    public class LoginViewModel : AbstractViewModel, ILoginViewModel
    {
        public const string TargetEmptyMessage = "Invalid URI: The URI is empty.";
        public const string TargetInvalidFormatMessage = "Invalid URI: The format of the URI could not be determined.";
        private string _target;
        private string _username;
        private bool _skipSsl;
        private bool _hasErrors;
        private string _errorMessage;
        private string _connectionName;
        private ITasExplorerViewModel _tasExplorer;

        public LoginViewModel(IServiceProvider services)
            : base(services)
        {
            SkipSsl = true;

            _tasExplorer = services.GetRequiredService<ITasExplorerViewModel>();
        }

        public string ConnectionName
        {
            get => _connectionName;

            set
            {
                _connectionName = value;
                RaisePropertyChangedEvent("ConnectionName");
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

        public Func<bool> PasswordEmpty { get; set; }

        public bool CanLogIn(object arg = null)
        {
            return !(string.IsNullOrWhiteSpace(ConnectionName) || string.IsNullOrWhiteSpace(Target) || string.IsNullOrWhiteSpace(Username) || PasswordEmpty());
        }

        public async Task LogIn(object arg)
        {
            HasErrors = false;

            if (!VerifyTarget())
            {
                return;
            }

            var result = await CloudFoundryService.ConnectToCFAsync(Target, Username, GetPassword(), SkipSsl);
            ErrorMessage = result.ErrorMessage;

            if (result.IsLoggedIn)
            {
                _tasExplorer.SetConnection(new CloudFoundryInstance(ConnectionName, Target));
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
