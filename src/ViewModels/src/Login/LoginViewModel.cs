using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security;
using System.Threading.Tasks;
using Tanzu.Toolkit.Models;
using Tanzu.Toolkit.Services;
using Tanzu.Toolkit.Services.CloudFoundry;

[assembly: InternalsVisibleTo("Tanzu.Toolkit.ViewModels.Tests")]

namespace Tanzu.Toolkit.ViewModels
{
    public class LoginViewModel : AbstractViewModel, ILoginViewModel
    {
        public const string TargetEmptyMessage = "Invalid URI: The URI is empty.";
        public const string TargetInvalidFormatMessage = "Invalid URI: The format of the URI could not be determined.";
        private string _target;
        private string _username;
        private bool _skipSsl = false;
        private bool _hasErrors;
        private string _errorMessage;
        private string _apiAddressError;
        private int _currentPageNum = 1;
        private bool _ssoEnabled = false;
        private bool _connectingToCf = false;
        private bool _isApiAddressFormatValid;
        private bool _certificateInvalid = false;
        internal ITasExplorerViewModel _tasExplorer;
        private string _ssoLink;

        public LoginViewModel(IServiceProvider services)
            : base(services)
        {
            IsApiAddressFormatValid = false;
            _tasExplorer = services.GetRequiredService<ITasExplorerViewModel>();
            CfClient = Services.GetRequiredService<ICloudFoundryService>();
        }

        // Properties //

        public CloudFoundryInstance TargetCf { get; set; }

        public string ConnectionName { get; set; }

