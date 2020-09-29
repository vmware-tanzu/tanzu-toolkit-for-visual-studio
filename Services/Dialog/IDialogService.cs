namespace TanzuForVS.Services.Dialog
{
    public interface IDialogService
    {
        IDialogResult ShowDialog(string dialogViewModel, object parameter = null);

        void CloseDialog(object dialogWindow, bool result);
    }
}
