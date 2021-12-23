using System;
using System.Security;
using System.Threading.Tasks;

namespace Tanzu.Toolkit.ViewModels
{
    public interface ILoginViewModel : IViewModel
    {
        string ConnectionName { get; set; }
        string Target { get; set; }
        string Username { get; set; }
        bool SkipSsl { get; set; }
        bool HasErrors { get; set; }
        string ErrorMessage { get; set; }
        Func<SecureString> GetPassword { get; set; }
        Func<bool> PasswordEmpty { get; set; }
        Action ClearPassword { get; set; }

        Task LogIn(object arg);
        bool CanLogIn(object arg);
        bool ValidateApiAddress(string apiAddress);
        bool CanOpenSsoDialog(object arg = null);
        Task OpenSsoDialog(object apiAddress = null);
        void CloseDialog();
        void SetConnection();
        void NavigateToTargetPage(object arg = null);
        Task NavigateToAuthPage(object arg = null);
        bool CanProceedToAuthentication(object arg = null);
    }
}
