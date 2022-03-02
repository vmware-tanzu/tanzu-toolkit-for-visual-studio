using Microsoft.Extensions.DependencyInjection;
using System;
using System.Runtime.CompilerServices;
using System.Security;
using System.Threading.Tasks;
using Tanzu.Toolkit.Models;
using Tanzu.Toolkit.Services;
using Tanzu.Toolkit.Services.CloudFoundry;
using Tanzu.Toolkit.ViewModels.SsoDialog;

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
        private bool _verifyingApiAddress = false;
        private bool _apiAddressIsValid = true;
        private bool _certificateInvalid = false;
        private ISsoDialogViewModel _ssoDialog;

        internal ITasExplorerViewModel _tasExplorer;

        public LoginViewModel(IServiceProvider services)
            : base(services)
        {
            _tasExplorer = services.GetRequiredService<ITasExplorerViewModel>();
            _ssoDialog = services.GetRequiredService<ISsoDialogViewModel>();
            CfClient = Services.GetRequiredService<ICloudFoundryService>();
        }

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

        public Func<SecureString> GetPassword { get; set; }

        public Action ClearPassword { get; set; }

        public Func<bool> PasswordEmpty { get; set; }

        internal ICloudFoundryService CfClient { get; set; }

        public bool CanLogIn(object arg = null)
        {
            return !(string.IsNullOrWhiteSpace(TargetApiAddress) || string.IsNullOrWhiteSpace(Username) || PasswordEmpty()) && ValidateApiAddressFormat(TargetApiAddress);
        }

        public bool CanOpenSsoDialog(object arg = null)
        {
            return !string.IsNullOrWhiteSpace(TargetApiAddress) && ApiAddressIsValid;
        }

        public async Task VerifyApiAddress(object arg = null)
        {
            VerifyingApiAddress = true;

            var candidateCf = new CloudFoundryInstance(GetTargetDisplayName(), TargetApiAddress, SkipSsl);
            var newCfClient = Services.GetRequiredService<ICloudFoundryService>();
            var successfullyTargetedCf = newCfClient.ConfigureForCf(candidateCf).Succeeded;
            if (!successfullyTargetedCf)
            {
                Fail();
                return;
            }

            CfClient = newCfClient;
            var certTestResult = CfClient.VerfiyNewApiConnection(TargetApiAddress, SkipSsl);
            if (!certTestResult.Succeeded)
            {
                Fail(certTestResult.FailureType);
                return;
            }

            var ssoPromptResult = await CfClient.GetSsoPrompt(TargetApiAddress, SkipSsl);
            if (!ssoPromptResult.Succeeded && ssoPromptResult.FailureType != FailureType.MissingSsoPrompt)
            {
                Fail(ssoPromptResult.FailureType);
                return;
            }

            SsoEnabledOnTarget = ssoPromptResult.Succeeded;
            TargetCf = candidateCf;
            PageNum = 2;
            VerifyingApiAddress = false;

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
                        ApiAddressIsValid = false;
                        break;
                }

                TargetCf = null;
                VerifyingApiAddress = false;
            }
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

        public bool ValidateApiAddressFormat(string apiAddress)
        {
            if (Uri.IsWellFormedUriString(apiAddress, UriKind.Absolute) || string.IsNullOrWhiteSpace(apiAddress))
            {
                ApiAddressError = null;
                ApiAddressIsValid = true;

                return true;
            }
            else
            {
                ApiAddressError = TargetInvalidFormatMessage;
                ApiAddressIsValid = false;

                return false;
            }
        }

        public async Task OpenSsoDialog(object arg = null)
        {
            if (string.IsNullOrWhiteSpace(TargetApiAddress))
            {
                ErrorMessage = "Must specify an API address to log in via SSO.";
            }
            else
            {
                var ssoPromptResult = await CfClient.GetSsoPrompt(TargetApiAddress);

                if (ssoPromptResult.Succeeded)
                {
                    var ssoUrlPrompt = ssoPromptResult.Content;

                    _ssoDialog.ApiAddress = TargetApiAddress;
                    _ssoDialog.ShowWithPrompt(ssoUrlPrompt, this);
                }
            }
        }

        public void CloseDialog()
        {
            DialogService.CloseDialogByName(nameof(LoginViewModel));
        }

        public void NavigateToTargetPage(object arg = null)
        {
            PageNum = 1;

            // clear previous login errors
            ErrorMessage = null;
        }

        public bool CanProceedToAuthentication(object arg = null)
        {
            bool certValidOrBypassed = !CertificateInvalid || (CertificateInvalid && SkipSsl);
            return ApiAddressIsValid && !string.IsNullOrWhiteSpace(TargetApiAddress) && certValidOrBypassed;
        }

        public void ResetTargetDependentFields()
        {
            // reset invalid cert warning
            CertificateInvalid = false;

            // clear previous creds
            Username = null;
            ClearPassword();

            // clear previous errors
            ErrorMessage = null;
        }

        private string GetTargetDisplayName()
        {
            if (!string.IsNullOrWhiteSpace(ConnectionName)) return ConnectionName;

            var targetAddressValidUri = Uri.TryCreate(TargetApiAddress, UriKind.Absolute, out Uri uri);
            return targetAddressValidUri ? uri.Host : "Tanzu Application Service";
        }
    }
}
