using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Tanzu.Toolkit.Models;
using Tanzu.Toolkit.Services;

namespace Tanzu.Toolkit.ViewModels.Tests
{
    [TestClass]
    public class CloudExplorerViewModelTests : ViewModelTestSupport
    {
        private CloudExplorerViewModel _vm;
        private List<string> _receivedEvents;

        [TestInitialize]
        public void TestInit()
        {
            RenewMockServices();

            MockCloudFoundryService.SetupGet(mock => mock.CloudFoundryInstances).Returns(new Dictionary<string, CloudFoundryInstance>());
            _vm = new CloudExplorerViewModel(Services);
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
            Assert.IsTrue(_vm.CanOpenLoginView(null));
        }

        [TestMethod]
        public void CanStopCfApp_ReturnsTrue()
        {
            Assert.IsTrue(_vm.CanStopCfApp(null));
        }

        [TestMethod]
        public void CanStartCfApp_ReturnsTrue()
        {
            Assert.IsTrue(_vm.CanStartCfApp(null));
        }

        [TestMethod]
        public void CanDeleteCfApp_ReturnsTrue()
        {
            Assert.IsTrue(_vm.CanDeleteCfApp(null));
        }

        [TestMethod]
        public void CanRefreshCfInstance_ReturnsTrue()
        {
            Assert.IsTrue(_vm.CanRefreshCfInstance(null));
        }

        [TestMethod]
        public void CanRefreshOrg_ReturnsTrue()
        {
            Assert.IsTrue(_vm.CanRefreshOrg(null));
        }

        [TestMethod]
        public void CanRefreshSpace_ReturnsTrue()
        {
            Assert.IsTrue(_vm.CanRefreshSpace(null));
        }

        [TestMethod]
        public void CanRefreshApp_ReturnsTrue()
        {
            Assert.IsTrue(_vm.CanRefreshApp(null));
        }

        [TestMethod]
        public void CanRefreshAllCloudConnections_ReturnsTrue()
        {
            Assert.IsTrue(_vm.CanRefreshAllCloudConnections(null));
        }

        [TestMethod]
        public void OpenLoginView_CallsDialogService_ShowDialog()
        {
            _vm.OpenLoginView(null);
            MockDialogService.Verify(ds => ds.ShowDialog(typeof(AddCloudDialogViewModel).Name, null), Times.Once);
        }

        [TestMethod]
        public void OpenLoginView_UpdatesCloudFoundryInstances_AfterDialogCloses()
        {
            var emptyCfsDict = new Dictionary<string, CloudFoundryInstance>();
            var fakeCfsDict = new Dictionary<string, CloudFoundryInstance>
            {
                { "fake cf", new CloudFoundryInstance("fake cf", null, null) },
            };

            MockCloudFoundryService.SetupSequence(mock => mock.CloudFoundryInstances)
                .Returns(emptyCfsDict) // return empty on first request to avoid error due to temporary "single-cf" requirement.
                .Returns(fakeCfsDict); // return fake cf on second request as mock result of having logged in.

            Assert.AreEqual(0, _vm.CloudFoundryList.Count);

            _vm.OpenLoginView(null);

            Assert.IsTrue(_vm.HasCloudTargets);
            Assert.AreEqual(1, _vm.CloudFoundryList.Count);
        }

        [TestMethod]
        public async Task StopCfApp_CallsStopCfAppAsync()
        {
            var fakeApp = new CloudFoundryApp("junk", "junk", parentSpace: null, null);

            MockCloudFoundryService.Setup(mock => mock.StopAppAsync(fakeApp, true)).ReturnsAsync(FakeSuccessDetailedResult);

            Exception shouldStayNull = null;
            try
            {
                await _vm.StopCfApp(fakeApp);
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
                StopAppAsync(fakeApp, true))
                    .ReturnsAsync(FakeFailureDetailedResult);

            await _vm.StopCfApp(fakeApp);

            var expectedErrorTitle = $"{CloudExplorerViewModel._stopAppErrorMsg} {fakeApp.AppName}.";
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
                StopAppAsync(fakeApp, true))
                    .ReturnsAsync(FakeFailureDetailedResult);

            await _vm.StopCfApp(fakeApp);

            var logPropVal1 = "{AppName}";
            var logPropVal2 = "{StopResult}";
            var expectedLogMsg = $"{CloudExplorerViewModel._stopAppErrorMsg} {logPropVal1}. {logPropVal2}";

            MockLogger.Verify(m => m.
                Error(expectedLogMsg, fakeApp.AppName, FakeFailureDetailedResult.ToString()),
                    Times.Once);
        }

        [TestMethod]
        public async Task StartCfApp_CallsStartAppAsync()
        {
            var fakeApp = new CloudFoundryApp("junk", "junk", parentSpace: null, null);

            MockCloudFoundryService.Setup(mock => mock.StartAppAsync(fakeApp, true)).ReturnsAsync(FakeSuccessDetailedResult);

            Exception shouldStayNull = null;
            try
            {
                await _vm.StartCfApp(fakeApp);
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
                StartAppAsync(fakeApp, true))
                    .ReturnsAsync(FakeFailureDetailedResult);

            await _vm.StartCfApp(fakeApp);

            var expectedErrorTitle = $"{CloudExplorerViewModel._startAppErrorMsg} {fakeApp.AppName}.";
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
                StartAppAsync(fakeApp, true))
                    .ReturnsAsync(FakeFailureDetailedResult);

            await _vm.StartCfApp(fakeApp);

            var logPropVal1 = "{AppName}";
            var logPropVal2 = "{StartResult}";
            var expectedLogMsg = $"{CloudExplorerViewModel._startAppErrorMsg} {logPropVal1}. {logPropVal2}";

