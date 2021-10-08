﻿using System;
using System.Security;
using System.Threading.Tasks;

namespace Tanzu.Toolkit.ViewModels
{
    public interface ILoginViewModel : IViewModel
    {
        string InstanceName { get; set; }
        string Target { get; set; }
        string Username { get; set; }
        string HttpProxy { get; set; }
        bool SkipSsl { get; set; }
        bool HasErrors { get; set; }
        string ErrorMessage { get; set; }
        Func<SecureString> GetPassword { get; set; }
        Task LogIn(object arg);
        bool CanLogIn(object arg);
    }
}
