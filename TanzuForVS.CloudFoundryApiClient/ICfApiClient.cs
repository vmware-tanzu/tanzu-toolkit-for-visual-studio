using System.Threading.Tasks;

namespace TanzuForVS.CloudFoundryApiClient
{
    public interface ICfApiClient
    {
        Task LoginAsync(string cfTarget, string cfUsername, string cfPassword);
    }
}
