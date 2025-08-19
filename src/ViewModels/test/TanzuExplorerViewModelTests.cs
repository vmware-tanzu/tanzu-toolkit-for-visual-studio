using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tanzu.Toolkit.Models;
using Tanzu.Toolkit.Services;
using Tanzu.Toolkit.ViewModels.AppDeletionConfirmation;

namespace Tanzu.Toolkit.ViewModels.Tests
{
    [TestClass]
    public class TanzuExplorerViewModelTests : ViewModelTestSupport
    {
        private TanzuExplorerViewModel _sut;
        private List<string> _receivedEvents;
        private CfInstanceViewModel _fakeTanzuConnection;

        [TestInitialize]
        public void TestInit()
        {
            RenewMockServices();

            // set up mockUiDispatcherService to run whatever method is passed
            // to RunOnUiThread; do not delegate to the UI Dispatcher
            MockUiDispatcherService.Setup(mock => mock.RunOnUIThreadAsync(It.IsAny<Action>()))
                .Callback<Action>(action => { action(); });

            _receivedEvents = [];
            _fakeTanzuConnection = new FakeCfInstanceViewModel(_fakeCfInstance, Services);

            _sut = new TanzuExplorerViewModel(Services) { CloudFoundryConnection = _fakeTanzuConnection };
            _sut.PropertyChanged += (_, e) => { _receivedEvents.Add(e.PropertyName); };
        }

        [TestCleanup]
        public void TestCleanup()
        {
            MockDataPersistenceService.VerifyAll();
            MockCloudFoundryService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("Ctor")]
        public void Ctor_SetsConnectionToNull_WhenSavedConnectionNameNull()
        {
            MockDataPersistenceService.Setup(m => m.ReadStringData(TanzuExplorerViewModel._connectionNameKey)).Returns((string)null);
            MockDataPersistenceService.Setup(m => m.ReadStringData(TanzuExplorerViewModel._connectionAddressKey)).Returns("junk non-null value");
            MockDataPersistenceService.Setup(m => m.SavedCloudFoundryCredentialsExist()).Returns(true);

            _sut = new TanzuExplorerViewModel(Services);

            Assert.IsNull(_sut.CloudFoundryConnection);
        }

        [TestMethod]
        [TestCategory("Ctor")]
        public void Ctor_SetsConnectionToNull_WhenSavedConnectionAddressNull()
        {
            MockDataPersistenceService.Setup(m => m.ReadStringData(TanzuExplorerViewModel._connectionNameKey)).Returns("junk non-null value");
            MockDataPersistenceService.Setup(m => m.ReadStringData(TanzuExplorerViewModel._connectionAddressKey)).Returns((string)null);
            MockDataPersistenceService.Setup(m => m.SavedCloudFoundryCredentialsExist()).Returns(true);

            _sut = new TanzuExplorerViewModel(Services);

            Assert.IsNull(_sut.CloudFoundryConnection);
        }

        [TestMethod]
        [TestCategory("Ctor")]
        public void Ctor_SetsConnectionToNull_WhenAccessTokenIrretrievable()
        {
            MockDataPersistenceService.Setup(m => m.SavedCloudFoundryCredentialsExist()).Returns(false);
            MockDataPersistenceService.Setup(m => m.ReadStringData(TanzuExplorerViewModel._connectionNameKey)).Returns("junk non-null value");
            MockDataPersistenceService.Setup(m => m.ReadStringData(TanzuExplorerViewModel._connectionAddressKey)).Returns("junk non-null value");
            MockDataPersistenceService.Setup(m => m.ReadStringData(TanzuExplorerViewModel._connectionSslPolicyKey)).Returns("junk non-null value");

            _sut = new TanzuExplorerViewModel(Services);

            Assert.IsNull(_sut.CloudFoundryConnection);
        }

