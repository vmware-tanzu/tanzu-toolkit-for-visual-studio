using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using Tanzu.Toolkit.VisualStudio.Models;
using Tanzu.Toolkit.VisualStudio.Services;

namespace Tanzu.Toolkit.VisualStudio.ViewModels.Tests
{
    [TestClass]
    public class CloudExplorerViewModelTests : ViewModelTestSupport
    {
        private CloudExplorerViewModel vm;
        List<string> receivedEvents;

        [TestInitialize]
        public void TestInit()
        {
            mockCloudFoundryService.SetupGet(mock => mock.CloudFoundryInstances).Returns(new Dictionary<string, CloudFoundryInstance>());
            vm = new CloudExplorerViewModel(services);
            receivedEvents = new List<string>();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            mockCloudFoundryService.VerifyAll();
        }

        [TestMethod]
        public void CanOpenLoginView_ReturnsExpected()
        {
            Assert.IsTrue(vm.CanOpenLoginView(null));
        }

        [TestMethod]
        public void CanStopCfApp_ReturnsTrue()
        {
            Assert.IsTrue(vm.CanStopCfApp(null));
        }

        [TestMethod]
        public void CanStartCfApp_ReturnsTrue()
        {
            Assert.IsTrue(vm.CanStartCfApp(null));
        }

        [TestMethod]
        public void CanDeleteCfApp_ReturnsTrue()
        {
            Assert.IsTrue(vm.CanDeleteCfApp(null));

        }

        [TestMethod]
        public void CanRefreshCfInstance_ReturnsTrue()
        {
            Assert.IsTrue(vm.CanRefreshCfInstance(null));
        }

        [TestMethod]
        public void CanRefreshOrg_ReturnsTrue()
        {
            Assert.IsTrue(vm.CanRefreshOrg(null));
        }

        [TestMethod]
        public void CanRefreshSpace_ReturnsTrue()
        {
            Assert.IsTrue(vm.CanRefreshSpace(null));
        }

        [TestMethod]
        public void CanRefreshApp_ReturnsTrue()
        {
            Assert.IsTrue(vm.CanRefreshApp(null));
        }

        [TestMethod]
        public void CanRefreshAllCloudConnections_ReturnsTrue()
        {
            Assert.IsTrue(vm.CanRefreshAllCloudConnections(null));
        }


        [TestMethod]
        public void OpenLoginView_CallsDialogService_ShowDialog()
        {
            vm.OpenLoginView(null);
            mockDialogService.Verify(ds => ds.ShowDialog(typeof(AddCloudDialogViewModel).Name, null), Times.Once);
        }

        [TestMethod]
        public void OpenLoginView_UpdatesCloudFoundryInstances_AfterDialogCloses()
        {
            var emptyCfsDict = new Dictionary<string, CloudFoundryInstance>();
            var fakeCfsDict = new Dictionary<string, CloudFoundryInstance>
            {
                { "fake cf", new CloudFoundryInstance("fake cf", null, null) }
            };

            mockCloudFoundryService.SetupSequence(mock => mock.CloudFoundryInstances)
                .Returns(emptyCfsDict) // return empty on first request to avoid error due to temporary "single-cf" requirement.
                .Returns(fakeCfsDict); // return fake cf on second request as mock result of having logged in.

            Assert.AreEqual(0, vm.CloudFoundryList.Count);

            vm.OpenLoginView(null);

            Assert.IsTrue(vm.HasCloudTargets);
            Assert.AreEqual(1, vm.CloudFoundryList.Count);
        }

        [TestMethod]
        public async Task StopCfApp_CallsStopCfAppAsync()
        {
            var fakeApp = new CloudFoundryApp("junk", "junk", parentSpace: null, null);

            mockCloudFoundryService.Setup(mock => mock.StopAppAsync(fakeApp, true)).ReturnsAsync(fakeSuccessDetailedResult);

            Exception shouldStayNull = null;
            try
            {
                await vm.StopCfApp(fakeApp);
            }
            catch (Exception e)
            {
                shouldStayNull = e;
            }

            Assert.IsNull(shouldStayNull);
            mockCloudFoundryService.VerifyAll();
        }

        [TestMethod]
        public async Task StopCfApp_DisplaysErrorDialog_WhenStopAppAsyncFails()
        {
            var fakeApp = new CloudFoundryApp("junk", "junk", parentSpace: null, null);

            mockCloudFoundryService.Setup(mock => mock.
                StopAppAsync(fakeApp, true))
                    .ReturnsAsync(fakeFailureDetailedResult);

            await vm.StopCfApp(fakeApp);

            var expectedErrorTitle = $"{CloudExplorerViewModel._stopAppErrorMsg} {fakeApp.AppName}.";
            var expectedErrorMsg = fakeFailureDetailedResult.Explanation;

            mockDialogService.Verify(mock => mock.
              DisplayErrorDialog(expectedErrorTitle, expectedErrorMsg),
                Times.Once);
        }

        [TestMethod]
        public async Task StopCfApp_LogsError_WhenStopAppAsyncFails()
        {
            var fakeApp = new CloudFoundryApp("junk", "junk", parentSpace: null, null);

            mockCloudFoundryService.Setup(mock => mock.
                StopAppAsync(fakeApp, true))
                    .ReturnsAsync(fakeFailureDetailedResult);

            await vm.StopCfApp(fakeApp);

            var logPropVal1 = "{AppName}";
            var logPropVal2 = "{StopResult}";
            var expectedLogMsg = $"{CloudExplorerViewModel._stopAppErrorMsg} {logPropVal1}. {logPropVal2}";

            mockLogger.Verify(m => m.
                Error(expectedLogMsg, fakeApp.AppName, fakeFailureDetailedResult.ToString()),
                    Times.Once);
        }

        [TestMethod]
        public async Task StartCfApp_CallsStartAppAsync()
        {
            var fakeApp = new CloudFoundryApp("junk", "junk", parentSpace: null, null);

            mockCloudFoundryService.Setup(mock => mock.StartAppAsync(fakeApp, true)).ReturnsAsync(fakeSuccessDetailedResult);

            Exception shouldStayNull = null;
            try
            {
                await vm.StartCfApp(fakeApp);
            }
            catch (Exception e)
            {
                shouldStayNull = e;
            }

            Assert.IsNull(shouldStayNull);
            mockCloudFoundryService.VerifyAll();
        }

        [TestMethod]
        public async Task StartCfApp_DisplaysErrorDialog_WhenStartAppAsyncFails()
        {
            var fakeApp = new CloudFoundryApp("junk", "junk", parentSpace: null, null);

            mockCloudFoundryService.Setup(mock => mock.
                StartAppAsync(fakeApp, true))
                    .ReturnsAsync(fakeFailureDetailedResult);

            await vm.StartCfApp(fakeApp);

            var expectedErrorTitle = $"{CloudExplorerViewModel._startAppErrorMsg} {fakeApp.AppName}.";
            var expectedErrorMsg = fakeFailureDetailedResult.Explanation;

            mockDialogService.Verify(mock => mock.
              DisplayErrorDialog(expectedErrorTitle, expectedErrorMsg),
                Times.Once);
        }

