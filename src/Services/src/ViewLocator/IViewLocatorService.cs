using System.Threading.Tasks;

namespace Tanzu.Toolkit.Services.ViewLocator
{
    public interface IViewLocatorService
    {
        Task<object> GetViewByViewModelNameAsync(string viewModelName, object parameter = null);
    }
}