        [TestMethod]
        [TestCategory("Ctor")]
        [DataRow(TanzuExplorerViewModel._skipCertValidationValue, true)]
        [DataRow(TanzuExplorerViewModel._validateSslCertsValue, false)]
        [DataRow("unexpected value", false)]
        [DataRow(null, false)]
        public void Ctor_RestoresConnection_WhenSavedConnectionNameAddressAndTokenExist(string savedSslPolicyValue, bool expectedSkipSslValue)
        {
            var savedConnectionName = "junk";
            var savedConnectionAddress = "junk";

            MockDataPersistenceService.Setup(m => m.SavedCloudFoundryCredentialsExist()).Returns(true);
            MockDataPersistenceService.Setup(m => m.ReadStringData(TanzuExplorerViewModel._connectionNameKey)).Returns(savedConnectionName);
            MockDataPersistenceService.Setup(m => m.ReadStringData(TanzuExplorerViewModel._connectionAddressKey)).Returns(savedConnectionAddress);
            MockDataPersistenceService.Setup(m => m.ReadStringData(TanzuExplorerViewModel._connectionSslPolicyKey)).Returns(savedSslPolicyValue);

            _sut = new TanzuExplorerViewModel(Services);

            Assert.IsNotNull(_sut.CloudFoundryConnection);
            Assert.AreEqual(savedConnectionName, _sut.CloudFoundryConnection.CloudFoundryInstance.InstanceName);
            Assert.AreEqual(savedConnectionAddress, _sut.CloudFoundryConnection.CloudFoundryInstance.ApiAddress);
            Assert.AreEqual(expectedSkipSslValue, _sut.CloudFoundryConnection.CloudFoundryInstance.SkipSslCertValidation);
        }

        [TestMethod]
        [TestCategory("CloudFoundryConnection")]
        [TestCategory("TreeRoot")]
        public void SettingConnection_PopulatesTreeRoot()
        {
            _sut = new TanzuExplorerViewModel(Services);

            Assert.IsNull(_sut.CloudFoundryConnection);
            Assert.HasCount(1, _sut.TreeRoot);
            Assert.IsTrue(_sut.TreeRoot[0] is LoginPromptViewModel);

            _sut.CloudFoundryConnection = new CfInstanceViewModel(_fakeCfInstance, _sut, Services);

            Assert.IsNotNull(_sut.CloudFoundryConnection);
            Assert.HasCount(1, _sut.TreeRoot);
            Assert.AreEqual(_sut.TreeRoot[0], _sut.CloudFoundryConnection);
        }

        [TestMethod]
        [TestCategory("CloudFoundryConnection")]
        [TestCategory("TreeRoot")]
        public void SettingConnectionToNull_ClearsTreeRoot()
        {
            _sut.CloudFoundryConnection = _fakeTanzuConnection;

            Assert.IsNotNull(_sut.CloudFoundryConnection);
            Assert.HasCount(1, _sut.TreeRoot);
            Assert.AreEqual(_sut.TreeRoot[0], _sut.CloudFoundryConnection);

            _sut.CloudFoundryConnection = null;

            Assert.IsNull(_sut.CloudFoundryConnection);
            Assert.HasCount(1, _sut.TreeRoot);
            Assert.IsTrue(_sut.TreeRoot[0] is LoginPromptViewModel);
        }

        [TestMethod]
        public void CanOpenLoginView_ReturnsTrue()
        {
            Assert.IsTrue(_sut.CanOpenLoginView(null));
        }

        [TestMethod]
        public void CanStopCfApp_ReturnsTrue()
        {
            Assert.IsTrue(_sut.CanStopCloudFoundryApp(null));
        }

        [TestMethod]
        public void CanStartCfApp_ReturnsTrue()
        {
            Assert.IsTrue(_sut.CanStartCloudFoundryApp(null));
        }

        [TestMethod]
        public void CanOpenDeletionView_ReturnsTrue()
        {
            Assert.IsTrue(_sut.CanOpenDeletionView(null));
        }

        [TestMethod]
        public void CanRefreshOrg_ReturnsTrue()
        {
            Assert.IsTrue(_sut.CanRefreshOrg(null));
        }

        [TestMethod]
        public void CanRefreshSpace_ReturnsTrue()
        {
            Assert.IsTrue(_sut.CanRefreshSpace(null));
        }

        [TestMethod]
        public void CanInitiateFullRefresh_ReturnsTrue_WhenNotRefreshing()
        {
            _sut.IsRefreshingAll = false;
            Assert.IsTrue(_sut.CanInitiateFullRefresh(null));
        }

        [TestMethod]
        public void CanInitiateFullRefresh_ReturnsFalse_WhenRefreshing()
        {
            _sut.IsRefreshingAll = true;
            Assert.IsFalse(_sut.CanInitiateFullRefresh(null));
        }