        [TestMethod]
        public async Task StartCfApp_LogsError_WhenStartAppAsyncFails()
        {
            var fakeApp = new CloudFoundryApp("junk", "junk", parentSpace: null, null);

            mockCloudFoundryService.Setup(mock => mock.
                StartAppAsync(fakeApp, true))
                    .ReturnsAsync(fakeFailureDetailedResult);

            await vm.StartCfApp(fakeApp);

            var logPropVal1 = "{AppName}";
            var logPropVal2 = "{StartResult}";
            var expectedLogMsg = $"{CloudExplorerViewModel._startAppErrorMsg} {logPropVal1}. {logPropVal2}";

            mockLogger.Verify(m => m.
                Error(expectedLogMsg, fakeApp.AppName, fakeFailureDetailedResult.ToString()),
                    Times.Once);
        }

        [TestMethod]
        public async Task DeleteCfApp_CallsDeleteAppAsync()
        {
            var fakeApp = new CloudFoundryApp("junk", "junk", parentSpace: null, null);

            mockCloudFoundryService.Setup(mock => mock.DeleteAppAsync(fakeApp, true, true)).ReturnsAsync(fakeSuccessDetailedResult);

            Exception shouldStayNull = null;
            try
            {
                await vm.DeleteCfApp(fakeApp);
            }
            catch (Exception e)
            {
                shouldStayNull = e;
            }

            Assert.IsNull(shouldStayNull);
            mockCloudFoundryService.VerifyAll();
        }

        [TestMethod]
        public async Task DeleteCfApp_DisplaysErrorDialog_WhenDeleteAppAsyncFails()
        {
            var fakeApp = new CloudFoundryApp("junk", "junk", parentSpace: null, null);

            mockCloudFoundryService.Setup(mock => mock.
                DeleteAppAsync(fakeApp, true, true))
                    .ReturnsAsync(fakeFailureDetailedResult);

            await vm.DeleteCfApp(fakeApp);

            var expectedErrorTitle = $"{CloudExplorerViewModel._deleteAppErrorMsg} {fakeApp.AppName}.";
            var expectedErrorMsg = fakeFailureDetailedResult.Explanation;

            mockDialogService.Verify(mock => mock.
              DisplayErrorDialog(expectedErrorTitle, expectedErrorMsg),
                Times.Once);

        }

        [TestMethod]
        public async Task DeleteCfApp_LogsError_WhenDeleteAppAsyncFails()
        {
            var fakeApp = new CloudFoundryApp("junk", "junk", parentSpace: null, null);

            mockCloudFoundryService.Setup(mock => mock.
                DeleteAppAsync(fakeApp, true, true))
                    .ReturnsAsync(fakeFailureDetailedResult);

            await vm.DeleteCfApp(fakeApp);

            var logPropVal1 = "{AppName}";
            var logPropVal2 = "{DeleteResult}";
            var expectedLogMsg = $"{CloudExplorerViewModel._deleteAppErrorMsg} {logPropVal1}. {logPropVal2}";

            mockLogger.Verify(m => m.
                Error(expectedLogMsg, fakeApp.AppName, fakeFailureDetailedResult.ToString()),
                    Times.Once);
        }

        [TestMethod]
        public void RefreshApp_RaisesPropertyChangedEventForIsStopped()
        {
            CloudFoundryApp fakeApp = new CloudFoundryApp("fake app name", "fake app guid", null, null);
            var avm = new AppViewModel(fakeApp, services);

            avm.PropertyChanged += delegate (object sender, PropertyChangedEventArgs e)
            {
                receivedEvents.Add(e.PropertyName);
            };

            vm.RefreshApp(avm);

            Assert.AreEqual(1, receivedEvents.Count);
            Assert.AreEqual("IsStopped", receivedEvents[0]);
        }

        [TestMethod]
        public async Task RefreshCfInstance_UpdatesChildrenOnCfInstanceViewModel()
        {
            var fakeCfInstance = new CloudFoundryInstance("fake name", "fake id", null);
            var fakeCfInstanceViewModel = new CfInstanceViewModel(fakeCfInstance, services);

            var fakeOrgName1 = "fake org 1";
            var fakeOrgName2 = "fake org 2";
            var fakeOrgName3 = "fake org 3";

            var fakeOrgGuid1 = "fake org 1";
            var fakeOrgGuid2 = "fake org 2";
            var fakeOrgGuid3 = "fake org 3";

            fakeCfInstanceViewModel.Children = new ObservableCollection<TreeViewItemViewModel>
            {
                new OrgViewModel(new CloudFoundryOrganization(fakeOrgName1, fakeOrgGuid1, fakeCfInstance), services)
            };

            fakeCfInstanceViewModel.PropertyChanged += delegate (object sender, PropertyChangedEventArgs e)
            {
                receivedEvents.Add(e.PropertyName);
            };

            var fakeOrgsList = new List<CloudFoundryOrganization>
            {
                new CloudFoundryOrganization(fakeOrgName2, fakeOrgGuid2, fakeCfInstance),
                new CloudFoundryOrganization(fakeOrgName3, fakeOrgGuid3, fakeCfInstance)
            };

            var fakeSuccessResult = new DetailedResult<List<CloudFoundryOrganization>>(succeeded: true, content: fakeOrgsList);

            Assert.AreEqual(1, fakeCfInstanceViewModel.Children.Count);
            OrgViewModel firstChildOrg = (OrgViewModel)fakeCfInstanceViewModel.Children[0];
            Assert.AreEqual(fakeOrgName1, firstChildOrg.Org.OrgName);

            mockCloudFoundryService.Setup(mock => mock.
                GetOrgsForCfInstanceAsync(fakeCfInstance, true))
                    .ReturnsAsync(fakeSuccessResult);

            await vm.RefreshCfInstance(fakeCfInstanceViewModel);

            Assert.AreEqual(2, fakeCfInstanceViewModel.Children.Count);
            OrgViewModel firstNewChildOrg = (OrgViewModel)fakeCfInstanceViewModel.Children[0];
            OrgViewModel secondNewChildOrg = (OrgViewModel)fakeCfInstanceViewModel.Children[1];
            Assert.AreEqual(fakeOrgName2, firstNewChildOrg.Org.OrgName);
            Assert.AreEqual(fakeOrgName3, secondNewChildOrg.Org.OrgName);

            // property changed events should not be raised
            Assert.AreEqual(0, receivedEvents.Count);

            mockCloudFoundryService.VerifyAll();
        }
        
