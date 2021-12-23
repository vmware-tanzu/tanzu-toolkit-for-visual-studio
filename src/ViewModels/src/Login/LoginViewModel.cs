using Microsoft.Extensions.DependencyInjection;
using System;
using System.Security;
using System.Threading.Tasks;
using Tanzu.Toolkit.Models;
using Tanzu.Toolkit.ViewModels.SsoDialog;

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
        private string _apiAddressError;
        private int _currentPageNum = 1;
        private bool _ssoEnabled = false;
        private bool _verifyingApiAddress = false;
        private bool _apiAddressIsValid;
        private string _connectionName;
        private bool _certificateInvalid = false;
        private bool _proceedWithInvalidCertificate = false;
        private ISsoDialogViewModel _ssoDialog;

        internal ITasExplorerViewModel _tasExplorer;

        public LoginViewModel(IServiceProvider services)
            : base(services)
        {
            SkipSsl = true;

            ApiAddressIsValid = true;

            _tasExplorer = services.GetRequiredService<ITasExplorerViewModel>();
            _ssoDialog = services.GetRequiredService<ISsoDialogViewModel>();
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

                // reset invalid cert warning when target api address changed
                CertificateInvalid = false;
                ProceedWithInvalidCertificate = false;

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

        public bool ApiAddressIsValid
        {
            get { return _apiAddressIsValid; }

            set
            {
                _apiAddressIsValid = value;
                RaisePropertyChangedEvent("ApiAddressIsValid");
            }
        }

        public string ApiAddressError
        {
            get { return _apiAddressError; }

            set
            {
                _apiAddressError = value;
                RaisePropertyChangedEvent("ApiAddressError");
            }
        }

        public int PageNum
        {
            get => _currentPageNum;

            internal set
            {
                _currentPageNum = value;
                RaisePropertyChangedEvent("PageNum");
            }
        }

        public bool SsoEnabledOnTarget
        {
            get => _ssoEnabled;

            private set
            {
                _ssoEnabled = value;
                RaisePropertyChangedEvent("SsoEnabledOnTarget");
            }
        }

        public bool VerifyingApiAddress
        {
            get => _verifyingApiAddress;

            private set
            {
                _verifyingApiAddress = value;
                RaisePropertyChangedEvent("VerifyingApiAddress");
            }
        }

        public bool CertificateInvalid
        {
            get => _certificateInvalid;

            set
            {
                _certificateInvalid = value;

                RaisePropertyChangedEvent("CertificateInvalid");
            }
        }

        public bool ProceedWithInvalidCertificate
        {
            get => _proceedWithInvalidCertificate;

            set
            {
                _proceedWithInvalidCertificate = value;

                RaisePropertyChangedEvent("ProceedWithInvalidCertificate");
            }
        }

        public Func<SecureString> GetPassword { get; set; }

        public Action ClearPassword { get; set; }

        public Func<bool> PasswordEmpty { get; set; }

        public bool CanLogIn(object arg = null)
        {
            return !(string.IsNullOrWhiteSpace(Target) || string.IsNullOrWhiteSpace(Username) || PasswordEmpty()) && ValidateApiAddress(Target);
        }

        public bool CanOpenSsoDialog(object arg = null)
        {
            return !string.IsNullOrWhiteSpace(Target) && ApiAddressIsValid;
        }

        public async Task LogIn(object arg)
        {
            HasErrors = false;

            if (!ValidateApiAddress(Target))
            {
                return;
            }

            var result = await CloudFoundryService.LoginWithCredentials(Target, Username, GetPassword(), skipSsl: ProceedWithInvalidCertificate);

            if (result.Succeeded)
            {
                ErrorMessage = null;

                SetConnection();

                DialogService.CloseDialog(arg, true);

                PageNum = 1;

                ClearPassword();
            }
            else
            {
                ErrorMessage = result.Explanation;
            }
        }

        public void SetConnection()
        {
            string name;

            if (string.IsNullOrWhiteSpace(ConnectionName))
            {
                try
                {
                    name = new Uri(Target).Host;
                }
                catch
                {
                    name = "Tanzu Application Service";
                }
            }
            else
            {
                name = ConnectionName;
            }

            _tasExplorer.SetConnection(new CloudFoundryInstance(name, Target));
        }

        public bool ValidateApiAddress(string apiAddress)
        {
            if (string.IsNullOrWhiteSpace(apiAddress))
            {
                ApiAddressError = TargetEmptyMessage;
                ApiAddressIsValid = false;

                return false;
            }
            else if (!Uri.IsWellFormedUriString(apiAddress, UriKind.Absolute))
            {
                ApiAddressError = TargetInvalidFormatMessage;
                ApiAddressIsValid = false;

                return false;
            }
            else
            {
                ApiAddressError = null;
                ApiAddressIsValid = true;

                return true;
            }
        }

        public async Task OpenSsoDialog(object arg = null)
        {
            if (string.IsNullOrWhiteSpace(Target))
            {
                ErrorMessage = "Must specify an API address to log in via SSO.";
            }
            else
            {
                var ssoPromptResult = await CloudFoundryService.GetSsoPrompt(Target);

                if (ssoPromptResult.Succeeded)
                {
                    var ssoUrlPrompt = ssoPromptResult.Content;

                    _ssoDialog.ApiAddress = Target;
                    _ssoDialog.ShowWithPrompt(ssoUrlPrompt, this);
                }
            }
        }

        public void CloseDialog()
        {
            DialogService.CloseDialogByName(nameof(LoginViewModel));
        }

        public async Task NavigateToAuthPage(object arg = null)
        {
            VerifyingApiAddress = true;

            var certTestResult = CloudFoundryService.TargetApi(Target, skipSsl: ProceedWithInvalidCertificate);

            if (certTestResult.Succeeded)
            {
                var ssoPromptResult = await CloudFoundryService.GetSsoPrompt(Target, skipSsl: ProceedWithInvalidCertificate);

                if (ssoPromptResult.Succeeded)
                {
                    SsoEnabledOnTarget = true;

                    PageNum = 2;
                }
                else
                {
                    switch (ssoPromptResult.FailureType)
                    {
                        case Toolkit.Services.FailureType.MissingSsoPrompt:
                            SsoEnabledOnTarget = false;
                            PageNum = 2;
                            break;

                        default:
                            ApiAddressError = $"Unable to establish a connection with {Target}";
                            ApiAddressIsValid = false;
                            break;
                    }
                }

            }
            else
            {
                if (certTestResult.FailureType == Toolkit.Services.FailureType.InvalidCertificate)
                {
                    CertificateInvalid = true;
                }
                else
                {
                    ApiAddressError = $"Unable to establish a connection with {Target}";

                    ApiAddressIsValid = false;
                }
            }
                
            VerifyingApiAddress = false;
        }

        public void NavigateToTargetPage(object arg = null)
        {
            PageNum = 1;
        }

        public bool CanProceedToAuthentication(object arg = null)
        {
            bool certValidOrBypassed = !CertificateInvalid || (CertificateInvalid && ProceedWithInvalidCertificate);

            return ApiAddressIsValid && !string.IsNullOrWhiteSpace(Target) && certValidOrBypassed;
        }
    }
}