        [TestMethod]
        [TestCategory("OpenLoginViewAsync")]
        public async Task OpenLoginView_DisplaysLoginDialog_WhenConnectionIsNullAsync()
        {
            _sut = new TanzuExplorerViewModel(Services);

            Assert.IsNull(_sut.CloudFoundryConnection);

            await _sut.OpenLoginViewAsync(null);

            MockDialogService.Verify(ds => ds.ShowModalAsync(nameof(LoginViewModel), null), Times.Once);
        }

        [TestMethod]
        [TestCategory("OpenLoginViewAsync")]
        public async Task OpenLoginView_DisplaysErrorDialog_WhenConnectionIsNotNullAsync()
        {
            _sut.CloudFoundryConnection = _fakeTanzuConnection;

            Assert.IsNotNull(_sut.CloudFoundryConnection);

            await _sut.OpenLoginViewAsync(null);

            MockErrorDialogService.Verify(m => m.DisplayErrorDialog(TanzuExplorerViewModel._singleLoginErrorTitle,
                    It.Is<string>(s => s.Contains(TanzuExplorerViewModel._singleLoginErrorMessage1) && s.Contains(TanzuExplorerViewModel._singleLoginErrorMessage2))),
                Times.Once);
        }

        [TestMethod]
        [TestCategory("OpenLoginViewAsync")]
        public async Task OpenLoginView_DoesNotChangeAuthenticationRequired_WhenConnectionDoesNotGetSetAsync()
        {
            _sut = new TanzuExplorerViewModel(Services) { AuthenticationRequired = true };

            MockDialogService.Setup(mock => mock.ShowModalAsync(nameof(LoginViewModel), null))
                .Callback(() =>
                {
                    // Simulate unsuccessful login by NOT setting CloudFoundryConnection as LoginView would've done on a successful login
                });

            Assert.IsTrue(_sut.AuthenticationRequired);
            Assert.IsNull(_sut.CloudFoundryConnection);

            Assert.IsTrue(_sut.CanOpenLoginView(null));
            await _sut.OpenLoginViewAsync(null);

            Assert.IsNull(_sut.CloudFoundryConnection);
            Assert.IsTrue(_sut.AuthenticationRequired); // this prop should not have changed
        }

        [TestMethod]
        [TestCategory("OpenDeletionViewAsync")]
        public async Task OpenDeletionView_DisplaysDeletionDialog_WhenCfAppIsDeletedAsync()
        {
            var fakeApp = new CloudFoundryApp("junk", "junk", parentSpace: null, null);

            await _sut.OpenDeletionViewAsync(fakeApp);

            MockAppDeletionConfirmationViewModel.Verify(m => m.ShowConfirmationAsync(fakeApp), Times.Once);
        }

        [TestMethod]
        [TestCategory("OpenDeletionViewAsync")]
        public async Task OpenDeletionView_DoesNotDisplaysDeletionDialog_WhenArgumentIsNotACfAppAsync()
        {
            await _sut.OpenDeletionViewAsync(null);

            MockDialogService.Verify(ds => ds.ShowModalAsync(nameof(AppDeletionConfirmationViewModel), null), Times.Never);
        }

        [TestMethod]
        [TestCategory("StopCloudFoundryAppAsync")]
        public async Task StopCfApp_CallsStopCfAppAsync()
        {
            var fakeApp = new CloudFoundryApp("junk", "junk", parentSpace: null, null);

            MockCloudFoundryService.Setup(mock => mock.StopAppAsync(fakeApp, false, It.IsAny<int>())).ReturnsAsync(_fakeSuccessDetailedResult);

            Exception shouldStayNull = null;
            try
            {
                await _sut.StopCloudFoundryAppAsync(fakeApp);
            }
            catch (Exception e)
            {
                shouldStayNull = e;
            }

            Assert.IsNull(shouldStayNull);
            MockCloudFoundryService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("StopCloudFoundryAppAsync")]
        public async Task StopCfApp_DisplaysErrorDialog_WhenStopAppAsyncFails()
        {
            var fakeApp = new CloudFoundryApp("junk", "junk", parentSpace: null, null);

            MockCloudFoundryService.Setup(mock => mock.StopAppAsync(fakeApp, false, It.IsAny<int>()))
                .ReturnsAsync(_fakeFailureDetailedResult);

            await _sut.StopCloudFoundryAppAsync(fakeApp);

            var expectedErrorTitle = $"{TanzuExplorerViewModel._stopAppErrorMsg} {fakeApp.AppName}.";
            var expectedErrorMsg = _fakeFailureDetailedResult.Explanation;

            MockErrorDialogService.Verify(mock => mock.DisplayErrorDialog(expectedErrorTitle, expectedErrorMsg),
                Times.Once);
        }

