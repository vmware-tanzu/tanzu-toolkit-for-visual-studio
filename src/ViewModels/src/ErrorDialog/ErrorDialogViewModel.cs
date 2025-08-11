namespace Tanzu.Toolkit.ViewModels
{
    public class ErrorDialogViewModel : IErrorDialogViewModel
    {
        public ErrorDialogViewModel()
        {
        }

        public string Title { get; set; }
        public string Message { get; set; }

        public bool CanClose(object arg)
        {
            return true;
        }
    }
}