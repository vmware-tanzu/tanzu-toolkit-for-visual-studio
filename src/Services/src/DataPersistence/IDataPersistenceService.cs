namespace Tanzu.Toolkit.Services.DataPersistence
{
    public interface IDataPersistenceService
    {
        bool ClearDataFromProperty(string key);
        string ReadStringData(string key);
        bool WriteStringData(string key, string value);
    }
}