        [TestMethod]
        public async Task RefreshCfInstance_AddsPlaceholder_ToCFsThatBecameEmpty()
        {
            var fakeCfInstance = new CloudFoundryInstance("fake name", "fake id", null);
            var fakeCfInstanceViewModel = new CfInstanceViewModel(fakeCfInstance, services);

            var fakeOrgName1 = "fake org 1";
            var fakeOrgGuid1 = "fake org 1";

            fakeCfInstanceViewModel.Children = new ObservableCollection<TreeViewItemViewModel>
            {
                new OrgViewModel(new CloudFoundryOrganization(fakeOrgName1, fakeOrgGuid1, fakeCfInstance), services)
            };

            var newEmptyOrgsList = new List<CloudFoundryOrganization>();

            var fakeNoOrgsResult = new DetailedResult<List<CloudFoundryOrganization>>(succeeded: true, content: newEmptyOrgsList);

            Assert.AreEqual(1, fakeCfInstanceViewModel.Children.Count);
            OrgViewModel firstChildOrg = (OrgViewModel)fakeCfInstanceViewModel.Children[0];
            Assert.AreEqual(fakeOrgName1, firstChildOrg.Org.OrgName);

            mockCloudFoundryService.Setup(mock => mock.
                GetOrgsForCfInstanceAsync(fakeCfInstance, true))
                    .ReturnsAsync(fakeNoOrgsResult);

            await vm.RefreshCfInstance(fakeCfInstanceViewModel);

            Assert.AreEqual(1, fakeCfInstanceViewModel.Children.Count);
            Assert.AreEqual(typeof(PlaceholderViewModel), fakeCfInstanceViewModel.Children[0].GetType());
        }
        
        [TestMethod]
        public async Task RefreshCfInstance_RemovesPlaceholder_FromEmptyCFsThatGainedChildren()
        {
            var fakeCfInstance = new CloudFoundryInstance("fake name", "fake id", null);
            var fakeCfInstanceViewModel = new CfInstanceViewModel(fakeCfInstance, services);

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

            mockCloudFoundryService.Setup(mock => mock.
                GetOrgsForCfInstanceAsync(fakeCfInstance, true))
                    .ReturnsAsync(fakeSuccessResult);

            Assert.AreEqual(1, fakeCfInstanceViewModel.Children.Count);
            Assert.AreEqual(typeof(PlaceholderViewModel), fakeCfInstanceViewModel.Children[0].GetType());

            await vm.RefreshCfInstance(fakeCfInstanceViewModel);

            Assert.AreEqual(1, fakeCfInstanceViewModel.Children.Count);
            var child = fakeCfInstanceViewModel.Children[0];
            Assert.AreEqual(typeof(OrgViewModel), child.GetType());
        }

        [TestMethod]
        public async Task RefreshOrg_UpdatesChildrenOnOrgViewModel()
        {
            var fakeOrg = new CloudFoundryOrganization("fake org name", "fake org id", null);
            var fakeOrgViewModel = new OrgViewModel(fakeOrg, services);

            var fakeSpaceName1 = "fake space 1";
            var fakeSpaceName2 = "fake space 2";
            var fakeSpaceName3 = "fake space 3";

            var fakeSpaceGuid1 = "fake space 1";
            var fakeSpaceGuid2 = "fake space 2";
            var fakeSpaceGuid3 = "fake space 3";

            fakeOrgViewModel.Children = new ObservableCollection<TreeViewItemViewModel>
            {
                new SpaceViewModel(new CloudFoundrySpace(fakeSpaceName1, fakeSpaceGuid1, fakeOrg), services)
            };

            fakeOrgViewModel.PropertyChanged += delegate (object sender, PropertyChangedEventArgs e)
            {
                receivedEvents.Add(e.PropertyName);
            };

            Assert.AreEqual(1, fakeOrgViewModel.Children.Count);
            SpaceViewModel firstChildSpace = (SpaceViewModel)fakeOrgViewModel.Children[0];
            Assert.AreEqual(fakeSpaceName1, firstChildSpace.Space.SpaceName);

            List<CloudFoundrySpace> fakeSpaceList = new List<CloudFoundrySpace>
            {
                new CloudFoundrySpace(fakeSpaceName2, fakeSpaceGuid2, fakeOrg),
                new CloudFoundrySpace(fakeSpaceName3, fakeSpaceGuid3, fakeOrg)
            };

            var fakeSuccessResponse = new DetailedResult<List<CloudFoundrySpace>>
            (
                content: fakeSpaceList,
                succeeded: true,
                explanation: null,
                cmdDetails: fakeSuccessCmdResult
            );

            mockCloudFoundryService.Setup(mock => mock.
                GetSpacesForOrgAsync(fakeOrg, true))
                    .ReturnsAsync(fakeSuccessResponse);

            await vm.RefreshOrg(fakeOrgViewModel);

            Assert.AreEqual(2, fakeOrgViewModel.Children.Count);
            SpaceViewModel firstNewChildSpace = (SpaceViewModel)fakeOrgViewModel.Children[0];
            SpaceViewModel secondNewChildSpace = (SpaceViewModel)fakeOrgViewModel.Children[1];
            Assert.AreEqual(fakeSpaceName2, firstNewChildSpace.Space.SpaceName);
            Assert.AreEqual(fakeSpaceName3, secondNewChildSpace.Space.SpaceName);

            // property changed events should not be raised
            Assert.AreEqual(0, receivedEvents.Count);

            mockCloudFoundryService.VerifyAll();
        }
        
        [TestMethod]
        public async Task RefreshOrg_AddsPlaceholder_ToOrgsThatBecameEmpty()
        {
            var fakeOrg = new CloudFoundryOrganization("fake name", "fake id", null);
            var fakeOrgViewModel = new OrgViewModel(fakeOrg, services);

            var fakeSpaceName1 = "fake space 1";
            var fakeSpaceGuid1 = "fake space 1";

            fakeOrgViewModel.Children = new ObservableCollection<TreeViewItemViewModel>
            {
                new SpaceViewModel(new CloudFoundrySpace(fakeSpaceName1, fakeSpaceGuid1, fakeOrg), services)
            };

            var newEmptySpacesList = new List<CloudFoundrySpace>();

            var fakeNoSpacesResult = new DetailedResult<List<CloudFoundrySpace>>(succeeded: true, content: newEmptySpacesList);

            Assert.AreEqual(1, fakeOrgViewModel.Children.Count);
            SpaceViewModel firstChildSpace = (SpaceViewModel)fakeOrgViewModel.Children[0];
            Assert.AreEqual(fakeSpaceName1, firstChildSpace.Space.SpaceName);

            mockCloudFoundryService.Setup(mock => mock.
                GetSpacesForOrgAsync(fakeOrg, true))
                    .ReturnsAsync(fakeNoSpacesResult);

            await vm.RefreshOrg(fakeOrgViewModel);

            Assert.AreEqual(1, fakeOrgViewModel.Children.Count);
            Assert.AreEqual(typeof(PlaceholderViewModel), fakeOrgViewModel.Children[0].GetType());
        }
        
