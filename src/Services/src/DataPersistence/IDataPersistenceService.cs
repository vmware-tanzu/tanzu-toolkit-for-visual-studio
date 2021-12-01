namespace Tanzu.Toolkit.Services.DataPersistence
{
    public interface IDataPersistenceService
    {
        string ReadStringData(string key);
        bool WriteStringData(string key, string value);
    }
}