namespace TanzuForVS.ViewModels
{
    public interface IViewModel
    {
        object ActiveView { get; set; }

        bool IsLoggedIn { get; set; }

    }
}
