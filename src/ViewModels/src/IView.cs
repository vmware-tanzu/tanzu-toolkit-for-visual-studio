namespace Tanzu.Toolkit.ViewModels
{
    public interface IView
    {
        IViewModel ViewModel { get; }
        void Show();
    }
}