        [TestMethod]
        [TestCategory("StopCloudFoundryAppAsync")]
        public async Task StopCfApp_LogsError_WhenStopAppAsyncFails()
        {
            var fakeApp = new CloudFoundryApp("junk", "junk", parentSpace: null, null);

            MockCloudFoundryService.Setup(mock => mock.StopAppAsync(fakeApp, false, It.IsAny<int>()))
                .ReturnsAsync(_fakeFailureDetailedResult);

            await _sut.StopCloudFoundryAppAsync(fakeApp);

            var logPropVal1 = "{AppName}";
            var logPropVal2 = "{StopResult}";
            var expectedLogMsg = $"{TanzuExplorerViewModel._stopAppErrorMsg} {logPropVal1}. {logPropVal2}";

            MockLogger.Verify(m => m.Error(expectedLogMsg, fakeApp.AppName, _fakeFailureDetailedResult.ToString()),
                Times.Once);
        }

        [TestMethod]
        [TestCategory("StartCloudFoundryAppAsync")]
        public async Task StartCfApp_CallsStartAppAsync()
        {
            var fakeApp = new CloudFoundryApp("junk", "junk", parentSpace: null, null);

            MockCloudFoundryService.Setup(mock => mock.StartAppAsync(fakeApp, false, It.IsAny<int>())).ReturnsAsync(_fakeSuccessDetailedResult);

            Exception shouldStayNull = null;
            try
            {
                await _sut.StartCloudFoundryAppAsync(fakeApp);
            }
            catch (Exception e)
            {
                shouldStayNull = e;
            }

            Assert.IsNull(shouldStayNull);
            MockCloudFoundryService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("StartCloudFoundryAppAsync")]
        public async Task StartCfApp_DisplaysErrorDialog_WhenStartAppAsyncFails()
        {
            var fakeApp = new CloudFoundryApp("junk", "junk", parentSpace: null, null);

            MockCloudFoundryService.Setup(mock => mock.StartAppAsync(fakeApp, false, It.IsAny<int>()))
                .ReturnsAsync(_fakeFailureDetailedResult);

            await _sut.StartCloudFoundryAppAsync(fakeApp);

            var expectedErrorTitle = $"{TanzuExplorerViewModel._startAppErrorMsg} {fakeApp.AppName}.";
            var expectedErrorMsg = _fakeFailureDetailedResult.Explanation;

            MockErrorDialogService.Verify(mock => mock.DisplayErrorDialog(expectedErrorTitle, expectedErrorMsg),
                Times.Once);
        }

        [TestMethod]
        [TestCategory("StartCloudFoundryAppAsync")]
        public async Task StartCfApp_LogsError_WhenStartAppAsyncFails()
        {
            var fakeApp = new CloudFoundryApp("junk", "junk", parentSpace: null, null);

            MockCloudFoundryService.Setup(mock => mock.StartAppAsync(fakeApp, false, It.IsAny<int>()))
                .ReturnsAsync(_fakeFailureDetailedResult);

            await _sut.StartCloudFoundryAppAsync(fakeApp);

            var logPropVal1 = "{AppName}";
            var logPropVal2 = "{StartResult}";
            var expectedLogMsg = $"{TanzuExplorerViewModel._startAppErrorMsg} {logPropVal1}. {logPropVal2}";

            MockLogger.Verify(m => m.Error(expectedLogMsg, fakeApp.AppName, _fakeFailureDetailedResult.ToString()),
                Times.Once);
        }

        [TestMethod]
        [TestCategory("RefreshOrgAsync")]
        public async Task RefreshOrg_CallsUpdateAllChildrenOnOrg_WhenArgIsOrgViewModel()
        {
            var ovm = new FakeOrgViewModel(_fakeCfOrg, Services);

            Assert.IsInstanceOfType<OrgViewModel>(ovm);
            Assert.AreEqual(0, ovm.NumUpdates);

            await _sut.RefreshOrgAsync(ovm);

            Assert.AreEqual(1, ovm.NumUpdates);
        }

