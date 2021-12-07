using System;
using System.Threading.Tasks;

namespace Tanzu.Toolkit.ViewModels.SsoDialog
{
    public class SsoDialogViewModel : AbstractViewModel, ISsoDialogViewModel
    {
        private string _prompt;
        private string _passcode;
        private bool _hasErrors;
        private string _errorMessage;
        private string _apiAddress;

        public SsoDialogViewModel(IServiceProvider services) : base(services)
        {
            HasErrors = false;
        }


        public string ApiAddress
        {
            get { return _apiAddress; }

            set { _apiAddress = value; }
        }

        public string Passcode
        {
            get => _passcode;

            set
            {
                _passcode = value;
                RaisePropertyChangedEvent("Passcode");
            }
        }

        public string Prompt
        {
            get => _prompt;

            set
            {
                _prompt = value;
                RaisePropertyChangedEvent("Prompt");
            }
        }

        public bool HasErrors
        {
            get => _hasErrors;

            set
            {
                _hasErrors = value;

                if (value == false)
                {
                    ErrorMessage = null;
                }

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

        public void ShowWithPrompt(string prompt)
        {
            Prompt = prompt;

            DialogService.ShowDialog(typeof(SsoDialogViewModel).Name);
        }

        public bool CanLoginWithPasscode(object arg = null)
        {
            return true;
        }

        public async Task LoginWithPasscodeAsync(object arg = null)
        {
            HasErrors = false;

            if (string.IsNullOrWhiteSpace(Passcode))
            {
                ErrorMessage = "Passcode cannot be empty.";
            }
            else
            {
                var loginResult = await CloudFoundryService.LoginWithSsoPasscode(ApiAddress, Passcode);

                if (loginResult.Succeeded)
                {
                    DialogService.CloseDialog(arg, true);
                }
                else
                {
                    ErrorMessage = loginResult.Explanation;
                }
            }
        }
    }
}
