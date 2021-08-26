using System;

namespace Tanzu.Toolkit.ViewModels
{
    public class LoginPromptViewModel : PlaceholderViewModel
    {
        public LoginPromptViewModel(IServiceProvider services) : base(null, services)
        {
            DisplayText = "Disconnected; click to log in.";
        }
    }
}
