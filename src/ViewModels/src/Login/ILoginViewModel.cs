using System;
using System.Security;
using System.Threading.Tasks;
using Tanzu.Toolkit.Models;

namespace Tanzu.Toolkit.ViewModels
{
    public interface ILoginViewModel : IViewModel
    {
        string ConnectionName { get; }
        string TargetApiAddress { get; set; }
        string Username { get; set; }
        bool SkipSsl { get; set; }
        bool HasErrors { get; set; }
        string ErrorMessage { get; set; }
        Func<SecureString> GetPassword { get; set; }
        Func<bool> PasswordEmpty { get; set; }
        Action ClearPassword { get; set; }
        CloudFoundryInstance TargetCf { get; set; }

        Task LogIn(object arg);
        bool CanLogIn(object arg);
        bool ValidateApiAddressFormat(string apiAddress);
        void ShowSsoLogin(object apiAddress = null);
        void CloseDialog();
        void NavigateToTargetPage(object arg = null);
        Task ConnectToCf(object arg = null);
        bool CanProceedToAuthentication(object arg = null);
        void ResetTargetDependentFields();
        Task LoginWithSsoPasscodeAsync(object arg = null);
    }
}