        [TestMethod]
        [TestCategory("RefreshSpaceAsync")]
        public async Task RefreshSpace_CallsUpdateAllChildrenOnSpace_WhenArgIsSpaceViewModel()
        {
            var svm = new FakeSpaceViewModel(_fakeCfSpace, Services);

            Assert.IsInstanceOfType<SpaceViewModel>(svm);
            Assert.AreEqual(0, svm.NumUpdates);

            await _sut.RefreshSpaceAsync(svm);

            Assert.AreEqual(1, svm.NumUpdates);
        }

        [TestMethod]
        [TestCategory("BackgroundRefreshAllItems")]
        public void RefreshAllItems_SetsIsRefreshingAllFalse_WhenConnectionNull()
        {
            _sut = new TanzuExplorerViewModel(Services) { IsRefreshingAll = true };

            Assert.IsNull(_sut.CloudFoundryConnection);
            Assert.IsTrue(_sut.IsRefreshingAll);

            _sut.BackgroundRefreshAllItems();

            Assert.IsFalse(_sut.IsRefreshingAll);
        }

        [TestMethod]
        [TestCategory("BackgroundRefreshAllItems")]
        public void RefreshAllItems_SetsThreadServiceIsPollingFalse_WhenConnectionNull()
        {
            _sut = new TanzuExplorerViewModel(Services);

            MockThreadingService.SetupSet(mock => mock.IsPolling = false).Verifiable();

            Assert.IsNull(_sut.CloudFoundryConnection);

            _sut.BackgroundRefreshAllItems();

            MockThreadingService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("BackgroundRefreshAllItems")]
        public void RefreshAllItems_SetsIsRefreshingAllTrue_WhenNotPreviouslyRefreshingAll_AndConnectionNotNull()
        {
            _sut.CloudFoundryConnection = _fakeTanzuConnection;

            Assert.IsNotNull(_sut.CloudFoundryConnection);
            Assert.IsFalse(_sut.IsRefreshingAll);

            _sut.BackgroundRefreshAllItems();

            Assert.IsTrue(_sut.IsRefreshingAll);
        }

        [TestMethod]
        [TestCategory("BackgroundRefreshAllItems")]
        public void RefreshAllItems_StartsBackgroundTaskToRefreshAllItems_WhenNotPreviouslyRefreshingAll_AndConnectionNotNull()
        {
            _sut.CloudFoundryConnection = _fakeTanzuConnection;

            MockThreadingService.Setup(m => m.StartBackgroundTaskAsync(It.IsAny<Func<Task>>())).Verifiable();

            Assert.IsNotNull(_sut.CloudFoundryConnection);
            Assert.IsFalse(_sut.IsRefreshingAll);

            _sut.BackgroundRefreshAllItems();
            MockThreadingService.Verify(m => m.StartBackgroundTaskAsync(_sut.UpdateAllTreeItems), Times.Once);
        }

        [TestMethod]
        [TestCategory("BackgroundRefreshAllItems")]
        public void RefreshAllItems_DoesNotStartRefreshTask_WhenRefreshIsInProgress()
        {
            _sut.IsRefreshingAll = true;

            Assert.IsTrue(_sut.IsRefreshingAll);

            _sut.BackgroundRefreshAllItems();

            MockThreadingService.Verify(m => m.StartBackgroundTaskAsync(It.IsAny<Func<Task>>()), Times.Never);
        }

