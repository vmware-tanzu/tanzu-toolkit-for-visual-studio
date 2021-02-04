namespace Tanzu.Toolkit.VisualStudio.Services.ViewLocator
{
    public interface IViewLocatorService
    {
        string CurrentView { get; }
        object NavigateTo(string viewModelName, object parameter = null);
    }
}
