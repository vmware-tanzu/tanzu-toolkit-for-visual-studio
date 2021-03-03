using Tanzu.Toolkit.VisualStudio.Services.Dialog;

namespace Tanzu.Toolkit.VisualStudio.Services.Dialog
{
    public interface IDialogService
    {
        IDialogResult ShowDialog(string dialogViewModel, object parameter = null);

        void CloseDialog(object dialogWindow, bool result);
        void DisplayErrorDialog(string errorTitle, string errorMsg);
    }
}
