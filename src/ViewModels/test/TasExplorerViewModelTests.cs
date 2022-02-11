using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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

            _sut = new TasExplorerViewModel(Services);
            _receivedEvents = new List<string>();
            _fakeTasConnection = new FakeCfInstanceViewModel(FakeCfInstance, Services);

            _sut = new TasExplorerViewModel(Services);
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
            MockDataPersistenceService.Setup(m => m.ReadStringData(TasExplorerViewModel.ConnectionNameKey)).Returns((string)null);
            MockDataPersistenceService.Setup(m => m.ReadStringData(TasExplorerViewModel.ConnectionAddressKey)).Returns("junk non-null value");
            MockCloudFoundryService.Setup(m => m.IsValidConnection()).Returns(true);

            _sut = new TasExplorerViewModel(Services);

            Assert.IsNull(_sut.TasConnection);
        }

        [TestMethod]
        [TestCategory("Ctor")]
        public void Ctor_SetsTasConnectionToNull_WhenSavedConnectionAddressNull()
        {
            MockDataPersistenceService.Setup(m => m.ReadStringData(TasExplorerViewModel.ConnectionNameKey)).Returns("junk non-null value");
            MockDataPersistenceService.Setup(m => m.ReadStringData(TasExplorerViewModel.ConnectionAddressKey)).Returns((string)null);
            MockCloudFoundryService.Setup(m => m.IsValidConnection()).Returns(true);

            _sut = new TasExplorerViewModel(Services);

            Assert.IsNull(_sut.TasConnection);
        }

        [TestMethod]
        [TestCategory("Ctor")]
        public void Ctor_SetsTasConnectionToNull_WhenAccessTokenIrretrievable()
        {
            MockDataPersistenceService.Setup(m => m.ReadStringData(TasExplorerViewModel.ConnectionNameKey)).Returns("junk non-null value");
            MockDataPersistenceService.Setup(m => m.ReadStringData(TasExplorerViewModel.ConnectionAddressKey)).Returns("junk non-null value");
            MockCloudFoundryService.Setup(m => m.IsValidConnection()).Returns(false);

            _sut = new TasExplorerViewModel(Services);

            Assert.IsNull(_sut.TasConnection);
        }

        [TestMethod]
        [TestCategory("Ctor")]
        public void Ctor_RestoresTasConnection_WhenSavedConnectionNameAddressAndTokenExist()
        {
            var savedConnectionName = "junk";
            var savedConnectionAddress = "junk";

            MockDataPersistenceService.Setup(m => m.ReadStringData(TasExplorerViewModel.ConnectionNameKey)).Returns(savedConnectionName);
            MockDataPersistenceService.Setup(m => m.ReadStringData(TasExplorerViewModel.ConnectionAddressKey)).Returns(savedConnectionAddress);
            MockCloudFoundryService.Setup(m => m.IsValidConnection()).Returns(true);

            _sut = new TasExplorerViewModel(Services);

            Assert.IsNotNull(_sut.TasConnection);
            Assert.AreEqual(savedConnectionName, _sut.TasConnection.CloudFoundryInstance.InstanceName);
            Assert.AreEqual(savedConnectionAddress, _sut.TasConnection.CloudFoundryInstance.ApiAddress);
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

            _sut.TasConnection = new CfInstanceViewModel(FakeCfInstance, _sut, Services);

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
        public void CanRefreshCfInstance_ReturnsTrue()
        {
            Assert.IsTrue(_sut.CanRefreshCfInstance(null));
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
        public void CanRefreshApp_ReturnsTrue()
        {
            Assert.IsTrue(_sut.CanRefreshApp(null));
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
            Assert.IsNull(_sut.TasConnection);

            _sut.OpenLoginView(null);

            MockDialogService.Verify(ds => ds.ShowDialog(typeof(LoginViewModel).Name, null), Times.Once);
        }

        [TestMethod]
        [TestCategory("OpenLoginView")]
        public void OpenLoginView_DisplaysErrorDialog_WhenTasConnectionIsNotNull()
        {
            _sut.TasConnection = _fakeTasConnection;

            Assert.IsNotNull(_sut.TasConnection);

            _sut.OpenLoginView(null);

            MockErrorDialogService.Verify(m => m.
                DisplayErrorDialog(TasExplorerViewModel.SingleLoginErrorTitle,
                                   It.Is<string>(s => s.Contains(TasExplorerViewModel.SingleLoginErrorMessage1) && s.Contains(TasExplorerViewModel.SingleLoginErrorMessage2))),
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
                ShowDialog(typeof(LoginViewModel).Name, null))
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

            MockDialogService.Verify(ds => ds.ShowDialog(typeof(AppDeletionConfirmationViewModel).Name, null), Times.Never);
        }

        [TestMethod]
        [TestCategory("StopCfApp")]
        public async Task StopCfApp_CallsStopCfAppAsync()
        {
            var fakeApp = new CloudFoundryApp("junk", "junk", parentSpace: null, null);

            MockCloudFoundryService.Setup(mock => mock.StopAppAsync(fakeApp, false, It.IsAny<int>())).ReturnsAsync(FakeSuccessDetailedResult);

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
                    .ReturnsAsync(FakeFailureDetailedResult);

            await _sut.StopCfApp(fakeApp);

            var expectedErrorTitle = $"{TasExplorerViewModel._stopAppErrorMsg} {fakeApp.AppName}.";
            var expectedErrorMsg = FakeFailureDetailedResult.Explanation;

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
                    .ReturnsAsync(FakeFailureDetailedResult);

            await _sut.StopCfApp(fakeApp);

            var logPropVal1 = "{AppName}";
            var logPropVal2 = "{StopResult}";
            var expectedLogMsg = $"{TasExplorerViewModel._stopAppErrorMsg} {logPropVal1}. {logPropVal2}";

            MockLogger.Verify(m => m.
                Error(expectedLogMsg, fakeApp.AppName, FakeFailureDetailedResult.ToString()),
                    Times.Once);
        }

        [TestMethod]
        [TestCategory("StartCfApp")]
        public async Task StartCfApp_CallsStartAppAsync()
        {
            var fakeApp = new CloudFoundryApp("junk", "junk", parentSpace: null, null);

            MockCloudFoundryService.Setup(mock => mock.StartAppAsync(fakeApp, false, It.IsAny<int>())).ReturnsAsync(FakeSuccessDetailedResult);

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
                    .ReturnsAsync(FakeFailureDetailedResult);

            await _sut.StartCfApp(fakeApp);

            var expectedErrorTitle = $"{TasExplorerViewModel._startAppErrorMsg} {fakeApp.AppName}.";
            var expectedErrorMsg = FakeFailureDetailedResult.Explanation;

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
                    .ReturnsAsync(FakeFailureDetailedResult);

            await _sut.StartCfApp(fakeApp);

            var logPropVal1 = "{AppName}";
            var logPropVal2 = "{StartResult}";
            var expectedLogMsg = $"{TasExplorerViewModel._startAppErrorMsg} {logPropVal1}. {logPropVal2}";

            MockLogger.Verify(m => m.
                Error(expectedLogMsg, fakeApp.AppName, FakeFailureDetailedResult.ToString()),
                    Times.Once);
        }

        [TestMethod]
        [TestCategory("RefreshOrg")]
        public async Task RefreshOrg_CallsUpdateAllChildrenOnOrg_WhenArgIsOrgViewModel()
        {
            var ovm = new FakeOrgViewModel(FakeCfOrg, Services);
            var spacesCollection = new DetailedResult<List<CloudFoundrySpace>>();

            Assert.IsTrue(ovm is OrgViewModel);
            Assert.AreEqual(0, ovm.NumUpdates);

            await _sut.RefreshOrg(ovm);

            Assert.AreEqual(1, ovm.NumUpdates);
        }

        [TestMethod]
        [TestCategory("RefreshSpace")]
        public async Task RefreshSpace_CallsUpdateAllChildrenOnSpace_WhenArgIsSpaceViewModel()
        {
            var svm = new FakeSpaceViewModel(FakeCfSpace, Services);
            var appCollection = new DetailedResult<List<CloudFoundryApp>>();

            Assert.IsTrue(svm is SpaceViewModel);
            Assert.AreEqual(0, svm.NumUpdates);

            await _sut.RefreshSpace(svm);

            Assert.AreEqual(1, svm.NumUpdates);
        }

        [TestMethod]
        [TestCategory("RefreshAllItems")]
        public void RefreshAllItems_SetsIsRefreshingAllFalse_WhenTasConnectionNull()
        {
            _sut.IsRefreshingAll = true;

            Assert.IsNull(_sut.TasConnection);
            Assert.IsTrue(_sut.IsRefreshingAll);

            _sut.RefreshAllItems();

            Assert.IsFalse(_sut.IsRefreshingAll);
        }
        
        [TestMethod]
        [TestCategory("RefreshAllItems")]
        public void RefreshAllItems_SetsThreadServiceIsPollingFalse_WhenTasConnectionNull()
        {
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
            var fakeLogsResult = new DetailedResult<string>(content: null, succeeded: false, explanation: ":(", cmdDetails: FakeFailureCmdResult);

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
                DisplayErrorDialog(It.Is<string>(s => s.Contains(fakeApp.AppName)), It.Is<string>(s => s.Contains(fakeLogsResult.Explanation))),
                    Times.Once);
        }

        [TestMethod]
        [TestCategory("DisplayRecentAppLogs")]
        public async Task DisplayRecentAppLogs_CallsViewShowMethod_WhenLogsCmdSucceeds()
        {
            var fakeApp = new CloudFoundryApp("junk", "junk", null, "junk");
            var fakeView = new FakeOutputView();
            var fakeLogsResult = new DetailedResult<string>(content: "fake logs", succeeded: true, explanation: null, cmdDetails: FakeSuccessCmdResult);

            MockViewLocatorService.Setup(m => m.
                GetViewByViewModelName(nameof(OutputViewModel), null))
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
            var invalidRefreshTokenResult = new DetailedResult<string>(content: null, succeeded: false, explanation: ":(", cmdDetails: FakeFailureCmdResult)
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
                DisplayErrorDialog(It.Is<string>(s => s.Contains(fakeApp.AppName)), It.Is<string>(s => s.Contains(invalidRefreshTokenResult.Explanation))),
                    Times.Once);
        }

        [TestMethod]
        [TestCategory("AuthenticationRequired")]
        public void SettingAuthenticationRequiredToTrue_CollapsesCfInstanceViewModel()
        {
            _sut.TasConnection = new FakeCfInstanceViewModel(FakeCfInstance, Services, expanded: true);

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

            _sut.TasConnection = new FakeCfInstanceViewModel(FakeCfInstance, Services, expanded: false);

            Assert.IsTrue(_sut.AuthenticationRequired);
            Assert.IsFalse(_sut.TasConnection.IsExpanded);

            _sut.AuthenticationRequired = false;

            Assert.IsFalse(_sut.AuthenticationRequired);
            Assert.IsFalse(_sut.TasConnection.IsExpanded);
        }

        [TestMethod]
        [TestCategory("LogOutTas")]
        public void LogOutTas_SetsTasConnectionToNull_AndSetsIsLoggedInToFalse_AndClearsConnectionCache()
        {
            _sut.TasConnection = _fakeTasConnection;
            _sut.IsLoggedIn = true;

            Assert.IsNotNull(_sut.TasConnection);
            Assert.IsTrue(_sut.IsLoggedIn);
            _sut.LogOutTas(_sut.TasConnection);

            Assert.IsNull(_sut.TasConnection);
            Assert.IsFalse(_sut.IsLoggedIn);

            MockDataPersistenceService.Verify(m => m.ClearData(TasExplorerViewModel.ConnectionNameKey), Times.Once);
            MockDataPersistenceService.Verify(m => m.ClearData(TasExplorerViewModel.ConnectionAddressKey), Times.Once);
        }

        [TestMethod]
        [TestCategory("LogOutTas")]
        public void LogOutTas_ClearsCfAccessToken()
        {
            _sut.LogOutTas(_sut.TasConnection);

            MockCloudFoundryService.Verify(m => m.LogoutCfUser(), Times.Once);
        }

        [TestMethod]
        [TestCategory("SetConnection")]
        public void SetConnection_UpdatesTasConnection_WithNewCfInfo_WhenTasConnectionIsNull()
        {
            Assert.IsNull(_sut.TasConnection);

            _sut.SetConnection(FakeCfInstance);

            Assert.IsNotNull(_sut.TasConnection);
            Assert.AreEqual(FakeCfInstance, _sut.TasConnection.CloudFoundryInstance);
        }

        [TestMethod]
        [TestCategory("SetConnection")]
        public void SetConnection_SetsAuthenticationRequiredToFalse_WhenTasConnectionIsNull()
        {
            _sut.AuthenticationRequired = true;
            _receivedEvents.Clear();

            Assert.IsNull(_sut.TasConnection);
            Assert.IsTrue(_sut.AuthenticationRequired);

            Assert.AreEqual(0, _receivedEvents.Count);

            _sut.SetConnection(FakeCfInstance);

            Assert.IsFalse(_sut.AuthenticationRequired);
            CollectionAssert.Contains(_receivedEvents, "AuthenticationRequired");
        }

        [TestMethod]
        [TestCategory("SetConnection")]
        public void SetConnection_StartsBackgroundRefreshTask_WhenTasConnectionIsNull()
        {
            Assert.IsNull(_sut.TasConnection);

            _sut.SetConnection(FakeCfInstance);

            MockThreadingService.Verify(m => m.StartRecurrentUiTaskInBackground(_sut.RefreshAllItems, null, It.IsAny<int>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("SetConnection")]
        public void SetConnection_SavesConnectionName_WhenTasConnectionIsNull()
        {
            Assert.IsNull(_sut.TasConnection);

            _sut.SetConnection(FakeCfInstance);

            MockDataPersistenceService.Verify(m => m.WriteStringData(TasExplorerViewModel.ConnectionNameKey, FakeCfInstance.InstanceName), Times.Once);
        }

        [TestMethod]
        [TestCategory("SetConnection")]
        public void SetConnection_SavesConnectionApiAddress_WhenTasConnectionIsNull()
        {
            Assert.IsNull(_sut.TasConnection);

            _sut.SetConnection(FakeCfInstance);

            MockDataPersistenceService.Verify(m => m.WriteStringData(TasExplorerViewModel.ConnectionAddressKey, FakeCfInstance.ApiAddress), Times.Once);
        }

        [TestMethod]
        [TestCategory("SetConnection")]
        public void SetConnection_DoesNotChangeTasConnection_WhenTasConnectionIsNotNull()
        {
            _sut.TasConnection = new CfInstanceViewModel(FakeCfInstance, _sut, Services);

            Assert.IsNotNull(_sut.TasConnection);

            var initialCf = _sut.TasConnection.CloudFoundryInstance;

            _sut.SetConnection(FakeCfInstance);

            Assert.IsNotNull(_sut.TasConnection);
            Assert.AreEqual(initialCf, _sut.TasConnection.CloudFoundryInstance);
        }

        [TestMethod]
        [TestCategory("StreamAppLogs")]
        public async Task StreamAppLogs_ConstructsAndDisplaysNewOutputView()
        {
            var expectedViewParam = $"Logs for {FakeCfApp.AppName}"; // intended to be title of tool window
            var fakeView = new FakeOutputView();
            var fakeRecentLogsResult = new DetailedResult<string>
            {
                Succeeded = true,
                Content = "fake historical app logs",
            };

            MockViewLocatorService.Setup(m => m.GetViewByViewModelName(nameof(OutputViewModel), expectedViewParam)).Returns(fakeView);
            MockCloudFoundryService.Setup(m => m.GetRecentLogsAsync(FakeCfApp)).ReturnsAsync(fakeRecentLogsResult);

            await _sut.StreamAppLogsAsync(FakeCfApp);

            Assert.IsTrue(fakeView.ShowMethodWasCalled);
            MockViewLocatorService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("StreamAppLogs")]
        public async Task StreamAppLogs_PrintsRecentLogs_BeforeStartingStream()
        {
            var expectedViewParam = $"Logs for {FakeCfApp.AppName}";
            var fakeView = new FakeOutputView();
            var fakeViewModel = fakeView.ViewModel as IOutputViewModel;
            Action<string> expectedStdOutDelegate = fakeViewModel.AppendLine;
            Action<string> expectedStdErrDelegate = fakeViewModel.AppendLine;
            var fakeRecentLogsResult = new DetailedResult<string>
            {
                Succeeded = true,
                Content = "fake historical app logs",
            };

            MockViewLocatorService.Setup(m => m.GetViewByViewModelName(nameof(OutputViewModel), expectedViewParam)).Returns(fakeView);
            MockCloudFoundryService.Setup(m => m.GetRecentLogsAsync(FakeCfApp)).ReturnsAsync(fakeRecentLogsResult);

            await _sut.StreamAppLogsAsync(FakeCfApp);

            var vm = fakeViewModel as FakeOutputViewModel;
            Assert.IsTrue(vm.AppendLineInvocationArgs.Contains(fakeRecentLogsResult.Content));
            MockCloudFoundryService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("StreamAppLogs")]
        public async Task StreamAppLogs_LogsError_WhenRecentLogsRequestFails()
        {
            var expectedViewParam = $"Logs for {FakeCfApp.AppName}";
            var fakeView = new FakeOutputView();
            var fakeViewModel = fakeView.ViewModel as IOutputViewModel;
            Action<string> expectedStdOutDelegate = fakeViewModel.AppendLine;
            Action<string> expectedStdErrDelegate = fakeViewModel.AppendLine;
            var fakeRecentLogsResult = new DetailedResult<string>
            {
                Succeeded = false,
                Explanation = ":(",
            };

            MockViewLocatorService.Setup(m => m.GetViewByViewModelName(nameof(OutputViewModel), expectedViewParam)).Returns(fakeView);
            MockCloudFoundryService.Setup(m => m.GetRecentLogsAsync(FakeCfApp)).ReturnsAsync(fakeRecentLogsResult);

            await _sut.StreamAppLogsAsync(FakeCfApp);

            MockLogger.Verify(m => m.Error($"Unable to retrieve recent logs for {FakeCfApp.AppName}. {fakeRecentLogsResult.Explanation}. {fakeRecentLogsResult.CmdResult}"), Times.Once);
            MockCloudFoundryService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("StreamAppLogs")]
        public async Task StreamAppLogs_SetsAuthenticationRequiredToTrue_WhenRecentLogsRequestReportsInvalidRefreshToken()
        {
            var expectedViewParam = $"Logs for {FakeCfApp.AppName}";
            var fakeView = new FakeOutputView();
            var fakeViewModel = fakeView.ViewModel as IOutputViewModel;
            Action<string> expectedStdOutDelegate = fakeViewModel.AppendLine;
            Action<string> expectedStdErrDelegate = fakeViewModel.AppendLine;
            var fakeRecentLogsResult = new DetailedResult<string>
            {
                Succeeded = false,
                Explanation = ":(",
                FailureType = FailureType.InvalidRefreshToken,
            };

            MockViewLocatorService.Setup(m => m.GetViewByViewModelName(nameof(OutputViewModel), expectedViewParam)).Returns(fakeView);
            MockCloudFoundryService.Setup(m => m.GetRecentLogsAsync(FakeCfApp)).ReturnsAsync(fakeRecentLogsResult);

            Assert.IsFalse(_sut.AuthenticationRequired);

            await _sut.StreamAppLogsAsync(FakeCfApp);

            Assert.IsTrue(_sut.AuthenticationRequired);

            MockCloudFoundryService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("StreamAppLogs")]
        public async Task StreamAppLogs_RequestsLogStreamProcessFromCloudFoundryService()
        {
            var expectedViewParam = $"Logs for {FakeCfApp.AppName}";
            var fakeView = new FakeOutputView();
            var fakeViewModel = fakeView.ViewModel as IOutputViewModel;
            Action<string> expectedStdOutDelegate = fakeViewModel.AppendLine;
            Action<string> expectedStdErrDelegate = fakeViewModel.AppendLine;
            var fakeRecentLogsResult = new DetailedResult<string>
            {
                Succeeded = false,
                Explanation = ":(",
                FailureType = FailureType.InvalidRefreshToken,
            };
            var fakeLogStreamResult = new DetailedResult<Process>
            {
                Succeeded = true,
                Content = null, // ignoring content for this test
            };

            MockViewLocatorService.Setup(m => m.GetViewByViewModelName(nameof(OutputViewModel), expectedViewParam)).Returns(fakeView);
            MockCloudFoundryService.Setup(m => m.GetRecentLogsAsync(FakeCfApp)).ReturnsAsync(fakeRecentLogsResult);
            MockCloudFoundryService.Setup(m => m.StreamAppLogs(FakeCfApp, expectedStdOutDelegate, expectedStdErrDelegate)).Returns(fakeLogStreamResult);

            await _sut.StreamAppLogsAsync(FakeCfApp);

            MockCloudFoundryService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("StreamAppLogs")]
        public async Task StreamAppLogs_SavesLogStreamProcessToCorrectOutputViewModel_WhenCloudFoundryServiceSuccessfullyReturnsProcess()
        {
            using (var fakeLogsProcess = new Process())
            {
                var expectedViewParam = $"Logs for {FakeCfApp.AppName}"; // intended to be title of tool window
                var fakeView = new FakeOutputView();
                var fakeViewModel = fakeView.ViewModel as IOutputViewModel;
                Action<string> expectedStdOutDelegate = fakeViewModel.AppendLine;
                Action<string> expectedStdErrDelegate = fakeViewModel.AppendLine;
                var fakeRecentLogsResult = new DetailedResult<string>
                {
                    Succeeded = false,
                    Explanation = ":(",
                    FailureType = FailureType.InvalidRefreshToken,
                };
                var fakeLogStreamResult = new DetailedResult<Process>
                {
                    Succeeded = true,
                    Content = fakeLogsProcess,
                };

                MockViewLocatorService.Setup(m => m.GetViewByViewModelName(nameof(OutputViewModel), expectedViewParam)).Returns(fakeView);
                MockCloudFoundryService.Setup(m => m.GetRecentLogsAsync(FakeCfApp)).ReturnsAsync(fakeRecentLogsResult);
                MockCloudFoundryService.Setup(m => m.StreamAppLogs(FakeCfApp, expectedStdOutDelegate, expectedStdErrDelegate)).Returns(fakeLogStreamResult);

                await _sut.StreamAppLogsAsync(FakeCfApp);

                Assert.AreEqual(fakeLogsProcess, fakeViewModel.ActiveProcess);
            }
        }

        [TestMethod]
        [TestCategory("StreamAppLogs")]
        public async Task StreamAppLogs_DisplaysError_WhenCloudFoundryServiceFailsToReturnLogStreamProcess()
        {
            var expectedViewParam = $"Logs for {FakeCfApp.AppName}"; // intended to be title of tool window
            var fakeView = new FakeOutputView();
            var fakeViewModel = fakeView.ViewModel as IOutputViewModel;
            Action<string> expectedStdOutDelegate = fakeViewModel.AppendLine;
            Action<string> expectedStdErrDelegate = fakeViewModel.AppendLine;
            var fakeRecentLogsResult = new DetailedResult<string>
            {
                Succeeded = false,
                Explanation = ":(",
                FailureType = FailureType.InvalidRefreshToken,
            };
            var fakeLogStreamResult = new DetailedResult<Process>
            {
                Succeeded = false,
                Explanation = "junk",
            };

            MockViewLocatorService.Setup(m => m.GetViewByViewModelName(nameof(OutputViewModel), expectedViewParam)).Returns(fakeView);
            MockCloudFoundryService.Setup(m => m.GetRecentLogsAsync(FakeCfApp)).ReturnsAsync(fakeRecentLogsResult);
            MockCloudFoundryService.Setup(m => m.StreamAppLogs(FakeCfApp, expectedStdOutDelegate, expectedStdErrDelegate)).Returns(fakeLogStreamResult);
            MockErrorDialogService.Setup(m => m.DisplayErrorDialog("Error displaying app logs", $"Something went wrong while trying to display logs for {FakeCfApp.AppName}, please try again."));

            await _sut.StreamAppLogsAsync(FakeCfApp);

            MockErrorDialogService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("StreamAppLogs")]
        public async Task StreamAppLogs_DisplaysError_ViewLocatorServiceThrowsException()
        {
            var expectedViewParam = $"Logs for {FakeCfApp.AppName}"; // intended to be title of tool window
            var fakeView = new FakeOutputView();
            var fakeViewModel = fakeView.ViewModel as IOutputViewModel;
            Action<string> expectedStdOutDelegate = fakeViewModel.AppendLine;
            Action<string> expectedStdErrDelegate = fakeViewModel.AppendLine;
            var fakeViewLocatorException = new Exception(":(");

            MockViewLocatorService.Setup(m => m.GetViewByViewModelName(nameof(OutputViewModel), expectedViewParam)).Throws(fakeViewLocatorException);
            MockErrorDialogService.Setup(m => m.DisplayErrorDialog("Error displaying app logs", $"Something went wrong while trying to display logs for {FakeCfApp.AppName}, please try again."));

            await _sut.StreamAppLogsAsync(FakeCfApp);

            MockErrorDialogService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("StreamAppLogs")]
        public async Task StreamAppLogs_DisplaysError_WhenCloudFoundryServiceThrowsException()
        {
            var expectedViewParam = $"Logs for {FakeCfApp.AppName}"; // intended to be title of tool window
            var fakeView = new FakeOutputView();
            var fakeViewModel = fakeView.ViewModel as IOutputViewModel;
            Action<string> expectedStdOutDelegate = fakeViewModel.AppendLine;
            Action<string> expectedStdErrDelegate = fakeViewModel.AppendLine;
            var fakeRecentLogsResult = new DetailedResult<string>
            {
                Succeeded = false,
                Explanation = ":(",
                FailureType = FailureType.InvalidRefreshToken,
            };
            var fakeLogStreamProcessStartException = new Exception(":(");

            MockViewLocatorService.Setup(m => m.GetViewByViewModelName(nameof(OutputViewModel), expectedViewParam)).Returns(fakeView);
            MockCloudFoundryService.Setup(m => m.GetRecentLogsAsync(FakeCfApp)).ReturnsAsync(fakeRecentLogsResult);
            MockCloudFoundryService.Setup(m => m.StreamAppLogs(FakeCfApp, expectedStdOutDelegate, expectedStdErrDelegate)).Throws(fakeLogStreamProcessStartException);
            MockErrorDialogService.Setup(m => m.DisplayErrorDialog("Error displaying app logs", $"Something went wrong while trying to display logs for {FakeCfApp.AppName}, please try again."));

            await _sut.StreamAppLogsAsync(FakeCfApp);

            MockErrorDialogService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("StreamAppLogs")]
        public async Task StreamAppLogs_SetsAuthenticationRequiredToTrue_WhenLogsRequestFailsDueToInvalidRefreshToken()
        {
            var expectedViewParam = $"Logs for {FakeCfApp.AppName}";
            var fakeView = new FakeOutputView();
            var fakeViewModel = fakeView.ViewModel as IOutputViewModel;
            Action<string> expectedStdOutDelegate = fakeViewModel.AppendLine;
            Action<string> expectedStdErrDelegate = fakeViewModel.AppendLine;
            var fakeRecentLogsResult = new DetailedResult<string>
            {
                Succeeded = true,
                Content = "junk",
            };
            var fakeLogStreamResult = new DetailedResult<Process>
            {
                Succeeded = false,
                Content = null,
                FailureType = FailureType.InvalidRefreshToken,
            };

            MockViewLocatorService.Setup(m => m.GetViewByViewModelName(nameof(OutputViewModel), expectedViewParam)).Returns(fakeView);
            MockCloudFoundryService.Setup(m => m.GetRecentLogsAsync(FakeCfApp)).ReturnsAsync(fakeRecentLogsResult);
            MockCloudFoundryService.Setup(m => m.StreamAppLogs(FakeCfApp, expectedStdOutDelegate, expectedStdErrDelegate)).Returns(fakeLogStreamResult);
            MockErrorDialogService.Setup(m => m.DisplayErrorDialog(
                It.Is<string>(s => s.Contains("Error displaying app logs")),
                It.Is<string>(s => s.Contains(FakeCfApp.AppName))))
                .Verifiable();

            Assert.IsFalse(_sut.AuthenticationRequired);
            
            await _sut.StreamAppLogsAsync(FakeCfApp);

            Assert.IsTrue(_sut.AuthenticationRequired);
            MockErrorDialogService.VerifyAll();
        }
    }
}
