using System;
using System.Collections.Generic;
using System.Text;

namespace Tanzu.Toolkit.Services.ErrorDialog
{
    public interface IErrorDialog
    {
        void DisplayErrorDialog(string errorTitle, string errorMsg);
    }
}