        [TestMethod]
        public async Task RefreshOrg_RemovesPlaceholder_FromEmptyOrgsThatGainedChildren()
        {
            var fakeOrg = new CloudFoundryOrganization("fake name", "fake id", null);
            var fakeOrgViewModel = new OrgViewModel(fakeOrg, services);

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

            mockCloudFoundryService.Setup(mock => mock.
                GetSpacesForOrgAsync(fakeOrg, true))
                    .ReturnsAsync(fakeSuccessResult);

            Assert.AreEqual(1, fakeOrgViewModel.Children.Count);
            Assert.AreEqual(typeof(PlaceholderViewModel), fakeOrgViewModel.Children[0].GetType());

            await vm.RefreshOrg(fakeOrgViewModel);

            Assert.AreEqual(1, fakeOrgViewModel.Children.Count);
            var child = fakeOrgViewModel.Children[0];
            Assert.AreEqual(typeof(SpaceViewModel), child.GetType());
        }

        [TestMethod]
        public async Task RefreshSpace_UpdatesChildrenOnSpaceViewModel()
        {
            var fakeSpace = new CloudFoundrySpace("fake space name", "fake space id", null);
            var fakeSpaceViewModel = new SpaceViewModel(fakeSpace, services);

            var fakeAppName1 = "fake app 1";
            var fakeAppName2 = "fake app 2";
            var fakeAppName3 = "fake app 3";

            var fakeAppGuid1 = "fake app 1";
            var fakeAppGuid2 = "fake app 2";
            var fakeAppGuid3 = "fake app 3";

            fakeSpaceViewModel.Children = new ObservableCollection<TreeViewItemViewModel>
            {
                new AppViewModel(new CloudFoundryApp(fakeAppName1, fakeAppGuid1, fakeSpace, null), services)
            };

            fakeSpaceViewModel.PropertyChanged += delegate (object sender, PropertyChangedEventArgs e)
            {
                receivedEvents.Add(e.PropertyName);
            };

            Assert.AreEqual(1, fakeSpaceViewModel.Children.Count);
            AppViewModel firstChildApp = (AppViewModel)fakeSpaceViewModel.Children[0];
            Assert.AreEqual(fakeAppName1, firstChildApp.App.AppName);

            var fakeAppsResponseContent = new List<CloudFoundryApp>
            {
                new CloudFoundryApp(fakeAppName2, fakeAppGuid2, fakeSpace, null),
                new CloudFoundryApp(fakeAppName3, fakeAppGuid3, fakeSpace, null)
            };
            var fakeAppsResult = new DetailedResult<List<CloudFoundryApp>>(
                succeeded: true,
                content: fakeAppsResponseContent,
                explanation: null,
                cmdDetails: fakeSuccessCmdResult);

            mockCloudFoundryService.Setup(mock => mock.
                GetAppsForSpaceAsync(fakeSpace, true))
                    .ReturnsAsync(fakeAppsResult);

            await vm.RefreshSpace(fakeSpaceViewModel);

            Assert.AreEqual(2, fakeSpaceViewModel.Children.Count);
            AppViewModel firstNewChildApp = (AppViewModel)fakeSpaceViewModel.Children[0];
            AppViewModel secondNewChildApp = (AppViewModel)fakeSpaceViewModel.Children[1];
            Assert.AreEqual(fakeAppName2, firstNewChildApp.App.AppName);
            Assert.AreEqual(fakeAppName3, secondNewChildApp.App.AppName);

            // property changed events should not be raised
            Assert.AreEqual(0, receivedEvents.Count);

            mockCloudFoundryService.VerifyAll();
        }
        
        [TestMethod]
        public async Task RefreshSpace_AddsPlaceholder_ToSpacesThatBecameEmpty()
        {
            var fakeSpace = new CloudFoundrySpace("fake name", "fake id", null);
            var fakeSpaceViewModel = new SpaceViewModel(fakeSpace, services);

            var fakeAppName1 = "fake app 1";
            var fakeAppGuid1 = "fake app 1";

            fakeSpaceViewModel.Children = new ObservableCollection<TreeViewItemViewModel>
            {
                new AppViewModel(new CloudFoundryApp(fakeAppName1, fakeAppGuid1, fakeSpace, null), services)
            };

            var newEmptyAppsList = new List<CloudFoundryApp>();

            var fakeNoAppsResult = new DetailedResult<List<CloudFoundryApp>>(succeeded: true, content: newEmptyAppsList);

            Assert.AreEqual(1, fakeSpaceViewModel.Children.Count);
            AppViewModel firstChildApp = (AppViewModel)fakeSpaceViewModel.Children[0];
            Assert.AreEqual(fakeAppName1, firstChildApp.App.AppName);

            mockCloudFoundryService.Setup(mock => mock.
                GetAppsForSpaceAsync(fakeSpace, true))
                    .ReturnsAsync(fakeNoAppsResult);

            await vm.RefreshSpace(fakeSpaceViewModel);

            Assert.AreEqual(1, fakeSpaceViewModel.Children.Count);
            Assert.AreEqual(typeof(PlaceholderViewModel), fakeSpaceViewModel.Children[0].GetType());
        }
        
        [TestMethod]
        public async Task RefreshSpace_RemovesPlaceholder_FromEmptySpacesThatGainedChildren()
        {
            var fakeSpace = new CloudFoundrySpace("fake name", "fake id", null);
            var fakeSpaceViewModel = new SpaceViewModel(fakeSpace, services);

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

            mockCloudFoundryService.Setup(mock => mock.
                GetAppsForSpaceAsync(fakeSpace, true))
                    .ReturnsAsync(fakeSuccessResult);

            Assert.AreEqual(1, fakeSpaceViewModel.Children.Count);
            Assert.AreEqual(typeof(PlaceholderViewModel), fakeSpaceViewModel.Children[0].GetType());

            await vm.RefreshSpace(fakeSpaceViewModel);

            Assert.AreEqual(1, fakeSpaceViewModel.Children.Count);
            var child = fakeSpaceViewModel.Children[0];
            Assert.AreEqual(typeof(AppViewModel), child.GetType());
        }

