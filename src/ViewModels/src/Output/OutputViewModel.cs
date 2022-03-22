using System;
using System.Diagnostics;

namespace Tanzu.Toolkit.ViewModels
{
    public class OutputViewModel : AbstractViewModel, IOutputViewModel
    {
        private string _outputContent;

        public OutputViewModel(IServiceProvider services) : base(services)
        {
        }

        public string OutputContent
        {
            get => _outputContent;

            set
            {
                _outputContent = value;
                RaisePropertyChangedEvent("OutputContent");
            }
        }
        
        public Process ActiveProcess { get; set; }

        public void AppendLine(string newContent)
        {
            OutputContent += $"{newContent}\n";
        }

        public void CancelActiveProcess()
        {
            ActiveProcess?.Kill();
            ActiveProcess?.Dispose();
        }

        public void ClearContent(object arg = null)
        {
            OutputContent = string.Empty;
        }
    }
}
