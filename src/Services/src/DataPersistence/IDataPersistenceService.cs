﻿namespace Tanzu.Toolkit.Services.DataPersistence
{
    public interface IDataPersistenceService
    {
        bool ClearData(string key);
        string ReadStringData(string key);
        bool SavedCfCredsExist();
        bool WriteStringData(string key, string value);
    }
}