        [TestMethod]
        public async Task RefreshAllCloudConnections_RefreshesEachTreeViewItemViewModel()
        {
            var fakeInitialCfInstance = new CloudFoundryInstance("fake cf name", "http://fake.api.address", "fake-token");
            var fakeNewCfInstance = new CloudFoundryInstance("new cf", "http://new.api.address", "new-token");
            var cfivm = new CfInstanceViewModel(fakeInitialCfInstance, services);

            var fakeInitialOrg = new CloudFoundryOrganization("fake org name", "fake org id", fakeInitialCfInstance);
            var fakeNewOrg = new CloudFoundryOrganization("new org", "new org id", fakeInitialCfInstance);
            var ovm = new OrgViewModel(fakeInitialOrg, services);

            var fakeInitialSpace = new CloudFoundrySpace("fake space name", "fake space id", fakeInitialOrg);
            var fakeNewSpace = new CloudFoundrySpace("new space", "new space id", fakeInitialOrg);
            var svm = new SpaceViewModel(fakeInitialSpace, services);

            var fakeInitialApp = new CloudFoundryApp("fake app name", "fake app id", fakeInitialSpace, null);
            var fakeNewApp = new CloudFoundryApp("new app", "new app id", fakeInitialSpace, null);
            var avm = new AppViewModel(fakeInitialApp, services);

            var fakeCfDict = new Dictionary<string, CloudFoundryInstance>
            {
                { "fake cf name", fakeInitialCfInstance },
                { "new cf name", fakeNewCfInstance },
            };

            var fakeSuccessfulOrgsResult = new DetailedResult<List<CloudFoundryOrganization>>
            (
                succeeded: true,
                content: new List<CloudFoundryOrganization>
                {
                    fakeInitialOrg,
                    fakeNewOrg
                },
                explanation: null,
                cmdDetails: fakeSuccessCmdResult
            );

            var fakeSuccessfulSpacesResult = new DetailedResult<List<CloudFoundrySpace>>
            (
                succeeded: true,
                content: new List<CloudFoundrySpace>
                {
                    fakeInitialSpace,
                    fakeNewSpace
                },
                explanation: null,
                cmdDetails: fakeSuccessCmdResult
            );

            var fakeSuccessfulAppsResult = new DetailedResult<List<CloudFoundryApp>>
            (
                succeeded: true,
                content: new List<CloudFoundryApp>
                {
                    fakeInitialApp,
                    fakeNewApp
                },
                explanation: null,
                cmdDetails: fakeSuccessCmdResult
            );

            mockCloudFoundryService.SetupGet(mock => mock.
                CloudFoundryInstances)
                    .Returns(fakeCfDict);

            mockCloudFoundryService.Setup(mock => mock.
                GetOrgsForCfInstanceAsync(fakeInitialCfInstance, true))
                    .ReturnsAsync(fakeSuccessfulOrgsResult);

            mockCloudFoundryService.Setup(mock => mock.
                GetSpacesForOrgAsync(fakeInitialOrg, true))
                    .ReturnsAsync(fakeSuccessfulSpacesResult);

            mockCloudFoundryService.Setup(mock => mock.
                GetAppsForSpaceAsync(fakeInitialSpace, true))
                    .ReturnsAsync(fakeSuccessfulAppsResult);



            cfivm.Children = new ObservableCollection<TreeViewItemViewModel> { ovm };

            ovm.Children = new ObservableCollection<TreeViewItemViewModel> { svm };

            svm.Children = new ObservableCollection<TreeViewItemViewModel> { avm };

            vm.CloudFoundryList = new ObservableCollection<CfInstanceViewModel> { cfivm };

            var eventsRaisedByAVM = new List<string>();
            avm.PropertyChanged += delegate (object sender, PropertyChangedEventArgs e)
            {
                eventsRaisedByAVM.Add(e.PropertyName);
            };

            await vm.RefreshAllCloudConnections(null);

            Assert.AreEqual(2, vm.CloudFoundryList.Count);
            CfInstanceViewModel firstCfVm = vm.CloudFoundryList[0];
            CfInstanceViewModel secondCfVm = vm.CloudFoundryList[1];
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
            mockCloudFoundryService.Verify(mock => mock.
                GetOrgsForCfInstanceAsync(fakeNewCfInstance, true), Times.Never);

            // No need to get children for orgs that were just added by refresh.
            mockCloudFoundryService.Verify(mock => mock.
                GetSpacesForOrgAsync(fakeNewOrg, true), Times.Never);

            // No need to get children for spaces that were just added by refresh.
            mockCloudFoundryService.Verify(mock => mock.
                GetAppsForSpaceAsync(fakeNewSpace, true), Times.Never);
        }

        [TestMethod]
        public async Task RefreshAllCloudConnections_AddsPlaceholder_ToCFsThatBecameEmpty()
        {
            var fakeInitialCfInstance = new CloudFoundryInstance("fake cf name", "http://fake.api.address", "fake-token");
            var cfivm = new CfInstanceViewModel(fakeInitialCfInstance, services);

            var fakeInitialOrg = new CloudFoundryOrganization("fake org name", "fake org id", fakeInitialCfInstance);
            var ovm = new OrgViewModel(fakeInitialOrg, services);

            var fakeCfDict = new Dictionary<string, CloudFoundryInstance>
            {
                { "fake cf name", fakeInitialCfInstance },
            };

            var fakeEmptyOrgsResult = new DetailedResult<List<CloudFoundryOrganization>>
            (
                succeeded: true,
                content: new List<CloudFoundryOrganization>(), // simulate cf having lost all orgs before refresh
                explanation: null,
                cmdDetails: fakeSuccessCmdResult
            );

            mockCloudFoundryService.SetupGet(mock => mock.
                CloudFoundryInstances)
                    .Returns(fakeCfDict);

            mockCloudFoundryService.Setup(mock => mock.
                GetOrgsForCfInstanceAsync(fakeInitialCfInstance, true))
                    .ReturnsAsync(fakeEmptyOrgsResult);

            cfivm.Children = new ObservableCollection<TreeViewItemViewModel> { ovm }; // simulate cf initially having 1 org child

            vm.CloudFoundryList = new ObservableCollection<CfInstanceViewModel> { cfivm };

            Assert.AreEqual(1, cfivm.Children.Count);
            Assert.AreEqual(typeof(OrgViewModel), cfivm.Children[0].GetType());

            await vm.RefreshAllCloudConnections(null);

            Assert.AreEqual(1, cfivm.Children.Count);

            Assert.AreEqual(typeof(PlaceholderViewModel), cfivm.Children[0].GetType());
            Assert.AreEqual(CfInstanceViewModel._emptyOrgsPlaceholderMsg, cfivm.Children[0].DisplayText);
        }

        [TestMethod]
        public async Task RefreshAllCloudConnections_AddsPlaceholder_ToOrgsThatBecameEmpty()
        {
            var fakeInitialCfInstance = new CloudFoundryInstance("fake cf name", "http://fake.api.address", "fake-token");
            var cfivm = new CfInstanceViewModel(fakeInitialCfInstance, services);

            var fakeInitialOrg = new CloudFoundryOrganization("fake org name", "fake org id", fakeInitialCfInstance);
            var ovm = new OrgViewModel(fakeInitialOrg, services);

            var fakeInitialSpace = new CloudFoundrySpace("fake space name", "fake space id", fakeInitialOrg);
            var svm = new SpaceViewModel(fakeInitialSpace, services);

            var fakeCfDict = new Dictionary<string, CloudFoundryInstance>
            {
                { "fake cf name", fakeInitialCfInstance },
            };

            var fakeSuccessfulOrgsResult = new DetailedResult<List<CloudFoundryOrganization>>
            (
                succeeded: true,
                content: new List<CloudFoundryOrganization>
                {
                    fakeInitialOrg,
                },
                explanation: null,
                cmdDetails: fakeSuccessCmdResult
            );

            var fakeNoSpacesResult = new DetailedResult<List<CloudFoundrySpace>>
            (
                succeeded: true,
                content: new List<CloudFoundrySpace>(), // simulate org having lost all spaces before refresh
                explanation: null,
                cmdDetails: fakeSuccessCmdResult
            );

            mockCloudFoundryService.SetupGet(mock => mock.
                CloudFoundryInstances)
                    .Returns(fakeCfDict);

            mockCloudFoundryService.Setup(mock => mock.
                GetOrgsForCfInstanceAsync(fakeInitialCfInstance, true))
                    .ReturnsAsync(fakeSuccessfulOrgsResult);

            mockCloudFoundryService.Setup(mock => mock.
                GetSpacesForOrgAsync(fakeInitialOrg, true))
                    .ReturnsAsync(fakeNoSpacesResult);

            cfivm.Children = new ObservableCollection<TreeViewItemViewModel> { ovm };

            ovm.Children = new ObservableCollection<TreeViewItemViewModel> { svm }; // simulate org initially having 1 space child

            vm.CloudFoundryList = new ObservableCollection<CfInstanceViewModel> { cfivm };

            Assert.AreEqual(1, ovm.Children.Count);
            Assert.AreEqual(typeof(SpaceViewModel), ovm.Children[0].GetType());

            await vm.RefreshAllCloudConnections(null);

            Assert.AreEqual(1, ovm.Children.Count);
            Assert.AreEqual(typeof(PlaceholderViewModel), ovm.Children[0].GetType());
            Assert.AreEqual(OrgViewModel._emptySpacesPlaceholderMsg, ovm.Children[0].DisplayText);
        }

