using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TanzuForVS.Models;

namespace TanzuForVS.ViewModels
{
    [TestClass]
    public class CloudExplorerViewModelTests : ViewModelTestSupport
    {
        private CloudExplorerViewModel vm;

        [TestInitialize]
        public void TestInit()
        {
            mockCloudFoundryService.SetupGet(mock => mock.CloudFoundryInstances).Returns(new Dictionary<string, CloudFoundryInstance>());
            vm = new CloudExplorerViewModel(services);
        }

        [TestMethod]
        public void CanOpenLoginView_ReturnsExpected()
        {
            Assert.IsTrue(vm.CanOpenLoginView(null));
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
            var fakeCfsDict = new Dictionary<string, CloudFoundryInstance>
            {
                { "fake cf", new CloudFoundryInstance("fake cf", null, null) }
            };

            Assert.AreEqual(0, vm.CloudFoundryList.Count);

            mockCloudFoundryService.SetupGet(mock => mock.CloudFoundryInstances).Returns(fakeCfsDict);

            vm.OpenLoginView(null);

            Assert.IsTrue(vm.HasCloudTargets);
            Assert.AreEqual(1, vm.CloudFoundryList.Count);
        }

        [TestMethod]
        public void CanStopCfApp_ReturnsTrue()
        {
            Assert.IsTrue(vm.CanStopCfApp(null));
        }

        [TestMethod]
        public async Task StopCfApp_ThrowsException_IfArgTypeIsNotCloudFoundryApp()
        {
            object notAnApp = new object();

            Exception expectedException = null;
            try
            {
                await vm.StopCfApp(notAnApp);
            }
            catch (Exception e)
            {
                expectedException = e;
            }

            Assert.IsNotNull(expectedException);
            Assert.IsTrue(expectedException.Message.Contains("Expected a CloudFoundryApp"));
        }

        [TestMethod]
        public void CanDeleteCfApp_ReturnsTrue()
        {
            Assert.IsTrue(vm.CanDeleteCfApp(null));

        }

        [TestMethod]
        public async Task DeleteCfApp_ThrowsException_IfArgTypeIsNotCloudFoundryApp()
        {
            object notAnApp = new object();

            Exception expectedException = null;
            try
            {
                await vm.DeleteCfApp(notAnApp);
            }
            catch (Exception e)
            {
                expectedException = e;
            }

            Assert.IsNotNull(expectedException);
            Assert.IsTrue(expectedException.Message.Contains("Expected a CloudFoundryApp"));
        }

        [TestMethod]
        public async Task DeleteCfApp_CallsDeleteAppAsync()
        {
            var fakeApp = new CloudFoundryApp("junk", "junk", parentSpace: null);

            mockCloudFoundryService.Setup(mock => mock.DeleteAppAsync(fakeApp)).ReturnsAsync(true);

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
        public void CanRefreshSpace_ReturnsTrue()
        {
            Assert.IsTrue(vm.CanRefreshSpace(null));
        }

        [TestMethod]
        public async Task RefreshSpace_UpdatesChildrenOnSpaceViewModel()
        {
            var fakeSpace = new CloudFoundrySpace("fake space name", "fake space id", null);
            var fakeSpaceViewModel = new SpaceViewModel(fakeSpace, services);

            var fakeAppName1 = "fake app 1";
            var fakeAppName2 = "fake app 2";
            var fakeAppName3 = "fake app 3";

            fakeSpaceViewModel.Children = new System.Collections.ObjectModel.ObservableCollection<TreeViewItemViewModel>
            {
                new AppViewModel(new CloudFoundryApp(fakeAppName1, "junk", fakeSpace), services)
            };

            Assert.AreEqual(1, fakeSpaceViewModel.Children.Count);
            AppViewModel firstChildApp = (AppViewModel)fakeSpaceViewModel.Children[0];
            Assert.AreEqual(fakeAppName1, firstChildApp.App.AppName);

            mockCloudFoundryService.Setup(mock => mock.GetAppsForSpaceAsync(fakeSpace)).ReturnsAsync(new List<CloudFoundryApp>
            {
                new CloudFoundryApp(fakeAppName2, "junk", fakeSpace),
                new CloudFoundryApp(fakeAppName3, "junk", fakeSpace)
            });

            await vm.RefreshSpace(fakeSpaceViewModel);

            Assert.AreEqual(2, fakeSpaceViewModel.Children.Count);

            AppViewModel firstNewChildApp = (AppViewModel)fakeSpaceViewModel.Children[0];
            AppViewModel secondNewChildApp = (AppViewModel)fakeSpaceViewModel.Children[1];
            Assert.AreEqual(fakeAppName2, firstNewChildApp.App.AppName);
            Assert.AreEqual(fakeAppName3, secondNewChildApp.App.AppName);
            mockCloudFoundryService.VerifyAll();
        }
    }
}
