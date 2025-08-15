namespace Tanzu.Toolkit.Services.DataPersistence
{
    public interface IDataPersistenceService
    {
        bool ClearData(string key);

        string ReadStringData(string key);

        bool SavedCloudFoundryCredentialsExist();

        bool WriteStringData(string key, string value);
    }
}