        public string TargetApiAddress
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
                HasErrors = !string.IsNullOrWhiteSpace(value);
                RaisePropertyChangedEvent("ErrorMessage");
            }
        }

        public bool IsApiAddressFormatValid
        {
            get { return _isApiAddressFormatValid; }

            set
            {
                _isApiAddressFormatValid = value;
                RaisePropertyChangedEvent("IsApiAddressFormatValid");
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

        public string SsoLink
        {
            get => _ssoLink;

            set
            {
                _ssoLink = value;
                RaisePropertyChangedEvent("SsoLink");
            }
        }

        public bool ConnectingToCf
        {
            get => _connectingToCf;

            private set
            {
                _connectingToCf = value;
                RaisePropertyChangedEvent("ConnectingToCf");
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

        public Func<SecureString> GetPassword { get; set; }

        public Action ClearPassword { get; set; }

        public Func<bool> PasswordEmpty { get; set; }

        public string SsoPasscode { get; set; }

        // Methods //

        public async Task ConnectToCf(object arg = null)
        {
            ConnectingToCf = true;
            SsoEnabledOnTarget = false;

            var candidateCf = new CloudFoundryInstance(GetTargetDisplayName(), TargetApiAddress, SkipSsl);
            var newCfClient = Services.GetRequiredService<ICloudFoundryService>();
            var successfullyTargetedCf = newCfClient.ConfigureForCf(candidateCf).Succeeded;
            if (!successfullyTargetedCf)
            {
                Fail();
                return;
            }

            CfClient = newCfClient;
            var certTestResult = CfClient.TargetCfApi(TargetApiAddress, SkipSsl);
            if (!certTestResult.Succeeded)
            {
                Fail(certTestResult.FailureType);
                return;
            }

            var ssoPromptResult = await CfClient.GetSsoPrompt(TargetApiAddress, SkipSsl);
            if (ssoPromptResult.Succeeded)
            {
                try
                {
                    var prompt = ssoPromptResult.Content;
                    var promptComponents = prompt.Split(' ');
                    var linkStr = promptComponents.First(item => Uri.IsWellFormedUriString(item, UriKind.Absolute));
                    SsoEnabledOnTarget = true;
                    SsoLink = linkStr;
                }
                catch (Exception ex)
                {
                    Logger.Error("Unable to extract sso link from prompt \"{SsoPrompt}\". {SsoPromptException}", ssoPromptResult.Content, ex);
                }
            }
            else if (ssoPromptResult.FailureType != FailureType.MissingSsoPrompt)
            {
                Fail(ssoPromptResult.FailureType);
                return;
            }

            TargetCf = candidateCf;
            PageNum = 2;
            ConnectingToCf = false;

            // local helper
            void Fail(FailureType failureType = FailureType.None)
            {
                switch (failureType)
                {
                    case FailureType.InvalidCertificate:
                        CertificateInvalid = true;
                        break;
                    case FailureType.MissingSsoPrompt:
                        SsoEnabledOnTarget = false;
                        PageNum = 2;
                        break;
                    default:
                        ApiAddressError = $"Unable to establish a connection with {TargetApiAddress}";
                        break;
                }

                TargetCf = null;
                ConnectingToCf = false;
            }
        }

        public bool ValidateApiAddressFormat(string apiAddress)
        {
            if (Uri.IsWellFormedUriString(apiAddress, UriKind.Absolute) || string.IsNullOrWhiteSpace(apiAddress))
            {
                ApiAddressError = null;
                IsApiAddressFormatValid = true;

                return true;
            }
            else if (string.IsNullOrWhiteSpace(apiAddress))
            {
                ApiAddressError = null;
                IsApiAddressFormatValid = false;

                return false;
            }
            else
            {
                ApiAddressError = TargetInvalidFormatMessage;
                IsApiAddressFormatValid = false;

                return false;
            }
        }

        public void NavigateToTargetPage(object arg = null)
        {
            PageNum = 1;

            // clear previous login errors
            ErrorMessage = null;
        }

        public void ShowSsoLogin(object arg = null)
        {
            SsoPasscode = null; // clear previous entry
            PageNum = 3;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public async Task LogIn(object arg = null)
        {
            HasErrors = false;

            if (!ValidateApiAddressFormat(TargetApiAddress))
            {
                return;
            }

            var result = await CfClient.LoginWithCredentials(Username, GetPassword());

            if (result.Succeeded)
            {
                ErrorMessage = null;
                _tasExplorer.SetConnection(TargetCf);
                DialogService.CloseDialog(arg, true);
                PageNum = 1;
                ClearPassword();
            }
            else
            {
                ErrorMessage = result.Explanation;
            }
        }

        public async Task LoginWithSsoPasscodeAsync(object arg = null)
        {
            HasErrors = false;

            if (string.IsNullOrWhiteSpace(SsoPasscode))
            {
                ErrorMessage = "Passcode cannot be empty.";
            }
            else if (TargetCf == null)
            {
                ErrorMessage = "Unable to contact specified api address; please close this window and try again.\nIf this issue persists, please email tas-vs-extension@vmware.com";
            }
            else
            {
                var loginResult = await CfClient.LoginWithSsoPasscode(TargetCf.ApiAddress, SsoPasscode);

                if (loginResult.Succeeded)
                {
                    _tasExplorer.SetConnection(TargetCf);
                    CloseDialog();
                }
                else
                {
                    ErrorMessage = loginResult.Explanation;
                }
            }
        }

        public void CloseDialog()
        {
            DialogService.CloseDialogByName(nameof(LoginViewModel));
        }

        public void ResetTargetDependentFields()
        {
            // reset target-specific values
            CertificateInvalid = false;
            SsoEnabledOnTarget = false;
            SsoLink = null;

            // clear previous creds
            Username = null;
            ClearPassword();
            SsoPasscode = null;

            // clear previous errors
            ErrorMessage = null;
        }

        private string GetTargetDisplayName()
        {
            if (!string.IsNullOrWhiteSpace(ConnectionName)) return ConnectionName;

            var targetAddressValidUri = Uri.TryCreate(TargetApiAddress, UriKind.Absolute, out var uri);
            return targetAddressValidUri ? uri.Host : "Tanzu Application Service";
        }

        // Predicates //
        public bool CanLogIn(object arg = null)
        {
            return !(string.IsNullOrWhiteSpace(TargetApiAddress) || string.IsNullOrWhiteSpace(Username) || PasswordEmpty()) && ValidateApiAddressFormat(TargetApiAddress);
        }

        public bool CanProceedToAuthentication(object arg = null)
        {
            bool certValidOrBypassed = !CertificateInvalid || (CertificateInvalid && SkipSsl);
            return IsApiAddressFormatValid && certValidOrBypassed;
        }
    }
}
