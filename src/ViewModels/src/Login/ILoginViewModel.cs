﻿using System;
using System.Security;
using System.Threading.Tasks;

namespace Tanzu.Toolkit.ViewModels
{
    public interface ILoginViewModel : IViewModel
    {
        string ConnectionName { get; set; }
        string Target { get; set; }
        string Username { get; set; }
        bool SkipSsl { get; set; }
        bool HasErrors { get; set; }
        string ErrorMessage { get; set; }
        Func<SecureString> GetPassword { get; set; }
        Func<bool> PasswordEmpty { get; set; }

        Task LogIn(object arg);
        bool CanLogIn(object arg);
        bool VerifyApiAddress(string apiAddress);
    }
}
