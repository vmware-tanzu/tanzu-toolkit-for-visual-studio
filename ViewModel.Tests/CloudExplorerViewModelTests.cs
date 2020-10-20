using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
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
        public void CanOpenLoginView_ReturnsExcpected()
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

    }
}
