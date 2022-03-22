using System.Diagnostics;
using Tanzu.Toolkit.Models;

namespace Tanzu.Toolkit.ViewModels
{
    public interface IOutputViewModel
    {
        Process ActiveProcess { get; set; }

        void AppendLine(string newContent);
        void CancelActiveProcess();
        void ClearContent(object arg = null);
    }
}