            MockLogger.Verify(m => m.
                Error(expectedLogMsg, fakeApp.AppName, FakeFailureDetailedResult.ToString()),
                    Times.Once);
        }

        [TestMethod]
        public async Task DeleteCfApp_CallsDeleteAppAsync()
        {
            var fakeApp = new CloudFoundryApp("junk", "junk", parentSpace: null, null);

            MockCloudFoundryService.Setup(mock => mock.DeleteAppAsync(fakeApp, true, true)).ReturnsAsync(FakeSuccessDetailedResult);

            Exception shouldStayNull = null;
            try
            {
                await _vm.DeleteCfApp(fakeApp);
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
                DeleteAppAsync(fakeApp, true, true))
                    .ReturnsAsync(FakeFailureDetailedResult);

            await _vm.DeleteCfApp(fakeApp);

            var expectedErrorTitle = $"{CloudExplorerViewModel._deleteAppErrorMsg} {fakeApp.AppName}.";
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
                DeleteAppAsync(fakeApp, true, true))
                    .ReturnsAsync(FakeFailureDetailedResult);

            await _vm.DeleteCfApp(fakeApp);

            var logPropVal1 = "{AppName}";
            var logPropVal2 = "{DeleteResult}";
            var expectedLogMsg = $"{CloudExplorerViewModel._deleteAppErrorMsg} {logPropVal1}. {logPropVal2}";

            MockLogger.Verify(m => m.
                Error(expectedLogMsg, fakeApp.AppName, FakeFailureDetailedResult.ToString()),
                    Times.Once);
        }

        [TestMethod]
        public void RefreshApp_RaisesPropertyChangedEventForIsStopped()
        {
            CloudFoundryApp fakeApp = new CloudFoundryApp("fake app name", "fake app guid", null, null);
            var avm = new AppViewModel(fakeApp, Services);

            avm.PropertyChanged += (sender, e) =>
            {
                _receivedEvents.Add(e.PropertyName);
            };

            _vm.RefreshApp(avm);

            Assert.AreEqual(1, _receivedEvents.Count);
            Assert.AreEqual("IsStopped", _receivedEvents[0]);
        }

        [TestMethod]
        public async Task RefreshCfInstance_UpdatesChildrenOnCfInstanceViewModel()
        {
            var fakeCfInstance = new CloudFoundryInstance("fake name", "fake id", null);
            var fakeCfInstanceViewModel = new CfInstanceViewModel(fakeCfInstance, Services);

            var fakeOrgName1 = "fake org 1";
            var fakeOrgName2 = "fake org 2";
            var fakeOrgName3 = "fake org 3";

            var fakeOrgGuid1 = "fake org 1";
            var fakeOrgGuid2 = "fake org 2";
            var fakeOrgGuid3 = "fake org 3";

            fakeCfInstanceViewModel.Children = new ObservableCollection<TreeViewItemViewModel>
            {
                new OrgViewModel(new CloudFoundryOrganization(fakeOrgName1, fakeOrgGuid1, fakeCfInstance), Services),
            };

            fakeCfInstanceViewModel.PropertyChanged += (sender, e) =>
            {
                _receivedEvents.Add(e.PropertyName);
            };

            var fakeOrgsList = new List<CloudFoundryOrganization>
            {
                new CloudFoundryOrganization(fakeOrgName2, fakeOrgGuid2, fakeCfInstance),
                new CloudFoundryOrganization(fakeOrgName3, fakeOrgGuid3, fakeCfInstance),
            };

            var fakeSuccessResult = new DetailedResult<List<CloudFoundryOrganization>>(succeeded: true, content: fakeOrgsList);

            Assert.AreEqual(1, fakeCfInstanceViewModel.Children.Count);
            OrgViewModel firstChildOrg = (OrgViewModel)fakeCfInstanceViewModel.Children[0];
            Assert.AreEqual(fakeOrgName1, firstChildOrg.Org.OrgName);

            MockCloudFoundryService.Setup(mock => mock.
                GetOrgsForCfInstanceAsync(fakeCfInstance, true))
                    .ReturnsAsync(fakeSuccessResult);

            await _vm.RefreshCfInstance(fakeCfInstanceViewModel);

            Assert.AreEqual(2, fakeCfInstanceViewModel.Children.Count);
            OrgViewModel firstNewChildOrg = (OrgViewModel)fakeCfInstanceViewModel.Children[0];
            OrgViewModel secondNewChildOrg = (OrgViewModel)fakeCfInstanceViewModel.Children[1];
            Assert.AreEqual(fakeOrgName2, firstNewChildOrg.Org.OrgName);
            Assert.AreEqual(fakeOrgName3, secondNewChildOrg.Org.OrgName);

            // property changed events should not be raised
            Assert.AreEqual(0, _receivedEvents.Count);

            MockCloudFoundryService.VerifyAll();
        }

        [TestMethod]
        public async Task RefreshCfInstance_AddsPlaceholder_ToCFsThatBecameEmpty()
        {
            var fakeCfInstance = new CloudFoundryInstance("fake name", "fake id", null);
            var fakeCfInstanceViewModel = new CfInstanceViewModel(fakeCfInstance, Services);

            var fakeOrgName1 = "fake org 1";
            var fakeOrgGuid1 = "fake org 1";

            fakeCfInstanceViewModel.Children = new ObservableCollection<TreeViewItemViewModel>
            {
                new OrgViewModel(new CloudFoundryOrganization(fakeOrgName1, fakeOrgGuid1, fakeCfInstance), Services),
            };

            var newEmptyOrgsList = new List<CloudFoundryOrganization>();

            var fakeNoOrgsResult = new DetailedResult<List<CloudFoundryOrganization>>(succeeded: true, content: newEmptyOrgsList);

            Assert.AreEqual(1, fakeCfInstanceViewModel.Children.Count);
            OrgViewModel firstChildOrg = (OrgViewModel)fakeCfInstanceViewModel.Children[0];
            Assert.AreEqual(fakeOrgName1, firstChildOrg.Org.OrgName);

            MockCloudFoundryService.Setup(mock => mock.
                GetOrgsForCfInstanceAsync(fakeCfInstance, true))
                    .ReturnsAsync(fakeNoOrgsResult);

            await _vm.RefreshCfInstance(fakeCfInstanceViewModel);

            Assert.AreEqual(1, fakeCfInstanceViewModel.Children.Count);
            Assert.AreEqual(typeof(PlaceholderViewModel), fakeCfInstanceViewModel.Children[0].GetType());
        }

        [TestMethod]
        public async Task RefreshCfInstance_RemovesPlaceholder_FromEmptyCFsThatGainedChildren()
        {
            var fakeCfInstance = new CloudFoundryInstance("fake name", "fake id", null);
            var fakeCfInstanceViewModel = new CfInstanceViewModel(fakeCfInstance, Services);

            // simulate cf initially having no org children
            fakeCfInstanceViewModel.Children = new ObservableCollection<TreeViewItemViewModel> { fakeCfInstanceViewModel.EmptyPlaceholder };
            fakeCfInstanceViewModel.HasEmptyPlaceholder = true;

            var fakeOrgName1 = "fake org 1";
            var fakeOrgGuid1 = "fake org 1";

            var newOrgsList = new List<CloudFoundryOrganization>
            {
                new CloudFoundryOrganization(fakeOrgName1, fakeOrgGuid1, fakeCfInstance),
            };

            var fakeSuccessResult = new DetailedResult<List<CloudFoundryOrganization>>(succeeded: true, content: newOrgsList);

            MockCloudFoundryService.Setup(mock => mock.
                GetOrgsForCfInstanceAsync(fakeCfInstance, true))
                    .ReturnsAsync(fakeSuccessResult);

            Assert.AreEqual(1, fakeCfInstanceViewModel.Children.Count);
            Assert.AreEqual(typeof(PlaceholderViewModel), fakeCfInstanceViewModel.Children[0].GetType());

            await _vm.RefreshCfInstance(fakeCfInstanceViewModel);

            Assert.AreEqual(1, fakeCfInstanceViewModel.Children.Count);
            var child = fakeCfInstanceViewModel.Children[0];
            Assert.AreEqual(typeof(OrgViewModel), child.GetType());
        }

        [TestMethod]
        public async Task RefreshOrg_UpdatesChildrenOnOrgViewModel()
        {
            var fakeOrg = new CloudFoundryOrganization("fake org name", "fake org id", null);
            var fakeOrgViewModel = new OrgViewModel(fakeOrg, Services);

            var fakeSpaceName1 = "fake space 1";
            var fakeSpaceName2 = "fake space 2";
            var fakeSpaceName3 = "fake space 3";

            var fakeSpaceGuid1 = "fake space 1";
            var fakeSpaceGuid2 = "fake space 2";
            var fakeSpaceGuid3 = "fake space 3";

            fakeOrgViewModel.Children = new ObservableCollection<TreeViewItemViewModel>
            {
                new SpaceViewModel(new CloudFoundrySpace(fakeSpaceName1, fakeSpaceGuid1, fakeOrg), Services),
            };

            fakeOrgViewModel.PropertyChanged += (sender, e) =>
            {
                _receivedEvents.Add(e.PropertyName);
            };

            Assert.AreEqual(1, fakeOrgViewModel.Children.Count);
            SpaceViewModel firstChildSpace = (SpaceViewModel)fakeOrgViewModel.Children[0];
            Assert.AreEqual(fakeSpaceName1, firstChildSpace.Space.SpaceName);

            List<CloudFoundrySpace> fakeSpaceList = new List<CloudFoundrySpace>
            {
                new CloudFoundrySpace(fakeSpaceName2, fakeSpaceGuid2, fakeOrg),
                new CloudFoundrySpace(fakeSpaceName3, fakeSpaceGuid3, fakeOrg),
            };

            var fakeSuccessResponse = new DetailedResult<List<CloudFoundrySpace>>(
                content: fakeSpaceList,
                succeeded: true,
                explanation: null,
                cmdDetails: FakeSuccessCmdResult);

            MockCloudFoundryService.Setup(mock => mock.
                GetSpacesForOrgAsync(fakeOrg, true))
                    .ReturnsAsync(fakeSuccessResponse);

            await _vm.RefreshOrg(fakeOrgViewModel);

            Assert.AreEqual(2, fakeOrgViewModel.Children.Count);
            SpaceViewModel firstNewChildSpace = (SpaceViewModel)fakeOrgViewModel.Children[0];
            SpaceViewModel secondNewChildSpace = (SpaceViewModel)fakeOrgViewModel.Children[1];
            Assert.AreEqual(fakeSpaceName2, firstNewChildSpace.Space.SpaceName);
            Assert.AreEqual(fakeSpaceName3, secondNewChildSpace.Space.SpaceName);

            // property changed events should not be raised
            Assert.AreEqual(0, _receivedEvents.Count);

            MockCloudFoundryService.VerifyAll();
        }

        [TestMethod]
        public async Task RefreshOrg_AddsPlaceholder_ToOrgsThatBecameEmpty()
        {
            var fakeOrg = new CloudFoundryOrganization("fake name", "fake id", null);
            var fakeOrgViewModel = new OrgViewModel(fakeOrg, Services);

            var fakeSpaceName1 = "fake space 1";
            var fakeSpaceGuid1 = "fake space 1";

            fakeOrgViewModel.Children = new ObservableCollection<TreeViewItemViewModel>
            {
                new SpaceViewModel(new CloudFoundrySpace(fakeSpaceName1, fakeSpaceGuid1, fakeOrg), Services),
            };

            var newEmptySpacesList = new List<CloudFoundrySpace>();

            var fakeNoSpacesResult = new DetailedResult<List<CloudFoundrySpace>>(succeeded: true, content: newEmptySpacesList);

            Assert.AreEqual(1, fakeOrgViewModel.Children.Count);
            SpaceViewModel firstChildSpace = (SpaceViewModel)fakeOrgViewModel.Children[0];
            Assert.AreEqual(fakeSpaceName1, firstChildSpace.Space.SpaceName);

            MockCloudFoundryService.Setup(mock => mock.
                GetSpacesForOrgAsync(fakeOrg, true))
                    .ReturnsAsync(fakeNoSpacesResult);

            await _vm.RefreshOrg(fakeOrgViewModel);

            Assert.AreEqual(1, fakeOrgViewModel.Children.Count);
            Assert.AreEqual(typeof(PlaceholderViewModel), fakeOrgViewModel.Children[0].GetType());
        }

        [TestMethod]
        public async Task RefreshOrg_RemovesPlaceholder_FromEmptyOrgsThatGainedChildren()
        {
            var fakeOrg = new CloudFoundryOrganization("fake name", "fake id", null);
            var fakeOrgViewModel = new OrgViewModel(fakeOrg, Services);

            var fakeSpaceName1 = "fake space 1";
            var fakeSpaceGuid1 = "fake space 1";

            // simulate org initially having no space children
            fakeOrgViewModel.HasEmptyPlaceholder = true;
            fakeOrgViewModel.Children = new ObservableCollection<TreeViewItemViewModel>
            {
                fakeOrgViewModel.EmptyPlaceholder,
            };

            var newSpacesList = new List<CloudFoundrySpace>
            {
                new CloudFoundrySpace(fakeSpaceName1, fakeSpaceGuid1, fakeOrg),
            };

            var fakeSuccessResult = new DetailedResult<List<CloudFoundrySpace>>(succeeded: true, content: newSpacesList);

            MockCloudFoundryService.Setup(mock => mock.
                GetSpacesForOrgAsync(fakeOrg, true))
                    .ReturnsAsync(fakeSuccessResult);

            Assert.AreEqual(1, fakeOrgViewModel.Children.Count);
            Assert.AreEqual(typeof(PlaceholderViewModel), fakeOrgViewModel.Children[0].GetType());

            await _vm.RefreshOrg(fakeOrgViewModel);

            Assert.AreEqual(1, fakeOrgViewModel.Children.Count);
            var child = fakeOrgViewModel.Children[0];
            Assert.AreEqual(typeof(SpaceViewModel), child.GetType());
        }

        [TestMethod]
        public async Task RefreshSpace_UpdatesChildrenOnSpaceViewModel()
        {
            var fakeSpace = new CloudFoundrySpace("fake space name", "fake space id", null);
            var fakeSpaceViewModel = new SpaceViewModel(fakeSpace, Services);

            var fakeAppName1 = "fake app 1";
            var fakeAppName2 = "fake app 2";
            var fakeAppName3 = "fake app 3";

            var fakeAppGuid1 = "fake app 1";
            var fakeAppGuid2 = "fake app 2";
            var fakeAppGuid3 = "fake app 3";

            fakeSpaceViewModel.Children = new ObservableCollection<TreeViewItemViewModel>
            {
                new AppViewModel(new CloudFoundryApp(fakeAppName1, fakeAppGuid1, fakeSpace, null), Services),
            };

            fakeSpaceViewModel.PropertyChanged += (sender, e) =>
            {
                _receivedEvents.Add(e.PropertyName);
            };

            Assert.AreEqual(1, fakeSpaceViewModel.Children.Count);
            AppViewModel firstChildApp = (AppViewModel)fakeSpaceViewModel.Children[0];
            Assert.AreEqual(fakeAppName1, firstChildApp.App.AppName);

            var fakeAppsResponseContent = new List<CloudFoundryApp>
            {
                new CloudFoundryApp(fakeAppName2, fakeAppGuid2, fakeSpace, null),
                new CloudFoundryApp(fakeAppName3, fakeAppGuid3, fakeSpace, null),
            };
            var fakeAppsResult = new DetailedResult<List<CloudFoundryApp>>(
                succeeded: true,
                content: fakeAppsResponseContent,
                explanation: null,
                cmdDetails: FakeSuccessCmdResult);

            MockCloudFoundryService.Setup(mock => mock.
                GetAppsForSpaceAsync(fakeSpace, true))
                    .ReturnsAsync(fakeAppsResult);

            await _vm.RefreshSpace(fakeSpaceViewModel);

            Assert.AreEqual(2, fakeSpaceViewModel.Children.Count);
            AppViewModel firstNewChildApp = (AppViewModel)fakeSpaceViewModel.Children[0];
            AppViewModel secondNewChildApp = (AppViewModel)fakeSpaceViewModel.Children[1];
            Assert.AreEqual(fakeAppName2, firstNewChildApp.App.AppName);
            Assert.AreEqual(fakeAppName3, secondNewChildApp.App.AppName);

            // property changed events should not be raised
            Assert.AreEqual(0, _receivedEvents.Count);

            MockCloudFoundryService.VerifyAll();
        }

        [TestMethod]
        public async Task RefreshSpace_AddsPlaceholder_ToSpacesThatBecameEmpty()
        {
            var fakeSpace = new CloudFoundrySpace("fake name", "fake id", null);
            var fakeSpaceViewModel = new SpaceViewModel(fakeSpace, Services);

            var fakeAppName1 = "fake app 1";
            var fakeAppGuid1 = "fake app 1";

            fakeSpaceViewModel.Children = new ObservableCollection<TreeViewItemViewModel>
            {
                new AppViewModel(new CloudFoundryApp(fakeAppName1, fakeAppGuid1, fakeSpace, null), Services),
            };

            var newEmptyAppsList = new List<CloudFoundryApp>();

            var fakeNoAppsResult = new DetailedResult<List<CloudFoundryApp>>(succeeded: true, content: newEmptyAppsList);

            Assert.AreEqual(1, fakeSpaceViewModel.Children.Count);
            AppViewModel firstChildApp = (AppViewModel)fakeSpaceViewModel.Children[0];
            Assert.AreEqual(fakeAppName1, firstChildApp.App.AppName);

            MockCloudFoundryService.Setup(mock => mock.
                GetAppsForSpaceAsync(fakeSpace, true))
                    .ReturnsAsync(fakeNoAppsResult);

            await _vm.RefreshSpace(fakeSpaceViewModel);

            Assert.AreEqual(1, fakeSpaceViewModel.Children.Count);
            Assert.AreEqual(typeof(PlaceholderViewModel), fakeSpaceViewModel.Children[0].GetType());
        }

        [TestMethod]
        public async Task RefreshSpace_RemovesPlaceholder_FromEmptySpacesThatGainedChildren()
        {
            var fakeSpace = new CloudFoundrySpace("fake name", "fake id", null);
            var fakeSpaceViewModel = new SpaceViewModel(fakeSpace, Services);

            var fakeAppName1 = "fake app 1";
            var fakeAppGuid1 = "fake app 1";

            // simulate space initially having no app children
            fakeSpaceViewModel.HasEmptyPlaceholder = true;
            fakeSpaceViewModel.Children = new ObservableCollection<TreeViewItemViewModel>
            {
                fakeSpaceViewModel.EmptyPlaceholder,
            };

            var newAppsList = new List<CloudFoundryApp>
            {
                new CloudFoundryApp(fakeAppName1, fakeAppGuid1, fakeSpace, null),
            };

            var fakeSuccessResult = new DetailedResult<List<CloudFoundryApp>>(succeeded: true, content: newAppsList);

            MockCloudFoundryService.Setup(mock => mock.
                GetAppsForSpaceAsync(fakeSpace, true))
                    .ReturnsAsync(fakeSuccessResult);

            Assert.AreEqual(1, fakeSpaceViewModel.Children.Count);
            Assert.AreEqual(typeof(PlaceholderViewModel), fakeSpaceViewModel.Children[0].GetType());

            await _vm.RefreshSpace(fakeSpaceViewModel);

            Assert.AreEqual(1, fakeSpaceViewModel.Children.Count);
            var child = fakeSpaceViewModel.Children[0];
            Assert.AreEqual(typeof(AppViewModel), child.GetType());
        }

        [TestMethod]
        public async Task RefreshAllCloudConnections_RefreshesEachTreeViewItemViewModel()
        {
            var fakeInitialCfInstance = new CloudFoundryInstance("fake cf name", "http://fake.api.address", "fake-token");
            var fakeNewCfInstance = new CloudFoundryInstance("new cf", "http://new.api.address", "new-token");
            var cfivm = new CfInstanceViewModel(fakeInitialCfInstance, Services);

            var fakeInitialOrg = new CloudFoundryOrganization("fake org name", "fake org id", fakeInitialCfInstance);
            var fakeNewOrg = new CloudFoundryOrganization("new org", "new org id", fakeInitialCfInstance);
            var ovm = new OrgViewModel(fakeInitialOrg, Services);

            var fakeInitialSpace = new CloudFoundrySpace("fake space name", "fake space id", fakeInitialOrg);
            var fakeNewSpace = new CloudFoundrySpace("new space", "new space id", fakeInitialOrg);
            var svm = new SpaceViewModel(fakeInitialSpace, Services);

            var fakeInitialApp = new CloudFoundryApp("fake app name", "fake app id", fakeInitialSpace, null);
            var fakeNewApp = new CloudFoundryApp("new app", "new app id", fakeInitialSpace, null);
            var avm = new AppViewModel(fakeInitialApp, Services);

            var fakeCfDict = new Dictionary<string, CloudFoundryInstance>
            {
                { "fake cf name", fakeInitialCfInstance },
                { "new cf name", fakeNewCfInstance },
            };

            var fakeSuccessfulOrgsResult = new DetailedResult<List<CloudFoundryOrganization>>(
                succeeded: true,
                content: new List<CloudFoundryOrganization>
                {
                    fakeInitialOrg,
                    fakeNewOrg,
                },
                explanation: null,
                cmdDetails: FakeSuccessCmdResult);

            var fakeSuccessfulSpacesResult = new DetailedResult<List<CloudFoundrySpace>>(
                succeeded: true,
                content: new List<CloudFoundrySpace>
                {
                    fakeInitialSpace,
                    fakeNewSpace,
                },
                explanation: null,
                cmdDetails: FakeSuccessCmdResult);

            var fakeSuccessfulAppsResult = new DetailedResult<List<CloudFoundryApp>>(
                succeeded: true,
                content: new List<CloudFoundryApp>
                {
                    fakeInitialApp,
                    fakeNewApp,
                },
                explanation: null,
                cmdDetails: FakeSuccessCmdResult);

            MockCloudFoundryService.SetupGet(mock => mock.
                CloudFoundryInstances)
                    .Returns(fakeCfDict);

            MockCloudFoundryService.Setup(mock => mock.
                GetOrgsForCfInstanceAsync(fakeInitialCfInstance, true))
                    .ReturnsAsync(fakeSuccessfulOrgsResult);

            MockCloudFoundryService.Setup(mock => mock.
                GetSpacesForOrgAsync(fakeInitialOrg, true))
                    .ReturnsAsync(fakeSuccessfulSpacesResult);

            MockCloudFoundryService.Setup(mock => mock.
                GetAppsForSpaceAsync(fakeInitialSpace, true))
                    .ReturnsAsync(fakeSuccessfulAppsResult);

            cfivm.Children = new ObservableCollection<TreeViewItemViewModel> { ovm };

            ovm.Children = new ObservableCollection<TreeViewItemViewModel> { svm };

            svm.Children = new ObservableCollection<TreeViewItemViewModel> { avm };

            _vm.CloudFoundryList = new ObservableCollection<CfInstanceViewModel> { cfivm };

            var eventsRaisedByAVM = new List<string>();
            avm.PropertyChanged += (sender, e) =>
            {
                eventsRaisedByAVM.Add(e.PropertyName);
            };

            await _vm.RefreshAllCloudConnections(null);

            Assert.AreEqual(2, _vm.CloudFoundryList.Count);
            CfInstanceViewModel firstCfVm = _vm.CloudFoundryList[0];
            CfInstanceViewModel secondCfVm = _vm.CloudFoundryList[1];
            Assert.AreEqual(cfivm, firstCfVm);
            Assert.AreEqual(fakeInitialCfInstance, firstCfVm.CloudFoundryInstance);
            Assert.AreEqual(fakeNewCfInstance, secondCfVm.CloudFoundryInstance);

            Assert.AreEqual(2, firstCfVm.Children.Count);
            OrgViewModel firstOrgVm = (OrgViewModel)firstCfVm.Children[0];
            OrgViewModel secondOrgVm = (OrgViewModel)firstCfVm.Children[1];
            Assert.AreEqual(ovm, firstOrgVm);
            Assert.AreEqual(fakeInitialOrg, firstOrgVm.Org);
            Assert.AreEqual(fakeNewOrg, secondOrgVm.Org);

            Assert.AreEqual(2, firstOrgVm.Children.Count);
            SpaceViewModel firstSpaceVm = (SpaceViewModel)firstOrgVm.Children[0];
            SpaceViewModel secondSpaceVm = (SpaceViewModel)firstOrgVm.Children[1];
            Assert.AreEqual(svm, firstSpaceVm);
            Assert.AreEqual(fakeInitialSpace, firstSpaceVm.Space);
            Assert.AreEqual(fakeNewSpace, secondSpaceVm.Space);

            Assert.AreEqual(2, firstSpaceVm.Children.Count);
            AppViewModel firstAppVm = (AppViewModel)firstSpaceVm.Children[0];
            AppViewModel secondAppVm = (AppViewModel)firstSpaceVm.Children[1];
            Assert.AreEqual(avm, firstAppVm);
            Assert.AreEqual(fakeInitialApp, firstAppVm.App);
            Assert.AreEqual(fakeNewApp, secondAppVm.App);

            Assert.AreEqual(1, eventsRaisedByAVM.Count);
            Assert.AreEqual("IsStopped", eventsRaisedByAVM[0]);

            // No need to get children for CFs that were just added by refresh.
            MockCloudFoundryService.Verify(mock => mock.
                GetOrgsForCfInstanceAsync(fakeNewCfInstance, true), Times.Never);

            // No need to get children for orgs that were just added by refresh.
            MockCloudFoundryService.Verify(mock => mock.
                GetSpacesForOrgAsync(fakeNewOrg, true), Times.Never);

            // No need to get children for spaces that were just added by refresh.
            MockCloudFoundryService.Verify(mock => mock.
                GetAppsForSpaceAsync(fakeNewSpace, true), Times.Never);
        }

        [TestMethod]
        public async Task RefreshAllCloudConnections_AddsPlaceholder_ToCFsThatBecameEmpty()
        {
            var fakeInitialCfInstance = new CloudFoundryInstance("fake cf name", "http://fake.api.address", "fake-token");
            var cfivm = new CfInstanceViewModel(fakeInitialCfInstance, Services);

            var fakeInitialOrg = new CloudFoundryOrganization("fake org name", "fake org id", fakeInitialCfInstance);
            var ovm = new OrgViewModel(fakeInitialOrg, Services);

            var fakeCfDict = new Dictionary<string, CloudFoundryInstance>
            {
                { "fake cf name", fakeInitialCfInstance },
            };

            var fakeEmptyOrgsResult = new DetailedResult<List<CloudFoundryOrganization>>(
                succeeded: true,
                content: new List<CloudFoundryOrganization>(), // simulate cf having lost all orgs before refresh
                explanation: null,
                cmdDetails: FakeSuccessCmdResult);

            MockCloudFoundryService.SetupGet(mock => mock.
                CloudFoundryInstances)
                    .Returns(fakeCfDict);

            MockCloudFoundryService.Setup(mock => mock.
                GetOrgsForCfInstanceAsync(fakeInitialCfInstance, true))
                    .ReturnsAsync(fakeEmptyOrgsResult);

            cfivm.Children = new ObservableCollection<TreeViewItemViewModel> { ovm }; // simulate cf initially having 1 org child

            _vm.CloudFoundryList = new ObservableCollection<CfInstanceViewModel> { cfivm };

            Assert.AreEqual(1, cfivm.Children.Count);
            Assert.AreEqual(typeof(OrgViewModel), cfivm.Children[0].GetType());

            await _vm.RefreshAllCloudConnections(null);

            Assert.AreEqual(1, cfivm.Children.Count);

            Assert.AreEqual(typeof(PlaceholderViewModel), cfivm.Children[0].GetType());
            Assert.AreEqual(CfInstanceViewModel._emptyOrgsPlaceholderMsg, cfivm.Children[0].DisplayText);
        }

        [TestMethod]
        public async Task RefreshAllCloudConnections_AddsPlaceholder_ToOrgsThatBecameEmpty()
        {
            var fakeInitialCfInstance = new CloudFoundryInstance("fake cf name", "http://fake.api.address", "fake-token");
            var cfivm = new CfInstanceViewModel(fakeInitialCfInstance, Services);

            var fakeInitialOrg = new CloudFoundryOrganization("fake org name", "fake org id", fakeInitialCfInstance);
            var ovm = new OrgViewModel(fakeInitialOrg, Services);

            var fakeInitialSpace = new CloudFoundrySpace("fake space name", "fake space id", fakeInitialOrg);
            var svm = new SpaceViewModel(fakeInitialSpace, Services);

            var fakeCfDict = new Dictionary<string, CloudFoundryInstance>
            {
                { "fake cf name", fakeInitialCfInstance },
            };

            var fakeSuccessfulOrgsResult = new DetailedResult<List<CloudFoundryOrganization>>(
                succeeded: true,
                content: new List<CloudFoundryOrganization>
                {
                    fakeInitialOrg,
                },
                explanation: null,
                cmdDetails: FakeSuccessCmdResult);

            var fakeNoSpacesResult = new DetailedResult<List<CloudFoundrySpace>>(
                succeeded: true,
                content: new List<CloudFoundrySpace>(), // simulate org having lost all spaces before refresh
                explanation: null,
                cmdDetails: FakeSuccessCmdResult);

            MockCloudFoundryService.SetupGet(mock => mock.
                CloudFoundryInstances)
                    .Returns(fakeCfDict);

            MockCloudFoundryService.Setup(mock => mock.
                GetOrgsForCfInstanceAsync(fakeInitialCfInstance, true))
                    .ReturnsAsync(fakeSuccessfulOrgsResult);

            MockCloudFoundryService.Setup(mock => mock.
                GetSpacesForOrgAsync(fakeInitialOrg, true))
                    .ReturnsAsync(fakeNoSpacesResult);

            cfivm.Children = new ObservableCollection<TreeViewItemViewModel> { ovm };

            ovm.Children = new ObservableCollection<TreeViewItemViewModel> { svm }; // simulate org initially having 1 space child

            _vm.CloudFoundryList = new ObservableCollection<CfInstanceViewModel> { cfivm };

            Assert.AreEqual(1, ovm.Children.Count);
            Assert.AreEqual(typeof(SpaceViewModel), ovm.Children[0].GetType());

            await _vm.RefreshAllCloudConnections(null);

            Assert.AreEqual(1, ovm.Children.Count);
            Assert.AreEqual(typeof(PlaceholderViewModel), ovm.Children[0].GetType());
            Assert.AreEqual(OrgViewModel._emptySpacesPlaceholderMsg, ovm.Children[0].DisplayText);
        }

        [TestMethod]
        public async Task RefreshAllCloudConnections_AddsPlaceholder_ToSpacesThatBecameEmpty()
        {
            var fakeInitialCfInstance = new CloudFoundryInstance("fake cf name", "http://fake.api.address", "fake-token");
            var cfivm = new CfInstanceViewModel(fakeInitialCfInstance, Services);

            var fakeInitialOrg = new CloudFoundryOrganization("fake org name", "fake org id", fakeInitialCfInstance);
            var ovm = new OrgViewModel(fakeInitialOrg, Services);

            var fakeInitialSpace = new CloudFoundrySpace("fake space name", "fake space id", fakeInitialOrg);
            var svm = new SpaceViewModel(fakeInitialSpace, Services);

            var fakeInitialApp = new CloudFoundryApp("fake app name", "fake app id", fakeInitialSpace, null);
            var avm = new AppViewModel(fakeInitialApp, Services);

            var fakeCfDict = new Dictionary<string, CloudFoundryInstance>
            {
                { "fake cf name", fakeInitialCfInstance },
            };

            var fakeSuccessfulOrgsResult = new DetailedResult<List<CloudFoundryOrganization>>(
                succeeded: true,
                content: new List<CloudFoundryOrganization>
                {
                    fakeInitialOrg,
                },
                explanation: null,
                cmdDetails: FakeSuccessCmdResult);

            var fakeSuccessfulSpacesResult = new DetailedResult<List<CloudFoundrySpace>>(
                succeeded: true,
                content: new List<CloudFoundrySpace>
                {
                    fakeInitialSpace,
                },
                explanation: null,
                cmdDetails: FakeSuccessCmdResult);

            var fakeNoAppsResult = new DetailedResult<List<CloudFoundryApp>>(
                succeeded: true,
                content: new List<CloudFoundryApp>(), // simulate space having lost all apps before refresh
                explanation: null,
                cmdDetails: FakeSuccessCmdResult);

            MockCloudFoundryService.SetupGet(mock => mock.
                CloudFoundryInstances)
                    .Returns(fakeCfDict);

            MockCloudFoundryService.Setup(mock => mock.
                GetOrgsForCfInstanceAsync(fakeInitialCfInstance, true))
                    .ReturnsAsync(fakeSuccessfulOrgsResult);

            MockCloudFoundryService.Setup(mock => mock.
                GetSpacesForOrgAsync(fakeInitialOrg, true))
                    .ReturnsAsync(fakeSuccessfulSpacesResult);

            MockCloudFoundryService.Setup(mock => mock.
                GetAppsForSpaceAsync(fakeInitialSpace, true))
                    .ReturnsAsync(fakeNoAppsResult);

            cfivm.Children = new ObservableCollection<TreeViewItemViewModel> { ovm };

            ovm.Children = new ObservableCollection<TreeViewItemViewModel> { svm };

            svm.Children = new ObservableCollection<TreeViewItemViewModel> { avm }; // simulate space initially having 1 app child

            _vm.CloudFoundryList = new ObservableCollection<CfInstanceViewModel> { cfivm };

            Assert.AreEqual(1, svm.Children.Count);
            Assert.AreEqual(typeof(AppViewModel), svm.Children[0].GetType());

            await _vm.RefreshAllCloudConnections(null);

            Assert.AreEqual(1, svm.Children.Count);
            Assert.AreEqual(typeof(PlaceholderViewModel), svm.Children[0].GetType());
            Assert.AreEqual(SpaceViewModel.EmptyAppsPlaceholderMsg, svm.Children[0].DisplayText);
        }

        [TestMethod]
        public async Task RefreshAllCloudConnections_RemovesPlaceholder_FromEmptyCFsThatGainedChildren()
        {
            var fakeInitialCfInstance = new CloudFoundryInstance("fake cf name", "http://fake.api.address", "fake-token");
            var cfivm = new CfInstanceViewModel(fakeInitialCfInstance, Services);

            // simulate cf initially having no org children
            cfivm.Children = new ObservableCollection<TreeViewItemViewModel> { cfivm.EmptyPlaceholder };
            cfivm.HasEmptyPlaceholder = true;

            var fakeNewOrg = new CloudFoundryOrganization("fake org name", "fake org id", fakeInitialCfInstance);
            var ovm = new OrgViewModel(fakeNewOrg, Services);

            var fakeCfDict = new Dictionary<string, CloudFoundryInstance>
            {
                { "fake cf name", fakeInitialCfInstance },
            };

            var fakeSuccessfulOrgsResult = new DetailedResult<List<CloudFoundryOrganization>>(
                succeeded: true,
                content: new List<CloudFoundryOrganization>
                {
                    fakeNewOrg, // simulate cf having gained an org child before refresh
                },
                explanation: null,
                cmdDetails: FakeSuccessCmdResult);

            _vm.CloudFoundryList = new ObservableCollection<CfInstanceViewModel> { cfivm };

            MockCloudFoundryService.SetupGet(mock => mock.
                CloudFoundryInstances)
                    .Returns(fakeCfDict);

            MockCloudFoundryService.Setup(mock => mock.
                GetOrgsForCfInstanceAsync(fakeInitialCfInstance, true))
                    .ReturnsAsync(fakeSuccessfulOrgsResult);

            Assert.AreEqual(1, cfivm.Children.Count);
            Assert.AreEqual(typeof(PlaceholderViewModel), cfivm.Children[0].GetType());
            Assert.AreEqual(CfInstanceViewModel._emptyOrgsPlaceholderMsg, cfivm.Children[0].DisplayText);

            await _vm.RefreshAllCloudConnections(null);

            Assert.AreEqual(1, cfivm.Children.Count);
            Assert.AreEqual(typeof(OrgViewModel), cfivm.Children[0].GetType());
        }

        [TestMethod]
        public async Task RefreshAllCloudConnections_RemovesPlaceholder_FromEmptyOrgsThatGainedChildren()
        {
            var fakeInitialCfInstance = new CloudFoundryInstance("fake cf name", "http://fake.api.address", "fake-token");
            var cfivm = new CfInstanceViewModel(fakeInitialCfInstance, Services);

            var fakeInitialOrg = new CloudFoundryOrganization("fake org name", "fake org id", fakeInitialCfInstance);
            var ovm = new OrgViewModel(fakeInitialOrg, Services);

            // simulate org initially having no space children
            ovm.Children = new ObservableCollection<TreeViewItemViewModel> { ovm.EmptyPlaceholder };
            ovm.HasEmptyPlaceholder = true;

            var fakeNewSpace = new CloudFoundrySpace("fake space name", "fake space id", fakeInitialOrg);
            var svm = new SpaceViewModel(fakeNewSpace, Services);

            var fakeCfDict = new Dictionary<string, CloudFoundryInstance>
            {
                { "fake cf name", fakeInitialCfInstance },
            };

            var fakeSuccessfulOrgsResult = new DetailedResult<List<CloudFoundryOrganization>>(
                succeeded: true,
                content: new List<CloudFoundryOrganization>
                {
                    fakeInitialOrg,
                },
                explanation: null,
                cmdDetails: FakeSuccessCmdResult);

            var fakeSuccessfulSpacesResult = new DetailedResult<List<CloudFoundrySpace>>(
                succeeded: true,
                content: new List<CloudFoundrySpace>
                {
                    fakeNewSpace, // simulate org having gained a space child before refresh
                },
                explanation: null,
                cmdDetails: FakeSuccessCmdResult);

            MockCloudFoundryService.SetupGet(mock => mock.
                CloudFoundryInstances)
                    .Returns(fakeCfDict);

            MockCloudFoundryService.Setup(mock => mock.
                GetOrgsForCfInstanceAsync(fakeInitialCfInstance, true))
                    .ReturnsAsync(fakeSuccessfulOrgsResult);

            MockCloudFoundryService.Setup(mock => mock.
                GetSpacesForOrgAsync(fakeInitialOrg, true))
                    .ReturnsAsync(fakeSuccessfulSpacesResult);

            cfivm.Children = new ObservableCollection<TreeViewItemViewModel> { ovm };
            ovm.Children = new ObservableCollection<TreeViewItemViewModel> { ovm.EmptyPlaceholder }; // simulate org initially having no space children

            _vm.CloudFoundryList = new ObservableCollection<CfInstanceViewModel> { cfivm };

            Assert.AreEqual(1, ovm.Children.Count);
            Assert.AreEqual(typeof(PlaceholderViewModel), ovm.Children[0].GetType());
            Assert.AreEqual(OrgViewModel._emptySpacesPlaceholderMsg, ovm.Children[0].DisplayText);

            await _vm.RefreshAllCloudConnections(null);

            Assert.AreEqual(1, ovm.Children.Count);
            Assert.AreEqual(typeof(SpaceViewModel), ovm.Children[0].GetType());
        }

        [TestMethod]
        public async Task RefreshAllCloudConnections_RemovesPlaceholder_FromEmptySpacesThatGainedChildren()
        {
            var fakeInitialCfInstance = new CloudFoundryInstance("fake cf name", "http://fake.api.address", "fake-token");
            var cfivm = new CfInstanceViewModel(fakeInitialCfInstance, Services);

            var fakeInitialOrg = new CloudFoundryOrganization("fake org name", "fake org id", fakeInitialCfInstance);
            var ovm = new OrgViewModel(fakeInitialOrg, Services);

            var fakeInitialSpace = new CloudFoundrySpace("fake space name", "fake space id", fakeInitialOrg);
            var svm = new SpaceViewModel(fakeInitialSpace, Services);

            // simulate space initially having no app children
            svm.Children = new ObservableCollection<TreeViewItemViewModel> { svm.EmptyPlaceholder };
            svm.HasEmptyPlaceholder = true;

            var fakeNewApp = new CloudFoundryApp("fake app name", "fake app id", fakeInitialSpace, null);
            var avm = new AppViewModel(fakeNewApp, Services);

            var fakeCfDict = new Dictionary<string, CloudFoundryInstance>
            {
                { "fake cf name", fakeInitialCfInstance },
            };

            var fakeSuccessfulOrgsResult = new DetailedResult<List<CloudFoundryOrganization>>(
                succeeded: true,
                content: new List<CloudFoundryOrganization>
                {
                    fakeInitialOrg,
                },
                explanation: null,
                cmdDetails: FakeSuccessCmdResult);

            var fakeSuccessfulSpacesResult = new DetailedResult<List<CloudFoundrySpace>>(
                succeeded: true,
                content: new List<CloudFoundrySpace>
                {
                    fakeInitialSpace,
                },
                explanation: null,
                cmdDetails: FakeSuccessCmdResult);

            var fakeSuccessfulAppsResult = new DetailedResult<List<CloudFoundryApp>>(
                succeeded: true,
                content: new List<CloudFoundryApp>
                {
                    fakeNewApp, // simulate space having gained an app child before refresh
                },
                explanation: null,
                cmdDetails: FakeSuccessCmdResult);

            MockCloudFoundryService.SetupGet(mock => mock.
                CloudFoundryInstances)
                    .Returns(fakeCfDict);

            MockCloudFoundryService.Setup(mock => mock.
                GetOrgsForCfInstanceAsync(fakeInitialCfInstance, true))
                    .ReturnsAsync(fakeSuccessfulOrgsResult);

            MockCloudFoundryService.Setup(mock => mock.
                GetSpacesForOrgAsync(fakeInitialOrg, true))
                    .ReturnsAsync(fakeSuccessfulSpacesResult);

            MockCloudFoundryService.Setup(mock => mock.
                GetAppsForSpaceAsync(fakeInitialSpace, true))
                    .ReturnsAsync(fakeSuccessfulAppsResult);

            cfivm.Children = new ObservableCollection<TreeViewItemViewModel> { ovm };
            ovm.Children = new ObservableCollection<TreeViewItemViewModel> { svm };
            svm.Children = new ObservableCollection<TreeViewItemViewModel> { svm.EmptyPlaceholder }; // simulate space initially having no app children

            _vm.CloudFoundryList = new ObservableCollection<CfInstanceViewModel> { cfivm };

            Assert.AreEqual(1, svm.Children.Count);
            Assert.AreEqual(typeof(PlaceholderViewModel), svm.Children[0].GetType());
            Assert.AreEqual(SpaceViewModel.EmptyAppsPlaceholderMsg, svm.Children[0].DisplayText);

            await _vm.RefreshAllCloudConnections(null);

            Assert.AreEqual(1, svm.Children.Count);
            Assert.AreEqual(typeof(AppViewModel), svm.Children[0].GetType());
        }

        [TestMethod]
        public async Task RefreshAllCloudConnections_DoesNotThrowExceptions_WhenCfInstancesHaveNoOrgChildren()
        {
            var fakeCfInstance = new CloudFoundryInstance("fake cf name", "http://fake.api.address", "fake-token");

            MockCloudFoundryService.SetupGet(mock => mock.CloudFoundryInstances)
                .Returns(new Dictionary<string, CloudFoundryInstance>
                {
                    { "fake cf name", fakeCfInstance },
                });

            var cfivm = new CfInstanceViewModel(fakeCfInstance, Services);

            // check for presence of Placeholder child (sanity check)
            Assert.AreEqual(1, cfivm.Children.Count);
            Assert.AreEqual(typeof(PlaceholderViewModel), cfivm.Children[0].GetType());

            var fakeSuccessResult = new DetailedResult<List<CloudFoundryOrganization>>(succeeded: true, content: new List<CloudFoundryOrganization>());

            MockCloudFoundryService.Setup(mock => mock.
                GetOrgsForCfInstanceAsync(fakeCfInstance, true))
                    .ReturnsAsync(fakeSuccessResult);

            _vm.CloudFoundryList = new ObservableCollection<CfInstanceViewModel>
            {
                cfivm,
            };

            Exception shouldStayNull = null;
            try
            {
                await _vm.RefreshAllCloudConnections(null);
            }
            catch (Exception e)
            {
                shouldStayNull = e;
            }

            Assert.IsNull(shouldStayNull);

            // check for presence of Placeholder child
            Assert.AreEqual(1, cfivm.Children.Count);
            Assert.AreEqual(typeof(PlaceholderViewModel), cfivm.Children[0].GetType());

            MockCloudFoundryService.VerifyAll();
        }

        [TestMethod]
        public async Task RefreshAllCloudConnections_DoesNotThrowExceptions_WhenOrgsHaveNoSpaceChildren()
        {
            var fakeCfInstance = new CloudFoundryInstance("fake cf name", "http://fake.api.address", "fake-token");

            MockCloudFoundryService.SetupGet(mock => mock.CloudFoundryInstances)
                .Returns(new Dictionary<string, CloudFoundryInstance>
                {
                    { "fake cf name", fakeCfInstance },
                });

            var cfivm = new CfInstanceViewModel(fakeCfInstance, Services);

            var fakeOrg = new CloudFoundryOrganization("fake org name", "fake org id", fakeCfInstance);
            var ovm = new OrgViewModel(fakeOrg, Services);

            cfivm.Children = new ObservableCollection<TreeViewItemViewModel>
            {
                ovm,
            };

            // check for presence of Placeholder child (sanity check)
            Assert.AreEqual(1, ovm.Children.Count);
            Assert.AreEqual(typeof(PlaceholderViewModel), ovm.Children[0].GetType());

            var fakeOrgsList = new List<CloudFoundryOrganization> { fakeOrg };
            var fakeSuccessResult = new DetailedResult<List<CloudFoundryOrganization>>(succeeded: true, content: fakeOrgsList);

            var fakeNoChildrenResponse = new DetailedResult<List<CloudFoundrySpace>>(
                content: new List<CloudFoundrySpace>(),
                succeeded: true,
                explanation: null,
                cmdDetails: FakeSuccessCmdResult);

            MockCloudFoundryService.Setup(mock => mock.
                GetOrgsForCfInstanceAsync(fakeCfInstance, true))
                    .ReturnsAsync(fakeSuccessResult);

            MockCloudFoundryService.Setup(mock => mock.
                GetSpacesForOrgAsync(fakeOrg, true))
                    .ReturnsAsync(fakeNoChildrenResponse);

            _vm.CloudFoundryList = new ObservableCollection<CfInstanceViewModel>
            {
                cfivm,
            };

            Exception shouldStayNull = null;
            try
            {
                await _vm.RefreshAllCloudConnections(null);
            }
            catch (Exception e)
            {
                shouldStayNull = e;
            }

            Assert.IsNull(shouldStayNull);

            Assert.AreEqual(1, cfivm.Children.Count);
            Assert.AreEqual(ovm, cfivm.Children[0]);

            // check for presence of Placeholder child
            Assert.AreEqual(1, ovm.Children.Count);
            Assert.AreEqual(typeof(PlaceholderViewModel), ovm.Children[0].GetType());

            MockCloudFoundryService.VerifyAll();
        }

        [TestMethod]
        public async Task RefreshAllCloudConnections_DoesNotThrowExceptions_WhenSpacesHaveNoAppChildren()
        {
            var fakeCfInstance = new CloudFoundryInstance("fake cf name", "http://fake.api.address", "fake-token");

            MockCloudFoundryService.SetupGet(mock => mock.CloudFoundryInstances)
                .Returns(new Dictionary<string, CloudFoundryInstance>
                {
                    { "fake cf name", fakeCfInstance },
                });

            var cfivm = new CfInstanceViewModel(fakeCfInstance, Services);

            var fakeOrg = new CloudFoundryOrganization("fake org name", "fake org id", fakeCfInstance);
            var ovm = new OrgViewModel(fakeOrg, Services);

            var fakeSpace = new CloudFoundrySpace("fake space name", "fake space id", fakeOrg);
            var svm = new SpaceViewModel(fakeSpace, Services);

            cfivm.Children = new ObservableCollection<TreeViewItemViewModel>
            {
                ovm,
            };

            ovm.Children = new ObservableCollection<TreeViewItemViewModel>
            {
                svm,
            };

            // check for presence of Placeholder child (sanity check)
            Assert.AreEqual(1, svm.Children.Count);
            Assert.AreEqual(typeof(PlaceholderViewModel), svm.Children[0].GetType());

            var fakeOrgsList = new List<CloudFoundryOrganization> { fakeOrg };
            var fakeSuccessResult = new DetailedResult<List<CloudFoundryOrganization>>(succeeded: true, content: fakeOrgsList);

            var fakeSpacesList = new List<CloudFoundrySpace> { fakeSpace };
            var fakeSpacesResponse = new DetailedResult<List<CloudFoundrySpace>>(
                content: fakeSpacesList,
                succeeded: true,
                explanation: null,
                cmdDetails: FakeSuccessCmdResult);

            var emptyAppsList = new List<CloudFoundryApp>();
            var fakeSuccessfulAppsResult = new DetailedResult<List<CloudFoundryApp>>(
                content: emptyAppsList,
                succeeded: true,
                explanation: null,
                cmdDetails: FakeSuccessCmdResult);

            MockCloudFoundryService.Setup(mock => mock.
                GetOrgsForCfInstanceAsync(fakeCfInstance, true))
                    .ReturnsAsync(fakeSuccessResult);

            MockCloudFoundryService.Setup(mock => mock.
                GetSpacesForOrgAsync(fakeOrg, true))
                    .ReturnsAsync(fakeSpacesResponse);

            MockCloudFoundryService.Setup(mock => mock.
                GetAppsForSpaceAsync(fakeSpace, true))
                    .ReturnsAsync(fakeSuccessfulAppsResult);

            _vm.CloudFoundryList = new ObservableCollection<CfInstanceViewModel>
            {
                cfivm,
            };

            Exception shouldStayNull = null;
            try
            {
                await _vm.RefreshAllCloudConnections(null);
            }
            catch (Exception e)
            {
                shouldStayNull = e;
            }

            Assert.IsNull(shouldStayNull);

            Assert.AreEqual(1, cfivm.Children.Count);
            Assert.AreEqual(ovm, cfivm.Children[0]);

            Assert.AreEqual(1, ovm.Children.Count);
            Assert.AreEqual(svm, ovm.Children[0]);

            // check for presence of Placeholder child
            Assert.AreEqual(1, svm.Children.Count);
            Assert.AreEqual(typeof(PlaceholderViewModel), svm.Children[0].GetType());

            MockCloudFoundryService.VerifyAll();
        }

        [TestMethod]
        public async Task DisplayRecentAppLogs_LogsError_WhenArgumentTypeIsNotApp()
        {
            await _vm.DisplayRecentAppLogs(new object());
            MockLogger.Verify(m => m.Error(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
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

            await _vm.DisplayRecentAppLogs(fakeApp);

            MockLogger.Verify(m => m.
                Error(It.Is<string>(s => s.Contains(fakeLogsResult.Explanation))),
                    Times.Once);

            MockErrorDialogService.Verify(m => m.
                DisplayErrorDialog(It.Is<string>(s => s.Contains(fakeApp.AppName)), It.Is<string>(s => s.Contains(fakeLogsResult.Explanation))),
                    Times.Once);
        }

        [TestMethod]
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

            await _vm.DisplayRecentAppLogs(fakeApp);

            Assert.IsTrue(fakeView.ShowMethodWasCalled);
        }
    }
}
