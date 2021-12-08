﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
                RunOnUiThread(It.IsAny<Action>()))
                    .Callback<Action>(action =>
                    {
                        action();
                    });

            // set up mock threading service to run whatever method is passed
            // to StartTask & wait for it to finish instead of letting it go
            MockThreadingService.Setup(mock => mock.
                StartTask(It.IsAny<Func<Task>>()))
                    .Callback<Func<Task>>(async method =>
                    {
                        await Task.Run(method);
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
        public async Task StopCfApp_CallsStopCfAppAsync()
        {
            var fakeApp = new CloudFoundryApp("junk", "junk", parentSpace: null, null);

            MockCloudFoundryService.Setup(mock => mock.StopAppAsync(fakeApp, true, It.IsAny<int>())).ReturnsAsync(FakeSuccessDetailedResult);

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
        public async Task StopCfApp_DisplaysErrorDialog_WhenStopAppAsyncFails()
        {
            var fakeApp = new CloudFoundryApp("junk", "junk", parentSpace: null, null);

            MockCloudFoundryService.Setup(mock => mock.
                StopAppAsync(fakeApp, true, It.IsAny<int>()))
                    .ReturnsAsync(FakeFailureDetailedResult);

            await _sut.StopCfApp(fakeApp);

            var expectedErrorTitle = $"{TasExplorerViewModel._stopAppErrorMsg} {fakeApp.AppName}.";
            var expectedErrorMsg = FakeFailureDetailedResult.Explanation;

            MockErrorDialogService.Verify(mock => mock.
              DisplayErrorDialog(expectedErrorTitle, expectedErrorMsg),
                Times.Once);
        }

        [TestMethod]
        public async Task StopCfApp_LogsError_WhenStopAppAsyncFails()
        {
            var fakeApp = new CloudFoundryApp("junk", "junk", parentSpace: null, null);

            MockCloudFoundryService.Setup(mock => mock.
                StopAppAsync(fakeApp, true, It.IsAny<int>()))
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
        public async Task StartCfApp_CallsStartAppAsync()
        {
            var fakeApp = new CloudFoundryApp("junk", "junk", parentSpace: null, null);

            MockCloudFoundryService.Setup(mock => mock.StartAppAsync(fakeApp, true, It.IsAny<int>())).ReturnsAsync(FakeSuccessDetailedResult);

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
        public async Task StartCfApp_DisplaysErrorDialog_WhenStartAppAsyncFails()
        {
            var fakeApp = new CloudFoundryApp("junk", "junk", parentSpace: null, null);

            MockCloudFoundryService.Setup(mock => mock.
                StartAppAsync(fakeApp, true, It.IsAny<int>()))
                    .ReturnsAsync(FakeFailureDetailedResult);

            await _sut.StartCfApp(fakeApp);

            var expectedErrorTitle = $"{TasExplorerViewModel._startAppErrorMsg} {fakeApp.AppName}.";
            var expectedErrorMsg = FakeFailureDetailedResult.Explanation;

            MockErrorDialogService.Verify(mock => mock.
              DisplayErrorDialog(expectedErrorTitle, expectedErrorMsg),
                Times.Once);
        }

        [TestMethod]
        public async Task StartCfApp_LogsError_WhenStartAppAsyncFails()
        {
            var fakeApp = new CloudFoundryApp("junk", "junk", parentSpace: null, null);

            MockCloudFoundryService.Setup(mock => mock.
                StartAppAsync(fakeApp, true, It.IsAny<int>()))
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
        public async Task RefreshOrg_CallsRefreshChildren_WhenArgIsOrgViewModel()
        {
            var ovm = new FakeOrgViewModel(FakeCfOrg, Services);
            var spacesCollection = new DetailedResult<List<CloudFoundrySpace>>();

            MockCloudFoundryService.Setup(mock => mock.
                GetSpacesForOrgAsync(FakeCfOrg, true, It.IsAny<int>()))
                    .ReturnsAsync(spacesCollection);

            Assert.IsTrue(ovm is OrgViewModel);
            Assert.AreEqual(0, ovm.NumRefreshes);

            await _sut.RefreshOrg(ovm);

            Assert.AreEqual(1, ovm.NumRefreshes);
        }

        [TestMethod]
        [TestCategory("RefreshSpace")]
        public async Task RefreshSpace_CallsRefreshChildren_WhenArgIsSpaceViewModel()
        {
            var svm = new FakeSpaceViewModel(FakeCfSpace, Services);
            var appCollection = new DetailedResult<List<CloudFoundryApp>>();

            MockCloudFoundryService.Setup(mock => mock.
                GetAppsForSpaceAsync(FakeCfSpace, true, It.IsAny<int>()))
                    .ReturnsAsync(appCollection);

            Assert.IsTrue(svm is SpaceViewModel);
            Assert.AreEqual(0, svm.NumRefreshes);

            await _sut.RefreshSpace(svm);

            Assert.AreEqual(1, svm.NumRefreshes);
        }

        [TestMethod]
        public void RefreshAllItems_StartsFullRefreshTask()
        {
            _sut.RefreshAllItems(null);
            MockThreadingService.Verify(m => m.StartTask(_sut.InitiateFullRefresh), Times.Once);
        }

        [TestMethod]
        public void RefreshAllItems_DoesNotStartRefreshTask_WhenRefreshIsInProgress()
        {
            _sut.IsRefreshingAll = true;

            _sut.RefreshAllItems(null);
            MockThreadingService.Verify(m => m.StartTask(_sut.InitiateFullRefresh), Times.Never);
        }

        [TestMethod]
        public async Task InitiateFullRefresh_RefreshesEachExpandedTreeViewItemViewModel()
        {
            /** INTENTION:
             * TasExplorerViewModel starts off with 1 (expanded) cloudFoundryInstanceViewModel "cfivm",
             * containing 1 (expanded) orgViewModel "ovm", which contains 1 (expanded) spaceViewModel "svm",
             * which contains 1 appViewModel "avm".
             */
            var fakeInitialOrg = new CloudFoundryOrganization("fake org name", "fake org id", FakeCfInstance);
            var fakeInitialSpace = new CloudFoundrySpace("fake space name", "fake space id", fakeInitialOrg);
            var fakeInitialApp = new CloudFoundryApp("fake app name", "fake app id", fakeInitialSpace, "state1");

            /** These view models are fakes; they inherit from their respective tvivms but override `RefreshChildren`
             * so that these tests are able to check how many times that method gets called (calling RefreshChildren
             * on these fakes increments `NumRefreshes` by 1). These view models are constructed with expanded = true
             * to make them eligible for refreshing.
             */
            var cfivm = new FakeCfInstanceViewModel(FakeCfInstance, Services, expanded: true);
            var ovm = new FakeOrgViewModel(fakeInitialOrg, Services, expanded: true);
            var svm = new FakeSpaceViewModel(fakeInitialSpace, Services, expanded: true);
            var avm = new AppViewModel(fakeInitialApp, Services);

            cfivm.Children = new ObservableCollection<TreeViewItemViewModel> { ovm };
            ovm.Children = new ObservableCollection<TreeViewItemViewModel> { svm };
            svm.Children = new ObservableCollection<TreeViewItemViewModel> { avm };
            _sut.TasConnection = cfivm;

            /** INTENTION:
             * Mocks should simulate TasExplorerViewModel.InitiateFullRefresh adding 1 org, 
             * 1 space & 1 app to each of the respective TreeViewItemViewModels above. In addition,
             * the initial app that existed prior to the refresh should have its state changed by 
             * the refresh.
             */
            var fakeNewOrg = new CloudFoundryOrganization("new org", "new org id", FakeCfInstance);
            var fakeNewSpace = new CloudFoundrySpace("new space", "new space id", fakeInitialOrg);
            var fakeUpdatedApp = new CloudFoundryApp(fakeInitialApp.AppName, fakeInitialApp.AppId, fakeInitialApp.ParentSpace, "new state");
            var fakeNewApp = new CloudFoundryApp("new app", "new app id", fakeInitialSpace, "junk state");

            // simulate addition of new Org
            MockCloudFoundryService.Setup(mock => mock.
                GetOrgsForCfInstanceAsync(FakeCfInstance, true, 1))
                    .ReturnsAsync(new DetailedResult<List<CloudFoundryOrganization>>(
                        succeeded: true,
                        content: new List<CloudFoundryOrganization>
                        {
                            fakeInitialOrg,
                            fakeNewOrg,
                        },
                        explanation: null,
                        cmdDetails: FakeSuccessCmdResult));

            // simulate addition of new Space
            MockCloudFoundryService.Setup(mock => mock.
                GetSpacesForOrgAsync(fakeInitialOrg, true, It.IsAny<int>()))
                    .ReturnsAsync(new DetailedResult<List<CloudFoundrySpace>>(
                succeeded: true,
                content: new List<CloudFoundrySpace>
                {
                    fakeInitialSpace,
                    fakeNewSpace,
                },
                explanation: null,
                cmdDetails: FakeSuccessCmdResult));

            // simulate addition of new App + change of state for initial app
            MockCloudFoundryService.Setup(mock => mock.
                GetAppsForSpaceAsync(fakeInitialSpace, true, It.IsAny<int>()))
                    .ReturnsAsync(new DetailedResult<List<CloudFoundryApp>>(
                succeeded: true,
                content: new List<CloudFoundryApp>
                {
                    fakeUpdatedApp,
                    fakeNewApp,
                },
                explanation: null,
                cmdDetails: FakeSuccessCmdResult));

            await _sut.InitiateFullRefresh();

            // ensure 1 thread was started per cf/org/space (not app; app refresh performed by space.RefreshChildren)
            Assert.AreEqual(3, MockThreadingService.Invocations.Count);

            // ensure RefreshChildren() was called once per cf/org/space
            Assert.AreEqual(1, cfivm.NumRefreshes);
            Assert.AreEqual(1, ovm.NumRefreshes);
            Assert.AreEqual(1, svm.NumRefreshes); // in the real implemenation of spaceViewModel.RefreshChildren, all apps should be refreshed

            // ensure refresh didn't change cfivm
            Assert.IsNotNull(_sut.TasConnection);
            Assert.AreEqual(cfivm, _sut.TasConnection);

            // ensure cf refresh added second org vm
            OrgViewModel firstOrgVm = (OrgViewModel)cfivm.Children[0];
            OrgViewModel secondOrgVm = (OrgViewModel)cfivm.Children[1];
            Assert.AreEqual(ovm, firstOrgVm);
            Assert.AreEqual(fakeInitialOrg, firstOrgVm.Org);
            Assert.AreEqual(fakeNewOrg, secondOrgVm.Org);

            // ensure org refresh added second space vm
            Assert.AreEqual(2, firstOrgVm.Children.Count);
            SpaceViewModel firstSpaceVm = (SpaceViewModel)firstOrgVm.Children[0];
            SpaceViewModel secondSpaceVm = (SpaceViewModel)firstOrgVm.Children[1];
            Assert.AreEqual(svm, firstSpaceVm);
            Assert.AreEqual(fakeInitialSpace, firstSpaceVm.Space);
            Assert.AreEqual(fakeNewSpace, secondSpaceVm.Space);

            // ensure space refresh added second app vm
            Assert.AreEqual(2, firstSpaceVm.Children.Count);
            AppViewModel firstAppVm = (AppViewModel)firstSpaceVm.Children[0];
            AppViewModel secondAppVm = (AppViewModel)firstSpaceVm.Children[1];
            Assert.AreEqual(fakeInitialApp.AppId, firstAppVm.App.AppId);
            Assert.AreNotEqual(fakeInitialApp.State, firstAppVm.App.State);
            Assert.AreEqual(fakeNewApp.AppId, secondAppVm.App.AppId);

            // ensure space refresh makes request for fresh apps
            MockCloudFoundryService.Verify(mock => mock.GetAppsForSpaceAsync(fakeInitialSpace, true, It.IsAny<int>()), Times.Once);

            // No need to get children for orgs that were just added by refresh.
            MockCloudFoundryService.Verify(mock => mock.
                GetSpacesForOrgAsync(fakeNewOrg, true, It.IsAny<int>()), Times.Never);

            // No need to get children for spaces that were just added by refresh.
            MockCloudFoundryService.Verify(mock => mock.
                GetAppsForSpaceAsync(fakeNewSpace, true, It.IsAny<int>()), Times.Never);
        }

        [TestMethod]
        public async Task InitiateFullRefresh_DoesNotAttemptToRefreshPlaceholderItems()
        {
            /** INTENTION:
             * TasExplorerViewModel starts off with 1 (expanded) cloudFoundryInstanceViewModel "cfivm",
             * containing 1 placeholder view model. No attempts should be made to refresh the placeholder.
             */
            var cfivm = new FakeCfInstanceViewModel(FakeCfInstance, Services, expanded: true);
            cfivm.Children = new ObservableCollection<TreeViewItemViewModel>
            {
                new FakePlaceholderViewModel(cfivm, Services),
            };

            MockCloudFoundryService.Setup(mock => mock.
                GetOrgsForCfInstanceAsync(FakeCfInstance, true, 1))
                    .ReturnsAsync(new DetailedResult<List<CloudFoundryOrganization>>(
                        succeeded: true,
                        content: new List<CloudFoundryOrganization>
                        {
                        },
                        explanation: null,
                        cmdDetails: FakeSuccessCmdResult));

            _sut.TasConnection = cfivm;

            // pre-check: tas explorer has a cf view model & it's expanded
            Assert.IsNotNull(_sut.TasConnection);
            Assert.IsTrue(_sut.TasConnection.IsExpanded);

            // pre-check: cf view model has 1 child & it's a placeholder 
            Assert.AreEqual(1, _sut.TasConnection.Children.Count);
            Assert.AreEqual(typeof(FakePlaceholderViewModel), _sut.TasConnection.Children[0].GetType());

            await _sut.InitiateFullRefresh();

            // ensure 1 thread is started to refresh the cf view model
            Assert.AreEqual(1, MockThreadingService.Invocations.Count);

            // ensure RefreshChildren() was called for cfivm but not its placeholder
            Assert.AreEqual(1, cfivm.NumRefreshes);
            var placeholderChild = (FakePlaceholderViewModel)cfivm.Children[0];
            Assert.AreEqual(0, placeholderChild.NumRefreshes);
        }

        [TestMethod]
        public async Task InitiateFullRefresh_DoesNotAttemptToRefreshCollapsedItems()
        {
            /** INTENTION:
             * TasExplorerViewModel starts off with 1 (expanded) cloudFoundryInstanceViewModel "cfivm",
             * containing 1 (collapsed) orgViewModel "ovm" which should not itself be refreshed.
             */
            var fakeInitialOrg = new CloudFoundryOrganization("fake org name", "fake org id", FakeCfInstance);
            var cfivm = new FakeCfInstanceViewModel(FakeCfInstance, Services, expanded: true);
            var ovm = new FakeOrgViewModel(fakeInitialOrg, Services, expanded: false);

            MockCloudFoundryService.Setup(mock => mock.
                GetOrgsForCfInstanceAsync(FakeCfInstance, true, 1))
                    .ReturnsAsync(new DetailedResult<List<CloudFoundryOrganization>>(
                        succeeded: true,
                        content: new List<CloudFoundryOrganization>
                        {
                            fakeInitialOrg,
                        },
                        explanation: null,
                        cmdDetails: FakeSuccessCmdResult));

            _sut.TasConnection = cfivm;

            // pre-check: tas explorer has a cf view model & it's expanded
            Assert.IsNotNull(_sut.TasConnection);
            Assert.IsTrue(_sut.TasConnection.IsExpanded);

            // pre-check: cf view model has 1 org child & it's collapsed
            Assert.AreEqual(1, _sut.TasConnection.Children.Count);
            Assert.IsFalse(_sut.TasConnection.Children[0].IsExpanded);

            await _sut.InitiateFullRefresh();

            // ensure 1 thread is started to refresh the cf view model
            Assert.AreEqual(1, MockThreadingService.Invocations.Count);

            // ensure RefreshChildren() was called once for cfivm
            Assert.AreEqual(1, cfivm.NumRefreshes);
            Assert.AreEqual(0, ovm.NumRefreshes);
        }

        [TestMethod]
        public async Task InitiateFullRefresh_DoesNotAttemptToRefreshLoadingCFs()
        {
            /** INTENTION:
             * TasExplorerViewModel starts off with 1 expanded but *loading* 
             * cloudFoundryInstanceViewModel "cfivm" which has a loading placeholder
             * as its only child. cfivm should not be refreshed (defer to the loading
             * task in progress).
             */
            var cfivm = new FakeCfInstanceViewModel(FakeCfInstance, Services, expanded: true)
            {
                IsLoading = true,
            };

            cfivm.Children = new ObservableCollection<TreeViewItemViewModel>
            {
                cfivm.LoadingPlaceholder,
            };

            _sut.TasConnection = cfivm;

            // pre-check: tas explorer has a cf view model & it's loading
            Assert.IsNotNull(_sut.TasConnection);
            Assert.IsTrue(_sut.TasConnection.IsExpanded);
            Assert.IsTrue(_sut.TasConnection.IsLoading);
            Assert.AreEqual(1, _sut.TasConnection.Children.Count);
            Assert.AreEqual(typeof(PlaceholderViewModel), _sut.TasConnection.Children[0].GetType());
            Assert.AreEqual(CfInstanceViewModel._loadingMsg, _sut.TasConnection.Children[0].DisplayText);

            await _sut.InitiateFullRefresh();

            // ensure no threads are started to refresh the cf view model
            Assert.AreEqual(0, MockThreadingService.Invocations.Count);

            // ensure RefreshChildren() was never called for cfivm
            Assert.AreEqual(0, cfivm.NumRefreshes);
        }

        [TestMethod]
        public async Task InitiateFullRefresh_DoesNotAttemptToRefreshLoadingOrgs()
        {
            /** INTENTION:
             * TasExplorerViewModel starts off with 1 expanded cloudFoundryInstanceViewModel 
             * "cfivm" which has 1 expanded but *loading* org child "ovm". cfivm should be 
             * refreshed but the loading ovm should not be (defer to the loading task in progress).
             */
            var fakeInitialOrg = new CloudFoundryOrganization("fake org name", "fake org id", FakeCfInstance);
            var cfivm = new FakeCfInstanceViewModel(FakeCfInstance, Services, expanded: true);
            var ovm = new FakeOrgViewModel(fakeInitialOrg, Services, expanded: true)
            {
                IsLoading = true
            };

            cfivm.Children = new ObservableCollection<TreeViewItemViewModel>
            {
                ovm,
            };

            MockCloudFoundryService.Setup(mock => mock.
                GetOrgsForCfInstanceAsync(FakeCfInstance, true, 1))
                    .ReturnsAsync(new DetailedResult<List<CloudFoundryOrganization>>(
                        succeeded: true,
                        content: new List<CloudFoundryOrganization>
                        {
                            fakeInitialOrg,
                        },
                        explanation: null,
                        cmdDetails: FakeSuccessCmdResult));

            _sut.TasConnection = cfivm;

            // pre-check: tas explorer has a cf view model & it's expanded
            Assert.IsNotNull(_sut.TasConnection);
            Assert.IsTrue(_sut.TasConnection.IsExpanded);

            // pre-check: cfivm has 1 child & it's loading
            Assert.AreEqual(1, _sut.TasConnection.Children.Count);
            var orgChild = _sut.TasConnection.Children[0];
            Assert.IsTrue(orgChild.IsLoading);

            await _sut.InitiateFullRefresh();

            // ensure just 1 thread is started to refresh the cf view model
            Assert.AreEqual(1, MockThreadingService.Invocations.Count);

            // ensure RefreshChildren() was called once for cfivm
            Assert.AreEqual(1, cfivm.NumRefreshes);

            // ensure RefreshChildren() was never called for ovm
            Assert.AreEqual(0, ovm.NumRefreshes);
        }

        [TestMethod]
        public async Task InitiateFullRefresh_DoesNotAttemptToRefreshLoadingSpaces()
        {
            /** INTENTION:
             * TasExplorerViewModel starts off with 1 expanded cloudFoundryInstanceViewModel 
             * "cfivm" which has 1 expanded org child "ovm". ovm has 1 expanded but *loading*
             * space child "svm". cfivm & ovm should be refreshed but the loading svm should 
             * not be refreshed (defer to the loading task in progress).
             */
            var fakeInitialOrg = new CloudFoundryOrganization("fake org name", "fake org id", FakeCfInstance);
            var fakeInitialSpace = new CloudFoundrySpace("fake space name", "fake space id", fakeInitialOrg);
            var cfivm = new FakeCfInstanceViewModel(FakeCfInstance, Services, expanded: true);
            var ovm = new FakeOrgViewModel(fakeInitialOrg, Services, expanded: true);
            var svm = new FakeSpaceViewModel(fakeInitialSpace, Services, expanded: true)
            {
                IsLoading = true,
            };

            cfivm.Children = new ObservableCollection<TreeViewItemViewModel>
            {
                ovm,
            };

            ovm.Children = new ObservableCollection<TreeViewItemViewModel>
            {
                svm,
            };

            MockCloudFoundryService.Setup(mock => mock.
                GetOrgsForCfInstanceAsync(FakeCfInstance, true, 1))
                    .ReturnsAsync(new DetailedResult<List<CloudFoundryOrganization>>(
                        succeeded: true,
                        content: new List<CloudFoundryOrganization>
                        {
                            fakeInitialOrg,
                        },
                        explanation: null,
                        cmdDetails: FakeSuccessCmdResult));

            MockCloudFoundryService.Setup(mock => mock.
                GetSpacesForOrgAsync(fakeInitialOrg, true, It.IsAny<int>()))
                    .ReturnsAsync(new DetailedResult<List<CloudFoundrySpace>>(
                        succeeded: true,
                        content: new List<CloudFoundrySpace>
                        {
                            fakeInitialSpace,
                        },
                        explanation: null,
                        cmdDetails: FakeSuccessCmdResult));

            _sut.TasConnection = cfivm;

            // pre-check: tas explorer has a cf view model & it's expanded
            Assert.IsNotNull(_sut.TasConnection);
            var cf = _sut.TasConnection;
            Assert.IsTrue(cf.IsExpanded);

            // pre-check: cfivm has 1 org view model & it's expanded
            Assert.AreEqual(1, cf.Children.Count);
            var org = cf.Children[0];
            Assert.IsTrue(org.IsExpanded);

            // pre-check: ovm has 1 child & it's loading
            Assert.AreEqual(1, org.Children.Count);
            var space = org.Children[0];
            Assert.IsTrue(space.IsExpanded);
            Assert.IsTrue(space.IsLoading);

            await _sut.InitiateFullRefresh();

            // ensure just 2 threads are started: 1 to refresh cf, 1 to refresh org
            Assert.AreEqual(2, MockThreadingService.Invocations.Count);

            // ensure RefreshChildren() was called once for cfivm
            Assert.AreEqual(1, cfivm.NumRefreshes);

            // ensure RefreshChildren() was called once for ovm
            Assert.AreEqual(1, ovm.NumRefreshes);

            // ensure RefreshChildren() was never called for svm
            Assert.AreEqual(0, svm.NumRefreshes);
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
                NavigateTo(nameof(OutputViewModel), null))
                    .Callback(() => Assert.Fail("Output view does not need to be retrieved."));

            MockCloudFoundryService.Setup(m => m.
                GetRecentLogs(fakeApp))
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
                NavigateTo(nameof(OutputViewModel), null))
                    .Returns(fakeView);

            MockCloudFoundryService.Setup(m => m.
                GetRecentLogs(fakeApp))
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
                NavigateTo(nameof(OutputViewModel), null))
                    .Callback(() => Assert.Fail("Output view does not need to be retrieved."));

            MockCloudFoundryService.Setup(m => m.
                GetRecentLogs(fakeApp))
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

            MockThreadingService.Verify(m => m.StartUiBackgroundPoller(_sut.RefreshAllItems, null, It.IsAny<int>()), Times.Once);
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
    }

    internal class FakeCfInstanceViewModel : CfInstanceViewModel
    {
        private int _numRefreshes = 0;

        internal FakeCfInstanceViewModel(CloudFoundryInstance cloudFoundryInstance, IServiceProvider services, bool expanded = false)
            : base(cloudFoundryInstance, null, services, expanded)
        {
        }

        internal int NumRefreshes { get => _numRefreshes; private set => _numRefreshes = value; }

        public override async Task RefreshChildren()
        {
            _numRefreshes += 1;
            await base.RefreshChildren();
        }
    }

    internal class FakeOrgViewModel : OrgViewModel
    {
        private int _numRefreshes = 0;

        internal FakeOrgViewModel(CloudFoundryOrganization org, IServiceProvider services, bool expanded = false)
            : base(org, null, null, services, expanded)
        {
        }

        internal int NumRefreshes { get => _numRefreshes; private set => _numRefreshes = value; }

        public override async Task RefreshChildren()
        {
            _numRefreshes += 1;
            await base.RefreshChildren();
        }
    }

    internal class FakeSpaceViewModel : SpaceViewModel
    {
        private int _numRefreshes = 0;

        internal FakeSpaceViewModel(CloudFoundrySpace space, IServiceProvider services, bool expanded = false)
            : base(space, null, null, services, expanded)
        {
        }

        internal int NumRefreshes { get => _numRefreshes; private set => _numRefreshes = value; }

        public override async Task RefreshChildren()
        {
            _numRefreshes += 1;
            await base.RefreshChildren();
        }
    }

    internal class FakePlaceholderViewModel : PlaceholderViewModel
    {
        private int _numRefreshes = 0;

        internal FakePlaceholderViewModel(TreeViewItemViewModel parent, IServiceProvider services)
            : base(parent, services)
        {
        }

        internal int NumRefreshes { get => _numRefreshes; private set => _numRefreshes = value; }

        public override async Task RefreshChildren()
        {
            _numRefreshes += 1;
            await base.RefreshChildren();
        }
    }
}
