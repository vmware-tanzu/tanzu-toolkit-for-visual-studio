using System.Threading.Tasks;

namespace Tanzu.Toolkit.ViewModels.SsoDialog
{
    public interface ISsoDialogViewModel
    {
        string Prompt { get; set; }
        string ApiAddress { get; set; }

        bool CanLoginWithPasscode(object arg = null);
        Task LoginWithPasscodeAsync(object arg = null);
        void ShowWithPrompt(string prompt);
    }
}