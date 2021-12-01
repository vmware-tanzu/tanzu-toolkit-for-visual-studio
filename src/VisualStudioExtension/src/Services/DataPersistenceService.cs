using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Settings;
using Serilog;
using System;
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

        public DataPersistenceService(IServiceProvider services)
        {
            var settingsManager = new ShellSettingsManager(services);
            _readOnlySettingsStore = settingsManager.GetReadOnlySettingsStore(SettingsScope.UserSettings);
            _writableSettingsStore = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);

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
                _logger.Error(ex.Message);

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
                _logger.Error(ex.Message);

                return false;
            }
        }
    }
}
