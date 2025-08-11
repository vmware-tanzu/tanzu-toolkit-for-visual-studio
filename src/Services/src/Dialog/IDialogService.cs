namespace Tanzu.Toolkit.Services.Dialog
{
    public interface IDialogService
    {
        void CloseDialog(object dialogWindow, bool result);
        void CloseDialogByName(string dialogName, object parameter = null);
        IDialogResult ShowModal(string dialogName, object parameter = null);
    }
}