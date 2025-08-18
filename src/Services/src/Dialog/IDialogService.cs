using System.Threading.Tasks;

namespace Tanzu.Toolkit.Services.Dialog
{
    public interface IDialogService
    {
        void CloseDialog(object dialogWindow, bool result);

        Task CloseDialogByNameAsync(string dialogName, object parameter = null);

        Task<IDialogResult> ShowModalAsync(string dialogName, object parameter = null);
    }
}