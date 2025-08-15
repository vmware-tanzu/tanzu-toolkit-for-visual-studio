namespace Tanzu.Toolkit.Services.ErrorDialog
{
    public interface IErrorDialog
    {
        void DisplayErrorDialog(string errorTitle, string errorMsg);

        void DisplayWarningDialog(string warningTitle, string warningMsg);
    }
}