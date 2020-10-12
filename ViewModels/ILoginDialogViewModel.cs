using System;
using System.Security;
using System.Threading.Tasks;

namespace TanzuForVS.ViewModels
{
    public interface ILoginDialogViewModel : IViewModel
    {
        string InstanceName { get; set; }
        string Target { get; set; }
        string Username { get; set; }
        string HttpProxy { get; set; }
        bool SkipSsl { get; set; }
        bool HasErrors { get; set; }
        string ErrorMessage { get; set; }
        Func<SecureString> GetPassword { get; set; }
        Task ConnectToCloudFoundry(object arg);
        bool CanConnectToCloudFoundry(object arg);
        bool CanAddCloudFoundryInstance(object arg);
        Task AddCloudFoundryInstance(object arg);
    }
}
