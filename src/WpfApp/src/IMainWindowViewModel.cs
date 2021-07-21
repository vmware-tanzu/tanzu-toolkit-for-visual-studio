using Tanzu.Toolkit.ViewModels;

namespace Tanzu.Toolkit.WpfApp
{
    public interface IMainWindowViewModel : IViewModel
    {
        bool CanInvokeCfCli(object arg);
        bool CanInvokeCommandPrompt(object arg);
        bool CanOpenTasExplorer(object arg);
        void InvokeCfCli(object arg);
        void InvokeCommandPrompt(object arg);
        void OpenTasExplorer(object arg);
    }
}
