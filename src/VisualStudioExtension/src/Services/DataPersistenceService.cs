using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;
using Serilog;
using System;
using Tanzu.Toolkit.Services.CfCli;
using Tanzu.Toolkit.Services.DataPersistence;
using Tanzu.Toolkit.Services.Logging;

namespace Tanzu.Toolkit.VisualStudio.Services
{
    public class DataPersistenceService : IDataPersistenceService
    {
        private const string tasCollectionPath = @"Tanzu Application Service";

        private SettingsStore _readOnlySettingsStore;
        private WritableSettingsStore _writableSettingsStore;
        private ILogger _logger;
        private ICfCliService _cfCliService;

        public DataPersistenceService(IServiceProvider VsPackage, IServiceProvider services)
        {
            var settingsManager = new ShellSettingsManager(VsPackage);
            _readOnlySettingsStore = settingsManager.GetReadOnlySettingsStore(SettingsScope.UserSettings);
            _writableSettingsStore = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);

            _cfCliService = services.GetRequiredService<ICfCliService>();
            var logSvc = services.GetRequiredService<ILoggingService>();
            _logger = logSvc.Logger;
        }

        public string ReadStringData(string key)
        {
            try
            {
                if (!_readOnlySettingsStore.CollectionExists(tasCollectionPath))
                {
                    _logger.Error($"Attempted to read user settings store value under \"{tasCollectionPath}\" but no such collection path exists");

                    return null;
                }

                if (_readOnlySettingsStore.PropertyExists(tasCollectionPath, key))
                {
                    return _readOnlySettingsStore.GetString(tasCollectionPath, key);
                }
                else
                {
                    _logger.Error($"Attempted to read user settings store value for \"{key}\" but no such property exists");

                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.Error("DataPersistenceService caught exception while trying to read string data: {DataPersistenceReadException}", ex);

                return null;
            }
        }

        public bool WriteStringData(string key, string value)
        {
            try
            {
                if (!_writableSettingsStore.CollectionExists(tasCollectionPath))
                {
                    _writableSettingsStore.CreateCollection(tasCollectionPath);
                }

                _writableSettingsStore.SetString(tasCollectionPath, key, value);

                if (_writableSettingsStore.PropertyExists(tasCollectionPath, key))
                {
                    return true;
                }
                else
                {
                    throw new Exception($"Tried to write value \"{value}\" to user settings store property \"{key}\" but no such property existed after writing.");
                }
            }
            catch (Exception ex)
            {
                _logger.Error("DataPersistenceService caught exception while trying to write string data: {DataPersistenceWriteException}", ex);

                return false;
            }
        }

        public bool ClearData(string propertyName)
        {
            return _writableSettingsStore.DeleteProperty(tasCollectionPath, propertyName);
        }

        public bool SavedCfCredsExist()
        {
            try
            {
                var credRecordExists = _cfCliService.GetOAuthToken() != null;
                return credRecordExists;
            }
            catch (Exception ex)
            {
                _logger?.Error("DataPersistenceService caught exception while trying to determine if prior credentials were saved: {DataPersistenceCredsQueryException}", ex);
                return false;
            }
        }
    }
}
