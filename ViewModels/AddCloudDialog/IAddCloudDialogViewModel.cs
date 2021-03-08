using System;
using System.Security;
using System.Threading.Tasks;

namespace Tanzu.Toolkit.VisualStudio.ViewModels
{
    public interface IAddCloudDialogViewModel : IViewModel
    {
        string InstanceName { get; set; }
        string Target { get; set; }
        string Username { get; set; }
        string HttpProxy { get; set; }
        bool SkipSsl { get; set; }
        bool HasErrors { get; set; }
        string ErrorMessage { get; set; }
        Func<SecureString> GetPassword { get; set; }
        Task AddCloudFoundryInstance(object arg);
        bool CanAddCloudFoundryInstance(object arg);
    }
}
