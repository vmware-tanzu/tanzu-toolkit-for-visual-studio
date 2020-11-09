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
            var fakeApp = new CloudFoundryApp("junk","junk", parentSpace: null);

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
    }
}
