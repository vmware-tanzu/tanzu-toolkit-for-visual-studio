using System;

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

        public void AppendLine(string newContent)
        {
            OutputContent += $"{newContent}\n";
        }
    }
}
