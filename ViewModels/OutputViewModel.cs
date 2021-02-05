using System;

namespace Tanzu.Toolkit.VisualStudio.ViewModels
{
    public class OutputViewModel : AbstractViewModel, IOutputViewModel
    {
        private string outputContent;

        public OutputViewModel(IServiceProvider services) : base(services)
        {
        }

        public string OutputContent
        {
            get => outputContent;

            set
            {
                outputContent = value;
                RaisePropertyChangedEvent("OutputContent");
            }
        }

        public void AppendLine(string newContent)
        {
            OutputContent += $"{newContent}\n";
        }
    }
}