        [TestMethod]
        public async Task RefreshAllCloudConnections_AddsPlaceholder_ToSpacesThatBecameEmpty()
        {
            var fakeInitialCfInstance = new CloudFoundryInstance("fake cf name", "http://fake.api.address", "fake-token");
            var cfivm = new CfInstanceViewModel(fakeInitialCfInstance, services);

            var fakeInitialOrg = new CloudFoundryOrganization("fake org name", "fake org id", fakeInitialCfInstance);
            var ovm = new OrgViewModel(fakeInitialOrg, services);

            var fakeInitialSpace = new CloudFoundrySpace("fake space name", "fake space id", fakeInitialOrg);
            var svm = new SpaceViewModel(fakeInitialSpace, services);

            var fakeInitialApp = new CloudFoundryApp("fake app name", "fake app id", fakeInitialSpace, null);
            var avm = new AppViewModel(fakeInitialApp, services);

            var fakeCfDict = new Dictionary<string, CloudFoundryInstance>
            {
                { "fake cf name", fakeInitialCfInstance },
            };

            var fakeSuccessfulOrgsResult = new DetailedResult<List<CloudFoundryOrganization>>
            (
                succeeded: true,
                content: new List<CloudFoundryOrganization>
                {
                    fakeInitialOrg,
                },
                explanation: null,
                cmdDetails: fakeSuccessCmdResult
            );

            var fakeSuccessfulSpacesResult = new DetailedResult<List<CloudFoundrySpace>>
            (
                succeeded: true,
                content: new List<CloudFoundrySpace>
                {
                    fakeInitialSpace,
                },
                explanation: null,
                cmdDetails: fakeSuccessCmdResult
            );

            var fakeNoAppsResult = new DetailedResult<List<CloudFoundryApp>>
            (
                succeeded: true,
                content: new List<CloudFoundryApp>(), // simulate space having lost all apps before refresh
                explanation: null,
                cmdDetails: fakeSuccessCmdResult
            );

            mockCloudFoundryService.SetupGet(mock => mock.
                CloudFoundryInstances)
                    .Returns(fakeCfDict);

            mockCloudFoundryService.Setup(mock => mock.
                GetOrgsForCfInstanceAsync(fakeInitialCfInstance, true))
                    .ReturnsAsync(fakeSuccessfulOrgsResult);

            mockCloudFoundryService.Setup(mock => mock.
                GetSpacesForOrgAsync(fakeInitialOrg, true))
                    .ReturnsAsync(fakeSuccessfulSpacesResult);

            mockCloudFoundryService.Setup(mock => mock.
                GetAppsForSpaceAsync(fakeInitialSpace, true))
                    .ReturnsAsync(fakeNoAppsResult);

            cfivm.Children = new ObservableCollection<TreeViewItemViewModel> { ovm };

            ovm.Children = new ObservableCollection<TreeViewItemViewModel> { svm };

            svm.Children = new ObservableCollection<TreeViewItemViewModel> { avm }; // simulate space initially having 1 app child

            vm.CloudFoundryList = new ObservableCollection<CfInstanceViewModel> { cfivm };

            Assert.AreEqual(1, svm.Children.Count);
            Assert.AreEqual(typeof(AppViewModel), svm.Children[0].GetType());

            await vm.RefreshAllCloudConnections(null);

            Assert.AreEqual(1, svm.Children.Count);
            Assert.AreEqual(typeof(PlaceholderViewModel), svm.Children[0].GetType());
            Assert.AreEqual(SpaceViewModel.emptyAppsPlaceholderMsg, svm.Children[0].DisplayText);
        }

        [TestMethod]
        public async Task RefreshAllCloudConnections_RemovesPlaceholder_FromEmptyCFsThatGainedChildren()
        {
            var fakeInitialCfInstance = new CloudFoundryInstance("fake cf name", "http://fake.api.address", "fake-token");
            var cfivm = new CfInstanceViewModel(fakeInitialCfInstance, services);
            
            // simulate cf initially having no org children
            cfivm.Children = new ObservableCollection<TreeViewItemViewModel> { cfivm.EmptyPlaceholder }; 
            cfivm.HasEmptyPlaceholder = true;

            var fakeNewOrg = new CloudFoundryOrganization("fake org name", "fake org id", fakeInitialCfInstance);
            var ovm = new OrgViewModel(fakeNewOrg, services);

            var fakeCfDict = new Dictionary<string, CloudFoundryInstance>
            {
                { "fake cf name", fakeInitialCfInstance },
            };

            var fakeSuccessfulOrgsResult = new DetailedResult<List<CloudFoundryOrganization>>
            (
                succeeded: true,
                content: new List<CloudFoundryOrganization>
                {
                    fakeNewOrg, // simulate cf having gained an org child before refresh
                }, 
                explanation: null,
                cmdDetails: fakeSuccessCmdResult
            );

            vm.CloudFoundryList = new ObservableCollection<CfInstanceViewModel> { cfivm };

            mockCloudFoundryService.SetupGet(mock => mock.
                CloudFoundryInstances)
                    .Returns(fakeCfDict);

            mockCloudFoundryService.Setup(mock => mock.
                GetOrgsForCfInstanceAsync(fakeInitialCfInstance, true))
                    .ReturnsAsync(fakeSuccessfulOrgsResult);


            Assert.AreEqual(1, cfivm.Children.Count);
            Assert.AreEqual(typeof(PlaceholderViewModel), cfivm.Children[0].GetType());
            Assert.AreEqual(CfInstanceViewModel._emptyOrgsPlaceholderMsg, cfivm.Children[0].DisplayText);

            await vm.RefreshAllCloudConnections(null);

            Assert.AreEqual(1, cfivm.Children.Count);
            Assert.AreEqual(typeof(OrgViewModel), cfivm.Children[0].GetType());
        }