        [TestMethod]
        [TestCategory("DisplayRecentAppLogsAsync")]
        public async Task DisplayRecentAppLogs_LogsError_WhenArgumentTypeIsNotApp()
        {
            await _sut.DisplayRecentAppLogsAsync(new object());
            MockLogger.Verify(m => m.Error(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("DisplayRecentAppLogsAsync")]
        public async Task DisplayRecentAppLogs_DisplaysErrorDialog_WhenLogsCmdFails()
        {
            var fakeApp = new CloudFoundryApp("junk", "junk", null, "junk");
            var fakeLogsResult = new DetailedResult<string>(content: null, succeeded: false, explanation: ":(", cmdDetails: _fakeFailureCmdResult);

            MockViewLocatorService.Setup(m => m.GetViewByViewModelNameAsync(nameof(OutputViewModel), null))
                .Callback(() => Assert.Fail("Output view does not need to be retrieved."));

            MockCloudFoundryService.Setup(m => m.GetRecentLogsAsync(fakeApp))
                .ReturnsAsync(fakeLogsResult);

            await _sut.DisplayRecentAppLogsAsync(fakeApp);

            MockLogger.Verify(m => m.Error(It.Is<string>(s => s.Contains(fakeLogsResult.Explanation))),
                Times.Once);

            MockErrorDialogService.Verify(m => m.DisplayWarningDialog(It.Is<string>(s => s.Contains(fakeApp.AppName)), It.Is<string>(s => s.Contains(fakeLogsResult.Explanation))),
                Times.Once);
        }

        [TestMethod]
        [TestCategory("DisplayRecentAppLogsAsync")]
        public async Task DisplayRecentAppLogs_CallsViewShowMethod_WhenLogsCmdSucceeds()
        {
            var fakeApp = new CloudFoundryApp("junk", "junk", null, "junk");
            var fakeView = new FakeOutputView();
            var fakeLogsResult = new DetailedResult<string>(content: "fake logs", succeeded: true, explanation: null, cmdDetails: _fakeSuccessCmdResult);

            MockViewLocatorService.Setup(m => m.GetViewByViewModelNameAsync(nameof(OutputViewModel), It.IsAny<string>()))
                .ReturnsAsync(fakeView);

            MockCloudFoundryService.Setup(m => m.GetRecentLogsAsync(fakeApp))
                .ReturnsAsync(fakeLogsResult);

            Assert.IsFalse(fakeView.ShowMethodWasCalled);

            await _sut.DisplayRecentAppLogsAsync(fakeApp);

            Assert.IsTrue(fakeView.ShowMethodWasCalled);
        }

        [TestMethod]
        [TestCategory("DisplayRecentAppLogsAsync")]
        public async Task DisplayRecentAppLogs_SetsAuthenticationRequiredToTrue_WhenLogsCmdFailsDueToInvalidRefreshToken()
        {
            var fakeApp = new CloudFoundryApp("junk", "junk", null, "junk");
            var invalidRefreshTokenResult = new DetailedResult<string>(content: null, succeeded: false, explanation: ":(", cmdDetails: _fakeFailureCmdResult)
            {
                FailureType = FailureType.InvalidRefreshToken
            };

            MockViewLocatorService.Setup(m => m.GetViewByViewModelNameAsync(nameof(OutputViewModel), null))
                .Callback(() => Assert.Fail("Output view does not need to be retrieved."));

            MockCloudFoundryService.Setup(m => m.GetRecentLogsAsync(fakeApp))
                .ReturnsAsync(invalidRefreshTokenResult);

            Assert.IsFalse(_sut.AuthenticationRequired);

            await _sut.DisplayRecentAppLogsAsync(fakeApp);

            Assert.IsTrue(_sut.AuthenticationRequired);

            MockErrorDialogService.Verify(
                m => m.DisplayWarningDialog(It.Is<string>(s => s.Contains(fakeApp.AppName)), It.Is<string>(s => s.Contains(invalidRefreshTokenResult.Explanation))),
                Times.Once);
        }

        [TestMethod]
        [TestCategory("AuthenticationRequired")]
        public void SettingAuthenticationRequiredToTrue_CollapsesCfInstanceViewModel()
        {
            _sut.CloudFoundryConnection = new FakeCfInstanceViewModel(_fakeCfInstance, Services, expanded: true);

            Assert.IsFalse(_sut.AuthenticationRequired);
            Assert.IsTrue(_sut.CloudFoundryConnection.IsExpanded);

            _sut.AuthenticationRequired = true;

            Assert.IsTrue(_sut.AuthenticationRequired);
            Assert.IsFalse(_sut.CloudFoundryConnection.IsExpanded);
        }

        [TestMethod]
        [TestCategory("AuthenticationRequired")]
        public void SettingAuthenticationRequiredToFalse_DoesNotExpandCfInstanceViewModel()
        {
            _sut = new TanzuExplorerViewModel(Services)
            {
                AuthenticationRequired = true, CloudFoundryConnection = new FakeCfInstanceViewModel(_fakeCfInstance, Services, expanded: false)
            };

            Assert.IsTrue(_sut.AuthenticationRequired);
            Assert.IsFalse(_sut.CloudFoundryConnection.IsExpanded);

            _sut.AuthenticationRequired = false;

            Assert.IsFalse(_sut.AuthenticationRequired);
            Assert.IsFalse(_sut.CloudFoundryConnection.IsExpanded);
        }

        [TestMethod]
        [TestCategory("LogOutCloudFoundry")]
        public void LogOutTas_SetsConnectionToNull_AndSetsIsLoggedInToFalse()
        {
            _sut.CloudFoundryConnection = _fakeTanzuConnection;
            _sut.IsLoggedIn = true;

            Assert.IsNotNull(_sut.CloudFoundryConnection);
            Assert.IsTrue(_sut.IsLoggedIn);
            _sut.LogOutCloudFoundry(_sut.CloudFoundryConnection);

            Assert.IsFalse(_sut.IsLoggedIn);
            Assert.IsNull(_sut.CloudFoundryConnection);
        }

        [TestMethod]
        [TestCategory("LogOutCloudFoundry")]
        public void LogOutTas_ClearsConnectionCache()
        {
            _sut.CloudFoundryConnection = _fakeTanzuConnection;

            _sut.LogOutCloudFoundry(_sut.CloudFoundryConnection);

            MockDataPersistenceService.Verify(m => m.ClearData(TanzuExplorerViewModel._connectionNameKey), Times.Once);
            MockDataPersistenceService.Verify(m => m.ClearData(TanzuExplorerViewModel._connectionSslPolicyKey), Times.Once);
        }

        [TestMethod]
        [TestCategory("LogOutCloudFoundry")]
        public void LogOutTas_ClearsCfAccessToken()
        {
            _sut.CloudFoundryConnection = _fakeTanzuConnection;

            _sut.LogOutCloudFoundry(_sut.CloudFoundryConnection);

            MockCloudFoundryService.Verify(m => m.LogoutCfUser(), Times.Once);
        }

        [TestMethod]
        [TestCategory("SetConnection")]
        public void SetConnection_UpdatesConnection_WithNewCfInfo_WhenConnectionIsNull()
        {
            _sut = new TanzuExplorerViewModel(Services);

            Assert.IsNull(_sut.CloudFoundryConnection);

            _sut.SetConnection(_fakeCfInstance);

            Assert.IsNotNull(_sut.CloudFoundryConnection);
            Assert.AreEqual(_fakeCfInstance, _sut.CloudFoundryConnection.CloudFoundryInstance);
        }

        [TestMethod]
        [TestCategory("SetConnection")]
        public void SetConnection_SetsAuthenticationRequiredToFalse_WhenConnectionIsNull()
        {
            _sut = new TanzuExplorerViewModel(Services) { AuthenticationRequired = true };
            _sut.PropertyChanged += (_, e) => { _receivedEvents.Add(e.PropertyName); };
            _receivedEvents.Clear();

            Assert.IsNull(_sut.CloudFoundryConnection);
            Assert.IsTrue(_sut.AuthenticationRequired);

            Assert.IsEmpty(_receivedEvents);

            _sut.SetConnection(_fakeCfInstance);

            Assert.IsFalse(_sut.AuthenticationRequired);
            CollectionAssert.Contains(_receivedEvents, "AuthenticationRequired");
        }

        [TestMethod]
        [TestCategory("SetConnection")]
        public void SetConnection_StartsBackgroundRefreshTask_WhenConnectionIsNull()
        {
            _sut = new TanzuExplorerViewModel(Services);

            Assert.IsNull(_sut.CloudFoundryConnection);

            _sut.SetConnection(_fakeCfInstance);

            MockThreadingService.Verify(m => m.StartRecurrentUITaskInBackground(_sut.BackgroundRefreshAllItems, null, It.IsAny<int>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("SetConnection")]
        public void SetConnection_SavesConnectionName_WhenConnectionIsNull()
        {
            _sut = new TanzuExplorerViewModel(Services);

            Assert.IsNull(_sut.CloudFoundryConnection);

            _sut.SetConnection(_fakeCfInstance);

            MockDataPersistenceService.Verify(m => m.WriteStringData(TanzuExplorerViewModel._connectionNameKey, _fakeCfInstance.InstanceName), Times.Once);
        }

        [TestMethod]
        [TestCategory("SetConnection")]
        public void SetConnection_SavesConnectionApiAddress_WhenConnectionIsNull()
        {
            _sut = new TanzuExplorerViewModel(Services);

            Assert.IsNull(_sut.CloudFoundryConnection);

            _sut.SetConnection(_fakeCfInstance);

            MockDataPersistenceService.Verify(m => m.WriteStringData(TanzuExplorerViewModel._connectionAddressKey, _fakeCfInstance.ApiAddress), Times.Once);
        }

        [TestMethod]
        [TestCategory("SetConnection")]
        [DataRow(true, TanzuExplorerViewModel._skipCertValidationValue)]
        [DataRow(false, TanzuExplorerViewModel._validateSslCertsValue)]
        public void SetConnection_SavesConnectionSslCertValidationPolicy_WhenConnectionIsNull(bool skipSslValue, string expectedSavedSslPolicyValue)
        {
            _sut = new TanzuExplorerViewModel(Services);

            Assert.IsNull(_sut.CloudFoundryConnection);

            _fakeCfInstance.SkipSslCertValidation = skipSslValue;
            _sut.SetConnection(_fakeCfInstance);

            MockDataPersistenceService.Verify(m => m.WriteStringData(TanzuExplorerViewModel._connectionSslPolicyKey, expectedSavedSslPolicyValue), Times.Once);
        }

        [TestMethod]
        [TestCategory("SetConnection")]
        public void SetConnection_DoesNotChangeConnection_WhenConnectionIsNotNull()
        {
            _sut.CloudFoundryConnection = new CfInstanceViewModel(_fakeCfInstance, _sut, Services);

            Assert.IsNotNull(_sut.CloudFoundryConnection);

            var initialCf = _sut.CloudFoundryConnection.CloudFoundryInstance;

            _sut.SetConnection(_fakeCfInstance);

            Assert.IsNotNull(_sut.CloudFoundryConnection);
            Assert.AreEqual(initialCf, _sut.CloudFoundryConnection.CloudFoundryInstance);
        }

        [TestMethod]
        [TestCategory("StreamAppLogsAsync")]
        public async Task StreamAppLogs_AcquiresNewOutputView_AndBeginsStreamingLogsAsync()
        {
            var expectedViewTitle = $"Logs for {_fakeCfApp.AppName}";
            var fakeOutputViewModel = new FakeOutputViewModel();
            var fakeView = new FakeOutputView { ViewModel = fakeOutputViewModel };

            MockViewLocatorService.Setup(m => m.GetViewByViewModelNameAsync(nameof(OutputViewModel), expectedViewTitle)).ReturnsAsync(fakeView);
            Assert.IsFalse(fakeOutputViewModel.BeginStreamingAppLogsForAppAsyncWasCalled);

            await _sut.StreamAppLogsAsync(_fakeCfApp);

            MockViewLocatorService.VerifyAll();
            Assert.IsTrue(fakeOutputViewModel.BeginStreamingAppLogsForAppAsyncWasCalled);
        }

        [TestMethod]
        [TestCategory("StreamAppLogsAsync")]
        public async Task StreamAppLogs_DisplaysAndLogsError_ViewLocatorServiceThrowsExceptionAsync()
        {
            var expectedViewParam = $"Logs for {_fakeCfApp.AppName}"; // intended to be title of tool window
            var fakeViewLocatorException = new Exception(":(");

            MockViewLocatorService.Setup(m => m.GetViewByViewModelNameAsync(nameof(OutputViewModel), expectedViewParam)).Throws(fakeViewLocatorException);
            MockErrorDialogService.Setup(m =>
                m.DisplayErrorDialog("Error displaying app logs", $"Something went wrong while trying to display logs for {_fakeCfApp.AppName}, please try again."));
            MockLogger.Setup(m => m.Error("Caught exception trying to stream app logs for '{AppName}': {AppLogsException}", _fakeCfApp.AppName, fakeViewLocatorException));

            await _sut.StreamAppLogsAsync(_fakeCfApp);

            MockErrorDialogService.VerifyAll();
            MockLogger.VerifyAll();
        }
    }
}