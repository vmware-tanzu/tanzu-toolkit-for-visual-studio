﻿using Microsoft.Extensions.DependencyInjection;
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
        private bool _apiAddressIsValid;
        private string _connectionName;
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

            private set
            {
                _currentPageNum = value;
                RaisePropertyChangedEvent("PageNum");
            }
        }

        public bool SsoEnabledOnTarget
        {
            get { return _ssoEnabled; }
            private set
            {
                _ssoEnabled = value;
                RaisePropertyChangedEvent("SsoEnabledOnTarget");
            }
        }

        public Func<SecureString> GetPassword { get; set; }

        public Func<bool> PasswordEmpty { get; set; }

        public bool CanLogIn(object arg = null)
        {
            return !(string.IsNullOrWhiteSpace(Target) || string.IsNullOrWhiteSpace(Username) || PasswordEmpty()) && VerifyApiAddress(Target);
        }

        public bool CanOpenSsoDialog(object arg = null)
        {
            return !string.IsNullOrWhiteSpace(Target) && ApiAddressIsValid;
        }

        public async Task LogIn(object arg)
        {
            HasErrors = false;

            if (!VerifyApiAddress(Target))
            {
                return;
            }

            var result = await CloudFoundryService.LoginWithCredentials(Target, Username, GetPassword(), SkipSsl);
            ErrorMessage = result.ErrorMessage;

            if (result.IsLoggedIn)
            {
                SetConnection();
            }

            if (!HasErrors)
            {
                DialogService.CloseDialog(arg, true);
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

        public bool VerifyApiAddress(string apiAddress)
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
                HasErrors = true;
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
            var ssoPromptResult = await CloudFoundryService.GetSsoPrompt(Target);

            SsoEnabledOnTarget = ssoPromptResult.Succeeded;

            PageNum = 2;
        }

        public void NavigateToTargetPage(object arg = null)
        {
            PageNum = 1;
        }

        public bool CanProceedToAuthentication(object arg = null)
        {
            return ApiAddressIsValid && !string.IsNullOrWhiteSpace(Target);
        }
    }
}
