using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Tanzu.Toolkit.Models;
using Tanzu.Toolkit.Services;

namespace Tanzu.Toolkit.ViewModels.Tests
{
    [TestClass]
    public class TasExplorerViewModelTests : ViewModelTestSupport
    {
        private TasExplorerViewModel _sut;
        private List<string> _receivedEvents;

        [TestInitialize]
        public void TestInit()
        {
            RenewMockServices();

            MockCloudFoundryService.SetupGet(mock => mock.CloudFoundryInstances).Returns(new Dictionary<string, CloudFoundryInstance>());

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
        }

        [TestCleanup]
        public void TestCleanup()
        {
            MockCloudFoundryService.VerifyAll();
        }

        [TestMethod]
        public void CanOpenLoginView_ReturnsExpected()
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
        public void CanDeleteCfApp_ReturnsTrue()
        {
            Assert.IsTrue(_sut.CanDeleteCfApp(null));
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
        public void OpenLoginView_CallsDialogService_ShowDialog()
        {
            _sut.OpenLoginView(null);
            MockDialogService.Verify(ds => ds.ShowDialog(typeof(LoginViewModel).Name, null), Times.Once);
        }

        [TestMethod]
        [TestCategory("OpenLoginView")]
        public void OpenLoginView_UpdatesCloudFoundryInstances_AfterDialogCloses()
        {
            var emptyCfsDict = new Dictionary<string, CloudFoundryInstance>();
            var fakeCfsDict = new Dictionary<string, CloudFoundryInstance>
            {
                { "fake cf", new CloudFoundryInstance("fake cf", null) },
            };

            MockCloudFoundryService.SetupSequence(mock => mock.CloudFoundryInstances)
                .Returns(emptyCfsDict) // return empty on first request to avoid error due to temporary "single-cf" requirement.
                .Returns(fakeCfsDict); // return fake cf on second request as mock result of having logged in.

            Assert.AreEqual(0, _sut.CloudFoundryList.Count);

            _sut.OpenLoginView(null);

            Assert.IsTrue(_sut.HasCloudTargets);
            Assert.AreEqual(1, _sut.CloudFoundryList.Count);
        }

        [TestMethod]
        [TestCategory("OpenLoginView")]
        public void OpenLoginView_SetsAuthenticationRequiredToFalse_WhenCFGetsAdded()
        {
            _sut.AuthenticationRequired = true;

            MockDialogService.Setup(mock => mock.
                ShowDialog(typeof(LoginViewModel).Name, null))
                    .Callback(() =>
                    {
                        // Simulate successful login by mocking CloudFoundryService to return 1 CF instance

                        var fakeCfsDict = new Dictionary<string, CloudFoundryInstance>
                        {
                            { "fake cf", new CloudFoundryInstance("fake cf", null) },
                        };

                        MockCloudFoundryService.SetupGet(mock => mock.
                          CloudFoundryInstances).Returns(fakeCfsDict);
                    });

            Assert.IsTrue(_sut.AuthenticationRequired);
            Assert.AreEqual(0, _sut.CloudFoundryList.Count);

            _sut.OpenLoginView(null);

            Assert.AreEqual(1, _sut.CloudFoundryList.Count);
            Assert.IsFalse(_sut.AuthenticationRequired);
        }

        [TestMethod]
        [TestCategory("OpenLoginView")]
        public void OpenLoginView_DoesNotChangeAuthenticationRequired_WhenNoCFIsAdded()
        {
            _sut.AuthenticationRequired = true;

            MockDialogService.Setup(mock => mock.
                ShowDialog(typeof(LoginViewModel).Name, null))
                    .Callback(() =>
                    {
                        // Simulate unsuccessful login by mocking CloudFoundryService to return 0 CF instances

                        var emptyCfsDict = new Dictionary<string, CloudFoundryInstance>();

                        MockCloudFoundryService.SetupGet(mock => mock.
                          CloudFoundryInstances).Returns(emptyCfsDict);
                    });

            Assert.IsTrue(_sut.AuthenticationRequired);
            Assert.AreEqual(0, _sut.CloudFoundryList.Count);

            _sut.OpenLoginView(null);

            Assert.AreEqual(0, _sut.CloudFoundryList.Count);
            Assert.IsTrue(_sut.AuthenticationRequired); // this prop should not have changed
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
        public async Task DeleteCfApp_CallsDeleteAppAsync()
        {
            var fakeApp = new CloudFoundryApp("junk", "junk", parentSpace: null, null);

            MockCloudFoundryService.Setup(mock => mock.DeleteAppAsync(fakeApp, true, true, It.IsAny<int>())).ReturnsAsync(FakeSuccessDetailedResult);

            Exception shouldStayNull = null;
            try
            {
                await _sut.DeleteCfApp(fakeApp);
            }
            catch (Exception e)
            {
                shouldStayNull = e;
            }

            Assert.IsNull(shouldStayNull);
            MockCloudFoundryService.VerifyAll();
        }

        [TestMethod]
        public async Task DeleteCfApp_DisplaysErrorDialog_WhenDeleteAppAsyncFails()
        {
            var fakeApp = new CloudFoundryApp("junk", "junk", parentSpace: null, null);

            MockCloudFoundryService.Setup(mock => mock.
                DeleteAppAsync(fakeApp, true, true, It.IsAny<int>()))
                    .ReturnsAsync(FakeFailureDetailedResult);

            await _sut.DeleteCfApp(fakeApp);

            var expectedErrorTitle = $"{TasExplorerViewModel._deleteAppErrorMsg} {fakeApp.AppName}.";
            var expectedErrorMsg = FakeFailureDetailedResult.Explanation;

            MockErrorDialogService.Verify(mock => mock.
              DisplayErrorDialog(expectedErrorTitle, expectedErrorMsg),
                Times.Once);
        }

        [TestMethod]
        public async Task DeleteCfApp_LogsError_WhenDeleteAppAsyncFails()
        {
            var fakeApp = new CloudFoundryApp("junk", "junk", parentSpace: null, null);

            MockCloudFoundryService.Setup(mock => mock.
                DeleteAppAsync(fakeApp, true, true, It.IsAny<int>()))
                    .ReturnsAsync(FakeFailureDetailedResult);

            await _sut.DeleteCfApp(fakeApp);

            var logPropVal1 = "{AppName}";
            var logPropVal2 = "{DeleteResult}";
            var expectedLogMsg = $"{TasExplorerViewModel._deleteAppErrorMsg} {logPropVal1}. {logPropVal2}";

            MockLogger.Verify(m => m.
                Error(expectedLogMsg, fakeApp.AppName, FakeFailureDetailedResult.ToString()),
                    Times.Once);
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
            var fakeInitialCfInstance = new CloudFoundryInstance("fake cf name", "http://fake.api.address");
            var fakeInitialOrg = new CloudFoundryOrganization("fake org name", "fake org id", fakeInitialCfInstance);
            var fakeInitialSpace = new CloudFoundrySpace("fake space name", "fake space id", fakeInitialOrg);
            var fakeInitialApp = new CloudFoundryApp("fake app name", "fake app id", fakeInitialSpace, "state1");

            /** These view models are fakes; they inherit from their respective tvivms but override `RefreshChildren`
             * so that these tests are able to check how many times that method gets called (calling RefreshChildren
             * on these fakes increments `NumRefreshes` by 1). These view models are constructed with expanded = true
             * to make them eligible for refreshing.
             */
            var cfivm = new FakeCfInstanceViewModel(fakeInitialCfInstance, Services, expanded: true);
            var ovm = new FakeOrgViewModel(fakeInitialOrg, Services, expanded: true);
            var svm = new FakeSpaceViewModel(fakeInitialSpace, Services, expanded: true);
            var avm = new AppViewModel(fakeInitialApp, Services);

            cfivm.Children = new ObservableCollection<TreeViewItemViewModel> { ovm };
            ovm.Children = new ObservableCollection<TreeViewItemViewModel> { svm };
            svm.Children = new ObservableCollection<TreeViewItemViewModel> { avm };
            _sut.CloudFoundryList = new ObservableCollection<CfInstanceViewModel> { cfivm };

            /** INTENTION:
             * Mocks should simulate TasExplorerViewModel.InitiateFullRefresh adding 1 cf, 1 org, 
             * 1 space & 1 app to each of the respective TreeViewItemViewModels above. In addition,
             * the initial app that existed prior to the refresh should have its state changed by 
             * the refresh.
             */
            var fakeNewCfInstance = new CloudFoundryInstance("new cf", "http://new.api.address");
            var fakeNewOrg = new CloudFoundryOrganization("new org", "new org id", fakeInitialCfInstance);
            var fakeNewSpace = new CloudFoundrySpace("new space", "new space id", fakeInitialOrg);
            var fakeUpdatedApp = new CloudFoundryApp(fakeInitialApp.AppName, fakeInitialApp.AppId, fakeInitialApp.ParentSpace, "new state");
            var fakeNewApp = new CloudFoundryApp("new app", "new app id", fakeInitialSpace, "junk state");

            // simulate addition of new CF
            MockCloudFoundryService.SetupGet(mock => mock.
                CloudFoundryInstances)
                    .Returns(new Dictionary<string, CloudFoundryInstance>
                    {
                        { "fake cf name", fakeInitialCfInstance },
                        { "new cf name", fakeNewCfInstance },
                    });

            // simulate addition of new Org
            MockCloudFoundryService.Setup(mock => mock.
                GetOrgsForCfInstanceAsync(fakeInitialCfInstance, true, 1))
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

            // ensure refresh added second cfivm
            Assert.AreEqual(2, _sut.CloudFoundryList.Count);
            CfInstanceViewModel firstCfVm = _sut.CloudFoundryList[0];
            CfInstanceViewModel secondCfVm = _sut.CloudFoundryList[1];
            Assert.AreEqual(cfivm, firstCfVm);
            Assert.AreEqual(fakeInitialCfInstance, firstCfVm.CloudFoundryInstance);
            Assert.AreEqual(fakeNewCfInstance, secondCfVm.CloudFoundryInstance);

            // ensure cf refresh added second org vm
            Assert.AreEqual(2, firstCfVm.Children.Count);
            OrgViewModel firstOrgVm = (OrgViewModel)firstCfVm.Children[0];
            OrgViewModel secondOrgVm = (OrgViewModel)firstCfVm.Children[1];
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

            // No need to get children for CFs that were just added by refresh.
            MockCloudFoundryService.Verify(mock => mock.
                GetOrgsForCfInstanceAsync(fakeNewCfInstance, true, It.IsAny<int>()), Times.Never);

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
            var fakeInitialCfInstance = new CloudFoundryInstance("fake cf name", "http://fake.api.address");
            var cfivm = new FakeCfInstanceViewModel(fakeInitialCfInstance, Services, expanded: true);
            cfivm.Children = new ObservableCollection<TreeViewItemViewModel>
            {
                new FakePlaceholderViewModel(cfivm, Services),
            };

            MockCloudFoundryService.SetupGet(mock => mock.
                CloudFoundryInstances)
                    .Returns(new Dictionary<string, CloudFoundryInstance>
                    {
                        { "fake cf name", fakeInitialCfInstance },
                    });

            MockCloudFoundryService.Setup(mock => mock.
                GetOrgsForCfInstanceAsync(fakeInitialCfInstance, true, 1))
                    .ReturnsAsync(new DetailedResult<List<CloudFoundryOrganization>>(
                        succeeded: true,
                        content: new List<CloudFoundryOrganization>
                        {
                        },
                        explanation: null,
                        cmdDetails: FakeSuccessCmdResult));

            _sut.CloudFoundryList = new ObservableCollection<CfInstanceViewModel> { cfivm };

            // pre-check: tas explorer has 1 cf view model & it's expanded
            Assert.AreEqual(1, _sut.CloudFoundryList.Count);
            Assert.IsTrue(_sut.CloudFoundryList[0].IsExpanded);

            // pre-check: cf view model has 1 child & it's a placeholder 
            Assert.AreEqual(1, _sut.CloudFoundryList[0].Children.Count);
            Assert.AreEqual(typeof(FakePlaceholderViewModel), _sut.CloudFoundryList[0].Children[0].GetType());

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
            var fakeInitialCfInstance = new CloudFoundryInstance("fake cf name", "http://fake.api.address");
            var fakeInitialOrg = new CloudFoundryOrganization("fake org name", "fake org id", fakeInitialCfInstance);
            var cfivm = new FakeCfInstanceViewModel(fakeInitialCfInstance, Services, expanded: true);
            var ovm = new FakeOrgViewModel(fakeInitialOrg, Services, expanded: false);

            MockCloudFoundryService.SetupGet(mock => mock.
                CloudFoundryInstances)
                    .Returns(new Dictionary<string, CloudFoundryInstance>
                    {
                        { "fake cf name", fakeInitialCfInstance },
                    });

            MockCloudFoundryService.Setup(mock => mock.
                GetOrgsForCfInstanceAsync(fakeInitialCfInstance, true, 1))
                    .ReturnsAsync(new DetailedResult<List<CloudFoundryOrganization>>(
                        succeeded: true,
                        content: new List<CloudFoundryOrganization>
                        {
                            fakeInitialOrg,
                        },
                        explanation: null,
                        cmdDetails: FakeSuccessCmdResult));

            _sut.CloudFoundryList = new ObservableCollection<CfInstanceViewModel> { cfivm };

            // pre-check: tas explorer has 1 cf view model & it's expanded
            Assert.AreEqual(1, _sut.CloudFoundryList.Count);
            Assert.IsTrue(_sut.CloudFoundryList[0].IsExpanded);

            // pre-check: cf view model has 1 org child & it's collapsed
            Assert.AreEqual(1, _sut.CloudFoundryList[0].Children.Count);
            Assert.IsFalse(_sut.CloudFoundryList[0].Children[0].IsExpanded);

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
            var fakeInitialCfInstance = new CloudFoundryInstance("fake cf name", "http://fake.api.address");
            var cfivm = new FakeCfInstanceViewModel(fakeInitialCfInstance, Services, expanded: true)
            {
                IsLoading = true,
            };

            cfivm.Children = new ObservableCollection<TreeViewItemViewModel>
            {
                cfivm.LoadingPlaceholder,
            };

            MockCloudFoundryService.SetupGet(mock => mock.
                CloudFoundryInstances)
                    .Returns(new Dictionary<string, CloudFoundryInstance>
                    {
                        { fakeInitialCfInstance.InstanceName, fakeInitialCfInstance },
                    });

            _sut.CloudFoundryList = new ObservableCollection<CfInstanceViewModel> { cfivm };

            // pre-check: tas explorer has 1 cf view model & it's loading
            Assert.AreEqual(1, _sut.CloudFoundryList.Count);
            Assert.IsTrue(_sut.CloudFoundryList[0].IsExpanded);
            Assert.IsTrue(_sut.CloudFoundryList[0].IsLoading);
            Assert.AreEqual(1, _sut.CloudFoundryList[0].Children.Count);
            Assert.AreEqual(typeof(PlaceholderViewModel), _sut.CloudFoundryList[0].Children[0].GetType());
            Assert.AreEqual(CfInstanceViewModel._loadingMsg, _sut.CloudFoundryList[0].Children[0].DisplayText);

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
            var fakeInitialCfInstance = new CloudFoundryInstance("fake cf name", "http://fake.api.address");
            var fakeInitialOrg = new CloudFoundryOrganization("fake org name", "fake org id", fakeInitialCfInstance);
            var cfivm = new FakeCfInstanceViewModel(fakeInitialCfInstance, Services, expanded: true);
            var ovm = new FakeOrgViewModel(fakeInitialOrg, Services, expanded: true)
            {
                IsLoading = true
            };

            cfivm.Children = new ObservableCollection<TreeViewItemViewModel>
            {
                ovm,
            };

            MockCloudFoundryService.SetupGet(mock => mock.
                CloudFoundryInstances)
                    .Returns(new Dictionary<string, CloudFoundryInstance>
                    {
                        { fakeInitialCfInstance.InstanceName, fakeInitialCfInstance },
                    });

            MockCloudFoundryService.Setup(mock => mock.
                GetOrgsForCfInstanceAsync(fakeInitialCfInstance, true, 1))
                    .ReturnsAsync(new DetailedResult<List<CloudFoundryOrganization>>(
                        succeeded: true,
                        content: new List<CloudFoundryOrganization>
                        {
                            fakeInitialOrg,
                        },
                        explanation: null,
                        cmdDetails: FakeSuccessCmdResult));

            _sut.CloudFoundryList = new ObservableCollection<CfInstanceViewModel> { cfivm };

            // pre-check: tas explorer has 1 cf view model & it's expanded
            Assert.AreEqual(1, _sut.CloudFoundryList.Count);
            Assert.IsTrue(_sut.CloudFoundryList[0].IsExpanded);

            // pre-check: cfivm has 1 child & it's loading
            Assert.AreEqual(1, _sut.CloudFoundryList[0].Children.Count);
            var orgChild = _sut.CloudFoundryList[0].Children[0];
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
            var fakeInitialCfInstance = new CloudFoundryInstance("fake cf name", "http://fake.api.address");
            var fakeInitialOrg = new CloudFoundryOrganization("fake org name", "fake org id", fakeInitialCfInstance);
            var fakeInitialSpace = new CloudFoundrySpace("fake space name", "fake space id", fakeInitialOrg);
            var cfivm = new FakeCfInstanceViewModel(fakeInitialCfInstance, Services, expanded: true);
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

            MockCloudFoundryService.SetupGet(mock => mock.
                CloudFoundryInstances)
                    .Returns(new Dictionary<string, CloudFoundryInstance>
                    {
                        { fakeInitialCfInstance.InstanceName, fakeInitialCfInstance },
                    });

            MockCloudFoundryService.Setup(mock => mock.
                GetOrgsForCfInstanceAsync(fakeInitialCfInstance, true, 1))
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

            _sut.CloudFoundryList = new ObservableCollection<CfInstanceViewModel> { cfivm };

            // pre-check: tas explorer has 1 cf view model & it's expanded
            Assert.AreEqual(1, _sut.CloudFoundryList.Count);
            var cf = _sut.CloudFoundryList[0];
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
        public void SettingAuthenticationRequiredToTrue_CollapsesAllCfInstanceViewModels()
        {
            _sut.CloudFoundryList.Add(new CfInstanceViewModel(FakeCfInstance, _sut, Services, expanded: true));
            _sut.CloudFoundryList.Add(new CfInstanceViewModel(FakeCfInstance, _sut, Services, expanded: true));
            _sut.CloudFoundryList.Add(new CfInstanceViewModel(FakeCfInstance, _sut, Services, expanded: true));

            Assert.IsFalse(_sut.AuthenticationRequired);

            _sut.AuthenticationRequired = true;

            Assert.IsTrue(_sut.AuthenticationRequired);
            foreach (CfInstanceViewModel cfivm in _sut.CloudFoundryList)
            {
                Assert.IsFalse(cfivm.IsExpanded);
            }
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
