namespace TanzuForVS.Services.Locator
{
    public interface IViewLocatorService
    {
        string CurrentView { get; }
        object NavigateTo(string viewModelName, object parameter = null);
    }
}
