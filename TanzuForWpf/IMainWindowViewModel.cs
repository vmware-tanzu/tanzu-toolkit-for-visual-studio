using Tanzu.Toolkit.VisualStudio.ViewModels;

namespace Tanzu.Toolkit.WpfApp
{
    public interface IMainWindowViewModel : IViewModel
    {
        bool CanInvokeCfCli(object arg);
        bool CanInvokeCommandPrompt(object arg);
        bool CanOpenCloudExplorer(object arg);
        void InvokeCfCli(object arg);
        void InvokeCommandPrompt(object arg);
        void OpenCloudExplorer(object arg);
    }
}