        [TestMethod]
        public async Task RefreshAllCloudConnections_RemovesPlaceholder_FromEmptyOrgsThatGainedChildren()
        {
            var fakeInitialCfInstance = new CloudFoundryInstance("fake cf name", "http://fake.api.address", "fake-token");
            var cfivm = new CfInstanceViewModel(fakeInitialCfInstance, services);

            var fakeInitialOrg = new CloudFoundryOrganization("fake org name", "fake org id", fakeInitialCfInstance);
            var ovm = new OrgViewModel(fakeInitialOrg, services);

            // simulate org initially having no space children
            ovm.Children = new ObservableCollection<TreeViewItemViewModel> { ovm.EmptyPlaceholder };
            ovm.HasEmptyPlaceholder = true;

            var fakeNewSpace = new CloudFoundrySpace("fake space name", "fake space id", fakeInitialOrg);
            var svm = new SpaceViewModel(fakeNewSpace, services);

            var fakeCfDict = new Dictionary<string, CloudFoundryInstance>
            {
                { "fake cf name", fakeInitialCfInstance },
            };

            var fakeSuccessfulOrgsResult = new DetailedResult<List<CloudFoundryOrganization>>
            (
                succeeded: true,
                content: new List<CloudFoundryOrganization>
                {
                    fakeInitialOrg, 
                },
                explanation: null,
                cmdDetails: fakeSuccessCmdResult
            );
            
            var fakeSuccessfulSpacesResult = new DetailedResult<List<CloudFoundrySpace>>
            (
                succeeded: true,
                content: new List<CloudFoundrySpace>
                {
                    fakeNewSpace, // simulate org having gained a space child before refresh 
                },
                explanation: null,
                cmdDetails: fakeSuccessCmdResult
            );

            mockCloudFoundryService.SetupGet(mock => mock.
                CloudFoundryInstances)
                    .Returns(fakeCfDict);

            mockCloudFoundryService.Setup(mock => mock.
                GetOrgsForCfInstanceAsync(fakeInitialCfInstance, true))
                    .ReturnsAsync(fakeSuccessfulOrgsResult);
            
            mockCloudFoundryService.Setup(mock => mock.
                GetSpacesForOrgAsync(fakeInitialOrg, true))
                    .ReturnsAsync(fakeSuccessfulSpacesResult);

            cfivm.Children = new ObservableCollection<TreeViewItemViewModel> { ovm }; 
            ovm.Children = new ObservableCollection<TreeViewItemViewModel> { ovm.EmptyPlaceholder }; // simulate org initially having no space children

            vm.CloudFoundryList = new ObservableCollection<CfInstanceViewModel> { cfivm };

            Assert.AreEqual(1, ovm.Children.Count);
            Assert.AreEqual(typeof(PlaceholderViewModel), ovm.Children[0].GetType());
            Assert.AreEqual(OrgViewModel._emptySpacesPlaceholderMsg, ovm.Children[0].DisplayText);

            await vm.RefreshAllCloudConnections(null);

            Assert.AreEqual(1, ovm.Children.Count);
            Assert.AreEqual(typeof(SpaceViewModel), ovm.Children[0].GetType());
        }

        [TestMethod]
        public async Task RefreshAllCloudConnections_RemovesPlaceholder_FromEmptySpacesThatGainedChildren()
        {
            var fakeInitialCfInstance = new CloudFoundryInstance("fake cf name", "http://fake.api.address", "fake-token");
            var cfivm = new CfInstanceViewModel(fakeInitialCfInstance, services);

            var fakeInitialOrg = new CloudFoundryOrganization("fake org name", "fake org id", fakeInitialCfInstance);
            var ovm = new OrgViewModel(fakeInitialOrg, services);

            var fakeInitialSpace = new CloudFoundrySpace("fake space name", "fake space id", fakeInitialOrg);
            var svm = new SpaceViewModel(fakeInitialSpace, services);

            // simulate space initially having no app children
            svm.Children = new ObservableCollection<TreeViewItemViewModel> { svm.EmptyPlaceholder };
            svm.HasEmptyPlaceholder = true;

            var fakeNewApp = new CloudFoundryApp("fake app name", "fake app id", fakeInitialSpace, null);
            var avm = new AppViewModel(fakeNewApp, services);

            var fakeCfDict = new Dictionary<string, CloudFoundryInstance>
            {
                { "fake cf name", fakeInitialCfInstance },
            };

            var fakeSuccessfulOrgsResult = new DetailedResult<List<CloudFoundryOrganization>>
            (
                succeeded: true,
                content: new List<CloudFoundryOrganization>
                {
                    fakeInitialOrg,
                },
                explanation: null,
                cmdDetails: fakeSuccessCmdResult
            );

            var fakeSuccessfulSpacesResult = new DetailedResult<List<CloudFoundrySpace>>
            (
                succeeded: true,
                content: new List<CloudFoundrySpace>
                {
                    fakeInitialSpace, 
                },
                explanation: null,
                cmdDetails: fakeSuccessCmdResult
            );

            var fakeSuccessfulAppsResult = new DetailedResult<List<CloudFoundryApp>>
            (
                succeeded: true,
                content: new List<CloudFoundryApp>
                {
                    fakeNewApp, // simulate space having gained an app child before refresh 
                },
                explanation: null,
                cmdDetails: fakeSuccessCmdResult
            );

            mockCloudFoundryService.SetupGet(mock => mock.
                CloudFoundryInstances)
                    .Returns(fakeCfDict);

            mockCloudFoundryService.Setup(mock => mock.
                GetOrgsForCfInstanceAsync(fakeInitialCfInstance, true))
                    .ReturnsAsync(fakeSuccessfulOrgsResult);

            mockCloudFoundryService.Setup(mock => mock.
                GetSpacesForOrgAsync(fakeInitialOrg, true))
                    .ReturnsAsync(fakeSuccessfulSpacesResult);
            
            mockCloudFoundryService.Setup(mock => mock.
                GetAppsForSpaceAsync(fakeInitialSpace, true))
                    .ReturnsAsync(fakeSuccessfulAppsResult);

            cfivm.Children = new ObservableCollection<TreeViewItemViewModel> { ovm };
            ovm.Children = new ObservableCollection<TreeViewItemViewModel> { svm }; 
            svm.Children = new ObservableCollection<TreeViewItemViewModel> { svm.EmptyPlaceholder }; // simulate space initially having no app children

            vm.CloudFoundryList = new ObservableCollection<CfInstanceViewModel> { cfivm };

            Assert.AreEqual(1, svm.Children.Count);
            Assert.AreEqual(typeof(PlaceholderViewModel), svm.Children[0].GetType());
            Assert.AreEqual(SpaceViewModel.emptyAppsPlaceholderMsg, svm.Children[0].DisplayText);

            await vm.RefreshAllCloudConnections(null);

            Assert.AreEqual(1, svm.Children.Count);
            Assert.AreEqual(typeof(AppViewModel), svm.Children[0].GetType());
        }

