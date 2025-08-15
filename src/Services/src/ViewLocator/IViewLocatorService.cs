namespace Tanzu.Toolkit.Services.ViewLocator
{
    public interface IViewLocatorService
    {
        object GetViewByViewModelName(string viewModelName, object parameter = null);
    }
}