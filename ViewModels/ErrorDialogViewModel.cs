namespace Tanzu.Toolkit.VisualStudio.ViewModels
{
    public class ErrorDialogViewModel : IErrorDialogViewModel
    {
        public ErrorDialogViewModel()
        {
        }

        public string Title { get; set; }
        public string Message { get; set; }

    }
}