        [TestMethod]
        public async Task RefreshAllCloudConnections_DoesNotThrowExceptions_WhenCfInstancesHaveNoOrgChildren()
        {
            var fakeCfInstance = new CloudFoundryInstance("fake cf name", "http://fake.api.address", "fake-token");

            mockCloudFoundryService.SetupGet(mock => mock.CloudFoundryInstances)
                .Returns(new Dictionary<string, CloudFoundryInstance> {
                    { "fake cf name", fakeCfInstance }
                });

            var cfivm = new CfInstanceViewModel(fakeCfInstance, services);

            // check for presence of Placeholder child (sanity check)
            Assert.AreEqual(1, cfivm.Children.Count);
            Assert.AreEqual(typeof(PlaceholderViewModel), cfivm.Children[0].GetType());

            var fakeSuccessResult = new DetailedResult<List<CloudFoundryOrganization>>(succeeded: true, content: new List<CloudFoundryOrganization>());

            mockCloudFoundryService.Setup(mock => mock.
                GetOrgsForCfInstanceAsync(fakeCfInstance, true))
                    .ReturnsAsync(fakeSuccessResult);

            vm.CloudFoundryList = new ObservableCollection<CfInstanceViewModel>
            {
                cfivm
            };

            Exception shouldStayNull = null;
            try
            {
                await vm.RefreshAllCloudConnections(null);
            }
            catch (Exception e)
            {
                shouldStayNull = e;
            }

            Assert.IsNull(shouldStayNull);

            // check for presence of Placeholder child
            Assert.AreEqual(1, cfivm.Children.Count);
            Assert.AreEqual(typeof(PlaceholderViewModel), cfivm.Children[0].GetType());

            mockCloudFoundryService.VerifyAll();
        }

        [TestMethod]
        public async Task RefreshAllCloudConnections_DoesNotThrowExceptions_WhenOrgsHaveNoSpaceChildren()
        {
            var fakeCfInstance = new CloudFoundryInstance("fake cf name", "http://fake.api.address", "fake-token");

            mockCloudFoundryService.SetupGet(mock => mock.CloudFoundryInstances)
                .Returns(new Dictionary<string, CloudFoundryInstance> {
                    { "fake cf name", fakeCfInstance }
                });

            var cfivm = new CfInstanceViewModel(fakeCfInstance, services);

            var fakeOrg = new CloudFoundryOrganization("fake org name", "fake org id", fakeCfInstance);
            var ovm = new OrgViewModel(fakeOrg, services);

            cfivm.Children = new ObservableCollection<TreeViewItemViewModel>
            {
                ovm
            };

            // check for presence of Placeholder child (sanity check)
            Assert.AreEqual(1, ovm.Children.Count);
            Assert.AreEqual(typeof(PlaceholderViewModel), ovm.Children[0].GetType());

            var fakeOrgsList = new List<CloudFoundryOrganization> { fakeOrg };
            var fakeSuccessResult = new DetailedResult<List<CloudFoundryOrganization>>(succeeded: true, content: fakeOrgsList);

            var fakeNoChildrenResponse = new DetailedResult<List<CloudFoundrySpace>>
            (
                content: new List<CloudFoundrySpace>(),
                succeeded: true,
                explanation: null,
                cmdDetails: fakeSuccessCmdResult
            );

            mockCloudFoundryService.Setup(mock => mock.
                GetOrgsForCfInstanceAsync(fakeCfInstance, true))
                    .ReturnsAsync(fakeSuccessResult);

            mockCloudFoundryService.Setup(mock => mock.
                GetSpacesForOrgAsync(fakeOrg, true))
                    .ReturnsAsync(fakeNoChildrenResponse);

            vm.CloudFoundryList = new ObservableCollection<CfInstanceViewModel>
            {
                cfivm
            };

            Exception shouldStayNull = null;
            try
            {
                await vm.RefreshAllCloudConnections(null);
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

            mockCloudFoundryService.VerifyAll();

        }

        [TestMethod]
        public async Task RefreshAllCloudConnections_DoesNotThrowExceptions_WhenSpacesHaveNoAppChildren()
        {
            var fakeCfInstance = new CloudFoundryInstance("fake cf name", "http://fake.api.address", "fake-token");

            mockCloudFoundryService.SetupGet(mock => mock.CloudFoundryInstances)
                .Returns(new Dictionary<string, CloudFoundryInstance> {
                    { "fake cf name", fakeCfInstance }
                });

            var cfivm = new CfInstanceViewModel(fakeCfInstance, services);

            var fakeOrg = new CloudFoundryOrganization("fake org name", "fake org id", fakeCfInstance);
            var ovm = new OrgViewModel(fakeOrg, services);

            var fakeSpace = new CloudFoundrySpace("fake space name", "fake space id", fakeOrg);
            var svm = new SpaceViewModel(fakeSpace, services);

            cfivm.Children = new ObservableCollection<TreeViewItemViewModel>
            {
                ovm
            };

            ovm.Children = new ObservableCollection<TreeViewItemViewModel>
            {
                svm
            };

            // check for presence of Placeholder child (sanity check)
            Assert.AreEqual(1, svm.Children.Count);
            Assert.AreEqual(typeof(PlaceholderViewModel), svm.Children[0].GetType());

            var fakeOrgsList = new List<CloudFoundryOrganization> { fakeOrg };
            var fakeSuccessResult = new DetailedResult<List<CloudFoundryOrganization>>(succeeded: true, content: fakeOrgsList);

            var fakeSpacesList = new List<CloudFoundrySpace> { fakeSpace };
            var fakeSpacesResponse = new DetailedResult<List<CloudFoundrySpace>>
            (
                content: fakeSpacesList,
                succeeded: true,
                explanation: null,
                cmdDetails: fakeSuccessCmdResult
            );

            var emptyAppsList = new List<CloudFoundryApp>();
            var fakeSuccessfulAppsResult = new DetailedResult<List<CloudFoundryApp>>
            (
                content: emptyAppsList,
                succeeded: true,
                explanation: null,
                cmdDetails: fakeSuccessCmdResult
            );

            mockCloudFoundryService.Setup(mock => mock.
                GetOrgsForCfInstanceAsync(fakeCfInstance, true))
                    .ReturnsAsync(fakeSuccessResult);

            mockCloudFoundryService.Setup(mock => mock.
                GetSpacesForOrgAsync(fakeOrg, true))
                    .ReturnsAsync(fakeSpacesResponse);

            mockCloudFoundryService.Setup(mock => mock.
                GetAppsForSpaceAsync(fakeSpace, true))
                    .ReturnsAsync(fakeSuccessfulAppsResult);

            vm.CloudFoundryList = new ObservableCollection<CfInstanceViewModel>
            {
                cfivm
            };

            Exception shouldStayNull = null;
            try
            {
                await vm.RefreshAllCloudConnections(null);
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

            mockCloudFoundryService.VerifyAll();
        }

    }
}
