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
    public class TasExplorerViewModelTests : ViewModelTestSupport
    {
        private TasExplorerViewModel _sut;
        private List<string> _receivedEvents;
        private CfInstanceViewModel _fakeTasConnection;

        [TestInitialize]
        public void TestInit()
        {
            RenewMockServices();

            // set up mockUiDispatcherService to run whatever method is passed
            // to RunOnUiThread; do not delegate to the UI Dispatcher
            MockUiDispatcherService.Setup(mock => mock.
                RunOnUiThreadAsync(It.IsAny<Action>()))
                    .Callback<Action>(action =>
                    {
                        action();
                    });

            _receivedEvents = new List<string>();
            _fakeTasConnection = new FakeCfInstanceViewModel(_fakeCfInstance, Services);

            _sut = new TasExplorerViewModel(Services)
            {
                TasConnection = _fakeTasConnection,
            };
            _sut.PropertyChanged += (sender, e) =>
            {
                _receivedEvents.Add(e.PropertyName);
            };
        }

        [TestCleanup]
        public void TestCleanup()
        {
            MockDataPersistenceService.VerifyAll();
            MockCloudFoundryService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("Ctor")]
        public void Ctor_SetsTasConnectionToNull_WhenSavedConnectionNameNull()
        {
            MockDataPersistenceService.Setup(m => m.ReadStringData(TasExplorerViewModel._connectionNameKey)).Returns((string)null);
            MockDataPersistenceService.Setup(m => m.ReadStringData(TasExplorerViewModel._connectionAddressKey)).Returns("junk non-null value");
            MockDataPersistenceService.Setup(m => m.SavedCfCredsExist()).Returns(true);

            _sut = new TasExplorerViewModel(Services);

            Assert.IsNull(_sut.TasConnection);
        }

        [TestMethod]
        [TestCategory("Ctor")]
        public void Ctor_SetsTasConnectionToNull_WhenSavedConnectionAddressNull()
        {
            MockDataPersistenceService.Setup(m => m.ReadStringData(TasExplorerViewModel._connectionNameKey)).Returns("junk non-null value");
            MockDataPersistenceService.Setup(m => m.ReadStringData(TasExplorerViewModel._connectionAddressKey)).Returns((string)null);
            MockDataPersistenceService.Setup(m => m.SavedCfCredsExist()).Returns(true);

            _sut = new TasExplorerViewModel(Services);

            Assert.IsNull(_sut.TasConnection);
        }

        [TestMethod]
        [TestCategory("Ctor")]
        public void Ctor_SetsTasConnectionToNull_WhenAccessTokenIrretrievable()
        {
            MockDataPersistenceService.Setup(m => m.SavedCfCredsExist()).Returns(false);
            MockDataPersistenceService.Setup(m => m.ReadStringData(TasExplorerViewModel._connectionNameKey)).Returns("junk non-null value");
            MockDataPersistenceService.Setup(m => m.ReadStringData(TasExplorerViewModel._connectionAddressKey)).Returns("junk non-null value");
            MockDataPersistenceService.Setup(m => m.ReadStringData(TasExplorerViewModel._connectionSslPolicyKey)).Returns("junk non-null value");

            _sut = new TasExplorerViewModel(Services);

            Assert.IsNull(_sut.TasConnection);
        }

        [TestMethod]
        [TestCategory("Ctor")]
        [DataRow(TasExplorerViewModel._skipCertValidationValue, true)]
        [DataRow(TasExplorerViewModel._validateSslCertsValue, false)]
        [DataRow("unexpected value", false)]
        [DataRow(null, false)]
        public void Ctor_RestoresTasConnection_WhenSavedConnectionNameAddressAndTokenExist(string savedSslPolicyValue, bool expectedSkipSslValue)
        {
            var savedConnectionName = "junk";
            var savedConnectionAddress = "junk";

            MockDataPersistenceService.Setup(m => m.SavedCfCredsExist()).Returns(true);
            MockDataPersistenceService.Setup(m => m.ReadStringData(TasExplorerViewModel._connectionNameKey)).Returns(savedConnectionName);
            MockDataPersistenceService.Setup(m => m.ReadStringData(TasExplorerViewModel._connectionAddressKey)).Returns(savedConnectionAddress);
            MockDataPersistenceService.Setup(m => m.ReadStringData(TasExplorerViewModel._connectionSslPolicyKey)).Returns(savedSslPolicyValue);

            _sut = new TasExplorerViewModel(Services);

            Assert.IsNotNull(_sut.TasConnection);
            Assert.AreEqual(savedConnectionName, _sut.TasConnection.CloudFoundryInstance.InstanceName);
            Assert.AreEqual(savedConnectionAddress, _sut.TasConnection.CloudFoundryInstance.ApiAddress);
            Assert.AreEqual(expectedSkipSslValue, _sut.TasConnection.CloudFoundryInstance.SkipSslCertValidation);
        }

        [TestMethod]
        [TestCategory("TasConnection")]
        [TestCategory("TreeRoot")]
        public void SettingTasConnection_PopulatesTreeRoot()
        {
            _sut = new TasExplorerViewModel(Services);

            Assert.IsNull(_sut.TasConnection);
            Assert.AreEqual(1, _sut.TreeRoot.Count);
            Assert.IsTrue(_sut.TreeRoot[0] is LoginPromptViewModel);

            _sut.TasConnection = new CfInstanceViewModel(_fakeCfInstance, _sut, Services);

            Assert.IsNotNull(_sut.TasConnection);
            Assert.AreEqual(1, _sut.TreeRoot.Count);
            Assert.AreEqual(_sut.TreeRoot[0], _sut.TasConnection);
        }

        [TestMethod]
        [TestCategory("TasConnection")]
        [TestCategory("TreeRoot")]
        public void SettingTasConnectionToNull_ClearsTreeRoot()
        {
            _sut.TasConnection = _fakeTasConnection;

            Assert.IsNotNull(_sut.TasConnection);
            Assert.AreEqual(1, _sut.TreeRoot.Count);
            Assert.AreEqual(_sut.TreeRoot[0], _sut.TasConnection);

            _sut.TasConnection = null;

            Assert.IsNull(_sut.TasConnection);
            Assert.AreEqual(1, _sut.TreeRoot.Count);
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
            Assert.IsTrue(_sut.CanStopCfApp(null));
        }

        [TestMethod]
        public void CanStartCfApp_ReturnsTrue()
        {
            Assert.IsTrue(_sut.CanStartCfApp(null));
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
        [TestCategory("OpenLoginView")]
        public void OpenLoginView_DisplaysLoginDialog_WhenTasConnectionIsNull()
        {
            _sut = new TasExplorerViewModel(Services);

            Assert.IsNull(_sut.TasConnection);

            _sut.OpenLoginView(null);

            MockDialogService.Verify(ds => ds.ShowModal(typeof(LoginViewModel).Name, null), Times.Once);
        }

        [TestMethod]
        [TestCategory("OpenLoginView")]
        public void OpenLoginView_DisplaysErrorDialog_WhenTasConnectionIsNotNull()
        {
            _sut.TasConnection = _fakeTasConnection;

            Assert.IsNotNull(_sut.TasConnection);

            _sut.OpenLoginView(null);

            MockErrorDialogService.Verify(m => m.
                DisplayErrorDialog(TasExplorerViewModel._singleLoginErrorTitle,
                                   It.Is<string>(s => s.Contains(TasExplorerViewModel._singleLoginErrorMessage1) && s.Contains(TasExplorerViewModel._singleLoginErrorMessage2))),
                    Times.Once);
        }

        [TestMethod]
        [TestCategory("OpenLoginView")]
        public void OpenLoginView_DoesNotChangeAuthenticationRequired_WhenTasConnectionDoesNotGetSet()
        {
            _sut = new TasExplorerViewModel(Services)
            {
                AuthenticationRequired = true
            };

            MockDialogService.Setup(mock => mock.
                ShowModal(typeof(LoginViewModel).Name, null))
                    .Callback(() =>
                    {
                        // Simulate unsuccessful login by NOT setting TasConnection as LoginView would've done on a successful login
                    });

            Assert.IsTrue(_sut.AuthenticationRequired);
            Assert.IsNull(_sut.TasConnection);

            Assert.IsTrue(_sut.CanOpenLoginView(null));
            _sut.OpenLoginView(null);

            Assert.IsNull(_sut.TasConnection);
            Assert.IsTrue(_sut.AuthenticationRequired); // this prop should not have changed
        }

        [TestMethod]
        [TestCategory("OpenDeletionView")]
        public void OpenDeletionView_DisplaysDeletionDialog_WhenCfAppIsDeleted()
        {
            var fakeApp = new CloudFoundryApp("junk", "junk", parentSpace: null, null);

            _sut.OpenDeletionView(fakeApp);

            MockAppDeletionConfirmationViewModel.Verify(m => m.ShowConfirmation(fakeApp), Times.Once);
        }

        [TestMethod]
        [TestCategory("OpenDeletionView")]
        public void OpenDeletionView_DoesNotDisplaysDeletionDialog_WhenArgumentIsNotACfApp()
        {
            _sut.OpenDeletionView(null);

            MockDialogService.Verify(ds => ds.ShowModal(typeof(AppDeletionConfirmationViewModel).Name, null), Times.Never);
        }

        [TestMethod]
        [TestCategory("StopCfApp")]
        public async Task StopCfApp_CallsStopCfAppAsync()
        {
            var fakeApp = new CloudFoundryApp("junk", "junk", parentSpace: null, null);

            MockCloudFoundryService.Setup(mock => mock.StopAppAsync(fakeApp, false, It.IsAny<int>())).ReturnsAsync(_fakeSuccessDetailedResult);

            Exception shouldStayNull = null;
            try
            {
                await _sut.StopCfApp(fakeApp);
            }
            catch (Exception e)
            {
                shouldStayNull = e;
            }

            Assert.IsNull(shouldStayNull);
            MockCloudFoundryService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("StopCfApp")]
        public async Task StopCfApp_DisplaysErrorDialog_WhenStopAppAsyncFails()
        {
            var fakeApp = new CloudFoundryApp("junk", "junk", parentSpace: null, null);

            MockCloudFoundryService.Setup(mock => mock.
                StopAppAsync(fakeApp, false, It.IsAny<int>()))
                    .ReturnsAsync(_fakeFailureDetailedResult);

            await _sut.StopCfApp(fakeApp);

            var expectedErrorTitle = $"{TasExplorerViewModel._stopAppErrorMsg} {fakeApp.AppName}.";
            var expectedErrorMsg = _fakeFailureDetailedResult.Explanation;

            MockErrorDialogService.Verify(mock => mock.
              DisplayErrorDialog(expectedErrorTitle, expectedErrorMsg),
                Times.Once);
        }

        [TestMethod]
        [TestCategory("StopCfApp")]
        public async Task StopCfApp_LogsError_WhenStopAppAsyncFails()
        {
            var fakeApp = new CloudFoundryApp("junk", "junk", parentSpace: null, null);

            MockCloudFoundryService.Setup(mock => mock.
                StopAppAsync(fakeApp, false, It.IsAny<int>()))
                    .ReturnsAsync(_fakeFailureDetailedResult);

            await _sut.StopCfApp(fakeApp);

            var logPropVal1 = "{AppName}";
            var logPropVal2 = "{StopResult}";
            var expectedLogMsg = $"{TasExplorerViewModel._stopAppErrorMsg} {logPropVal1}. {logPropVal2}";

            MockLogger.Verify(m => m.
                Error(expectedLogMsg, fakeApp.AppName, _fakeFailureDetailedResult.ToString()),
                    Times.Once);
        }

        [TestMethod]
        [TestCategory("StartCfApp")]
        public async Task StartCfApp_CallsStartAppAsync()
        {
            var fakeApp = new CloudFoundryApp("junk", "junk", parentSpace: null, null);

            MockCloudFoundryService.Setup(mock => mock.StartAppAsync(fakeApp, false, It.IsAny<int>())).ReturnsAsync(_fakeSuccessDetailedResult);

            Exception shouldStayNull = null;
            try
            {
                await _sut.StartCfApp(fakeApp);
            }
            catch (Exception e)
            {
                shouldStayNull = e;
            }

            Assert.IsNull(shouldStayNull);
            MockCloudFoundryService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("StartCfApp")]
        public async Task StartCfApp_DisplaysErrorDialog_WhenStartAppAsyncFails()
        {
            var fakeApp = new CloudFoundryApp("junk", "junk", parentSpace: null, null);

            MockCloudFoundryService.Setup(mock => mock.
                StartAppAsync(fakeApp, false, It.IsAny<int>()))
                    .ReturnsAsync(_fakeFailureDetailedResult);

            await _sut.StartCfApp(fakeApp);

            var expectedErrorTitle = $"{TasExplorerViewModel._startAppErrorMsg} {fakeApp.AppName}.";
            var expectedErrorMsg = _fakeFailureDetailedResult.Explanation;

            MockErrorDialogService.Verify(mock => mock.
              DisplayErrorDialog(expectedErrorTitle, expectedErrorMsg),
                Times.Once);
        }

        [TestMethod]
        [TestCategory("StartCfApp")]
        public async Task StartCfApp_LogsError_WhenStartAppAsyncFails()
        {
            var fakeApp = new CloudFoundryApp("junk", "junk", parentSpace: null, null);

            MockCloudFoundryService.Setup(mock => mock.
                StartAppAsync(fakeApp, false, It.IsAny<int>()))
                    .ReturnsAsync(_fakeFailureDetailedResult);

            await _sut.StartCfApp(fakeApp);

            var logPropVal1 = "{AppName}";
            var logPropVal2 = "{StartResult}";
            var expectedLogMsg = $"{TasExplorerViewModel._startAppErrorMsg} {logPropVal1}. {logPropVal2}";

            MockLogger.Verify(m => m.
                Error(expectedLogMsg, fakeApp.AppName, _fakeFailureDetailedResult.ToString()),
                    Times.Once);
        }

        [TestMethod]
        [TestCategory("RefreshOrg")]
        public async Task RefreshOrg_CallsUpdateAllChildrenOnOrg_WhenArgIsOrgViewModel()
        {
            var ovm = new FakeOrgViewModel(_fakeCfOrg, Services);

            Assert.IsTrue(ovm is OrgViewModel);
            Assert.AreEqual(0, ovm.NumUpdates);

            await _sut.RefreshOrg(ovm);

            Assert.AreEqual(1, ovm.NumUpdates);
        }

        [TestMethod]
        [TestCategory("RefreshSpace")]
        public async Task RefreshSpace_CallsUpdateAllChildrenOnSpace_WhenArgIsSpaceViewModel()
        {
            var svm = new FakeSpaceViewModel(_fakeCfSpace, Services);

            Assert.IsTrue(svm is SpaceViewModel);
            Assert.AreEqual(0, svm.NumUpdates);

            await _sut.RefreshSpace(svm);

            Assert.AreEqual(1, svm.NumUpdates);
        }

        [TestMethod]
        [TestCategory("RefreshAllItems")]
        public void RefreshAllItems_SetsIsRefreshingAllFalse_WhenTasConnectionNull()
        {
            _sut = new TasExplorerViewModel(Services)
            {
                IsRefreshingAll = true
            };

            Assert.IsNull(_sut.TasConnection);
            Assert.IsTrue(_sut.IsRefreshingAll);

            _sut.RefreshAllItems();

            Assert.IsFalse(_sut.IsRefreshingAll);
        }

        [TestMethod]
        [TestCategory("RefreshAllItems")]
        public void RefreshAllItems_SetsThreadServiceIsPollingFalse_WhenTasConnectionNull()
        {
            _sut = new TasExplorerViewModel(Services);

            MockThreadingService.SetupSet(mock => mock.IsPolling = false).Verifiable();

            Assert.IsNull(_sut.TasConnection);

            _sut.RefreshAllItems();

            MockThreadingService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("RefreshAllItems")]
        public void RefreshAllItems_SetsIsRefreshingAllTrue_WhenNotPreviouslyRefreshingAll_AndTasConnectionNotNull()
        {
            _sut.TasConnection = _fakeTasConnection;

            Assert.IsNotNull(_sut.TasConnection);
            Assert.IsFalse(_sut.IsRefreshingAll);

            _sut.RefreshAllItems();

            Assert.IsTrue(_sut.IsRefreshingAll);
        }

        [TestMethod]
        [TestCategory("RefreshAllItems")]
        public void RefreshAllItems_StartsBackgroundTaskToRefreshAllItems_WhenNotPreviouslyRefreshingAll_AndTasConnectionNotNull()
        {
            _sut.TasConnection = _fakeTasConnection;

            MockThreadingService.Setup(m => m.StartBackgroundTask(It.IsAny<Func<Task>>())).Verifiable();

            Assert.IsNotNull(_sut.TasConnection);
            Assert.IsFalse(_sut.IsRefreshingAll);

            _sut.RefreshAllItems();

            MockThreadingService.Verify(m => m.StartBackgroundTask(_sut.UpdateAllTreeItems), Times.Once);
        }

        [TestMethod]
        [TestCategory("RefreshAllItems")]
        public void RefreshAllItems_DoesNotStartRefreshTask_WhenRefreshIsInProgress()
        {
            _sut.IsRefreshingAll = true;

            Assert.IsTrue(_sut.IsRefreshingAll);

            _sut.RefreshAllItems(null);

            MockThreadingService.Verify(m => m.StartBackgroundTask(It.IsAny<Func<Task>>()), Times.Never);
        }

        [TestMethod]
        [TestCategory("DisplayRecentAppLogs")]
        public async Task DisplayRecentAppLogs_LogsError_WhenArgumentTypeIsNotApp()
        {
            await _sut.DisplayRecentAppLogs(new object());
            MockLogger.Verify(m => m.Error(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("DisplayRecentAppLogs")]
        public async Task DisplayRecentAppLogs_DisplaysErrorDialog_WhenLogsCmdFails()
        {
            var fakeApp = new CloudFoundryApp("junk", "junk", null, "junk");
            var fakeLogsResult = new DetailedResult<string>(content: null, succeeded: false, explanation: ":(", cmdDetails: _fakeFailureCmdResult);

            MockViewLocatorService.Setup(m => m.
                GetViewByViewModelName(nameof(OutputViewModel), null))
                    .Callback(() => Assert.Fail("Output view does not need to be retrieved."));

            MockCloudFoundryService.Setup(m => m.
                GetRecentLogsAsync(fakeApp))
                    .ReturnsAsync(fakeLogsResult);

            await _sut.DisplayRecentAppLogs(fakeApp);

            MockLogger.Verify(m => m.
                Error(It.Is<string>(s => s.Contains(fakeLogsResult.Explanation))),
                    Times.Once);

            MockErrorDialogService.Verify(m => m.
                DisplayWarningDialog(It.Is<string>(s => s.Contains(fakeApp.AppName)), It.Is<string>(s => s.Contains(fakeLogsResult.Explanation))),
                    Times.Once);
        }

        [TestMethod]
        [TestCategory("DisplayRecentAppLogs")]
        public async Task DisplayRecentAppLogs_CallsViewShowMethod_WhenLogsCmdSucceeds()
        {
            var fakeApp = new CloudFoundryApp("junk", "junk", null, "junk");
            var fakeView = new FakeOutputView();
            var fakeLogsResult = new DetailedResult<string>(content: "fake logs", succeeded: true, explanation: null, cmdDetails: _fakeSuccessCmdResult);

            MockViewLocatorService.Setup(m => m.
                GetViewByViewModelName(nameof(OutputViewModel), It.IsAny<string>()))
                    .Returns(fakeView);

            MockCloudFoundryService.Setup(m => m.
                GetRecentLogsAsync(fakeApp))
                    .ReturnsAsync(fakeLogsResult);

            Assert.IsFalse(fakeView.ShowMethodWasCalled);

            await _sut.DisplayRecentAppLogs(fakeApp);

            Assert.IsTrue(fakeView.ShowMethodWasCalled);
        }

        [TestMethod]
        [TestCategory("DisplayRecentAppLogs")]
        public async Task DisplayRecentAppLogs_SetsAuthenticationRequiredToTrue_WhenLogsCmdFailsDueToInvalidRefreshToken()
        {
            var fakeApp = new CloudFoundryApp("junk", "junk", null, "junk");
            var invalidRefreshTokenResult = new DetailedResult<string>(content: null, succeeded: false, explanation: ":(", cmdDetails: _fakeFailureCmdResult)
            {
                FailureType = FailureType.InvalidRefreshToken
            };

            MockViewLocatorService.Setup(m => m.
                GetViewByViewModelName(nameof(OutputViewModel), null))
                    .Callback(() => Assert.Fail("Output view does not need to be retrieved."));

            MockCloudFoundryService.Setup(m => m.
                GetRecentLogsAsync(fakeApp))
                    .ReturnsAsync(invalidRefreshTokenResult);

            Assert.IsFalse(_sut.AuthenticationRequired);

            await _sut.DisplayRecentAppLogs(fakeApp);

            Assert.IsTrue(_sut.AuthenticationRequired);

            MockErrorDialogService.Verify(m => m.
                DisplayWarningDialog(It.Is<string>(s => s.Contains(fakeApp.AppName)), It.Is<string>(s => s.Contains(invalidRefreshTokenResult.Explanation))),
                    Times.Once);
        }

        [TestMethod]
        [TestCategory("AuthenticationRequired")]
        public void SettingAuthenticationRequiredToTrue_CollapsesCfInstanceViewModel()
        {
            _sut.TasConnection = new FakeCfInstanceViewModel(_fakeCfInstance, Services, expanded: true);

            Assert.IsFalse(_sut.AuthenticationRequired);
            Assert.IsTrue(_sut.TasConnection.IsExpanded);

            _sut.AuthenticationRequired = true;

            Assert.IsTrue(_sut.AuthenticationRequired);
            Assert.IsFalse(_sut.TasConnection.IsExpanded);
        }

        [TestMethod]
        [TestCategory("AuthenticationRequired")]
        public void SettingAuthenticationRequiredToFalse_DoesNotExpandCfInstanceViewModel()
        {
            _sut = new TasExplorerViewModel(Services)
            {
                AuthenticationRequired = true,
            };

            _sut.TasConnection = new FakeCfInstanceViewModel(_fakeCfInstance, Services, expanded: false);

            Assert.IsTrue(_sut.AuthenticationRequired);
            Assert.IsFalse(_sut.TasConnection.IsExpanded);

            _sut.AuthenticationRequired = false;

            Assert.IsFalse(_sut.AuthenticationRequired);
            Assert.IsFalse(_sut.TasConnection.IsExpanded);
        }

        [TestMethod]
        [TestCategory("LogOutTas")]
        public void LogOutTas_SetsTasConnectionToNull_AndSetsIsLoggedInToFalse()
        {
            _sut.TasConnection = _fakeTasConnection;
            _sut.IsLoggedIn = true;

            Assert.IsNotNull(_sut.TasConnection);
            Assert.IsTrue(_sut.IsLoggedIn);
            _sut.LogOutTas(_sut.TasConnection);

            Assert.IsNull(_sut.TasConnection);
            Assert.IsFalse(_sut.IsLoggedIn);
        }

        [TestMethod]
        [TestCategory("LogOutTas")]
        public void LogOutTas_ClearsConnectionCache()
        {
            _sut.TasConnection = _fakeTasConnection;

            _sut.LogOutTas(_sut.TasConnection);

            MockDataPersistenceService.Verify(m => m.ClearData(TasExplorerViewModel._connectionNameKey), Times.Once);
            MockDataPersistenceService.Verify(m => m.ClearData(TasExplorerViewModel._connectionAddressKey), Times.Once);
            MockDataPersistenceService.Verify(m => m.ClearData(TasExplorerViewModel._connectionSslPolicyKey), Times.Once);
        }

        [TestMethod]
        [TestCategory("LogOutTas")]
        public void LogOutTas_ClearsCfAccessToken()
        {
            _sut.TasConnection = _fakeTasConnection;

            _sut.LogOutTas(_sut.TasConnection);

            MockCloudFoundryService.Verify(m => m.LogoutCfUser(), Times.Once);
        }

        [TestMethod]
        [TestCategory("SetConnection")]
        public void SetConnection_UpdatesTasConnection_WithNewCfInfo_WhenTasConnectionIsNull()
        {
            _sut = new TasExplorerViewModel(Services);

            Assert.IsNull(_sut.TasConnection);

            _sut.SetConnection(_fakeCfInstance);

            Assert.IsNotNull(_sut.TasConnection);
            Assert.AreEqual(_fakeCfInstance, _sut.TasConnection.CloudFoundryInstance);
        }

        [TestMethod]
        [TestCategory("SetConnection")]
        public void SetConnection_SetsAuthenticationRequiredToFalse_WhenTasConnectionIsNull()
        {
            _sut = new TasExplorerViewModel(Services)
            {
                AuthenticationRequired = true
            };
            _sut.PropertyChanged += (sender, e) =>
            {
                _receivedEvents.Add(e.PropertyName);
            };
            _receivedEvents.Clear();

            Assert.IsNull(_sut.TasConnection);
            Assert.IsTrue(_sut.AuthenticationRequired);

            Assert.AreEqual(0, _receivedEvents.Count);

            _sut.SetConnection(_fakeCfInstance);

            Assert.IsFalse(_sut.AuthenticationRequired);
            CollectionAssert.Contains(_receivedEvents, "AuthenticationRequired");
        }

        [TestMethod]
        [TestCategory("SetConnection")]
        public void SetConnection_StartsBackgroundRefreshTask_WhenTasConnectionIsNull()
        {
            _sut = new TasExplorerViewModel(Services);

            Assert.IsNull(_sut.TasConnection);

            _sut.SetConnection(_fakeCfInstance);

            MockThreadingService.Verify(m => m.StartRecurrentUiTaskInBackground(_sut.RefreshAllItems, null, It.IsAny<int>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("SetConnection")]
        public void SetConnection_SavesConnectionName_WhenTasConnectionIsNull()
        {
            _sut = new TasExplorerViewModel(Services);

            Assert.IsNull(_sut.TasConnection);

            _sut.SetConnection(_fakeCfInstance);

            MockDataPersistenceService.Verify(m => m.WriteStringData(TasExplorerViewModel._connectionNameKey, _fakeCfInstance.InstanceName), Times.Once);
        }

        [TestMethod]
        [TestCategory("SetConnection")]
        public void SetConnection_SavesConnectionApiAddress_WhenTasConnectionIsNull()
        {
            _sut = new TasExplorerViewModel(Services);

            Assert.IsNull(_sut.TasConnection);

            _sut.SetConnection(_fakeCfInstance);

            MockDataPersistenceService.Verify(m => m.WriteStringData(TasExplorerViewModel._connectionAddressKey, _fakeCfInstance.ApiAddress), Times.Once);
        }

        [TestMethod]
        [TestCategory("SetConnection")]
        [DataRow(true, TasExplorerViewModel._skipCertValidationValue)]
        [DataRow(false, TasExplorerViewModel._validateSslCertsValue)]
        public void SetConnection_SavesConnectionSslCertValidationPolicy_WhenTasConnectionIsNull(bool skipSslValue, string expectedSavedSslPolicyValue)
        {
            _sut = new TasExplorerViewModel(Services);

            Assert.IsNull(_sut.TasConnection);

            _fakeCfInstance.SkipSslCertValidation = skipSslValue;
            _sut.SetConnection(_fakeCfInstance);

            MockDataPersistenceService.Verify(m => m.WriteStringData(TasExplorerViewModel._connectionSslPolicyKey, expectedSavedSslPolicyValue), Times.Once);
        }

        [TestMethod]
        [TestCategory("SetConnection")]
        public void SetConnection_DoesNotChangeTasConnection_WhenTasConnectionIsNotNull()
        {
            _sut.TasConnection = new CfInstanceViewModel(_fakeCfInstance, _sut, Services);

            Assert.IsNotNull(_sut.TasConnection);

            var initialCf = _sut.TasConnection.CloudFoundryInstance;

            _sut.SetConnection(_fakeCfInstance);

            Assert.IsNotNull(_sut.TasConnection);
            Assert.AreEqual(initialCf, _sut.TasConnection.CloudFoundryInstance);
        }

        [TestMethod]
        [TestCategory("StreamAppLogs")]
        public void StreamAppLogs_AcquiresNewOutputView_AndBeginsStreamingLogs()
        {
            var expectedViewTitle = $"Logs for {_fakeCfApp.AppName}";
            var fakeOutputViewModel = new FakeOutputViewModel();
            var fakeView = new FakeOutputView()
            {
                ViewModel = fakeOutputViewModel,
            };
            var fakeRecentLogsResult = new DetailedResult<string>
            {
                Succeeded = true,
                Content = "fake historical app logs",
            };

            MockViewLocatorService.Setup(m => m.GetViewByViewModelName(nameof(OutputViewModel), expectedViewTitle)).Returns(fakeView);
            Assert.IsFalse(fakeOutputViewModel.BeginStreamingAppLogsForAppAsyncWasCalled);

            _sut.StreamAppLogs(_fakeCfApp);

            MockViewLocatorService.VerifyAll();
            Assert.IsTrue(fakeOutputViewModel.BeginStreamingAppLogsForAppAsyncWasCalled);
        }

        [TestMethod]
        [TestCategory("StreamAppLogs")]
        public void StreamAppLogs_DisplaysAndLogsError_ViewLocatorServiceThrowsException()
        {
            var expectedViewParam = $"Logs for {_fakeCfApp.AppName}"; // intended to be title of tool window
            var fakeView = new FakeOutputView();
            var fakeViewModel = fakeView.ViewModel as IOutputViewModel;
            Action<string> expectedStdOutDelegate = fakeViewModel.AppendLine;
            Action<string> expectedStdErrDelegate = fakeViewModel.AppendLine;
            var fakeViewLocatorException = new Exception(":(");

            MockViewLocatorService.Setup(m => m.GetViewByViewModelName(nameof(OutputViewModel), expectedViewParam)).Throws(fakeViewLocatorException);
            MockErrorDialogService.Setup(m => m.DisplayErrorDialog("Error displaying app logs", $"Something went wrong while trying to display logs for {_fakeCfApp.AppName}, please try again."));
            MockLogger.Setup(m => m.Error("Caught exception trying to stream app logs for '{AppName}': {AppLogsException}", _fakeCfApp.AppName, fakeViewLocatorException));

            _sut.StreamAppLogs(_fakeCfApp);

            MockErrorDialogService.VerifyAll();
            MockLogger.VerifyAll();
        }
    }
}
