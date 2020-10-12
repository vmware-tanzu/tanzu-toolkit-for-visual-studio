using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TanzuForVS.Services.CloudFoundry;

namespace TanzuForVS.ViewModels
{
    [TestClass]
    public class CloudExplorerViewModelTests : ViewModelTestSupport
    {
        private CloudExplorerViewModel vm;

        [TestInitialize]
        public void TestInit()
        {
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
        public void OpenLoginView_UpdatesIsLoggedIn_AfterDialogCloses()
        {
            Assert.IsFalse(vm.IsLoggedIn);
            mockCloudFoundryService.SetupGet(mock => mock.IsLoggedIn).Returns(true);

            vm.OpenLoginView(null);

            Assert.IsTrue(vm.IsLoggedIn);
        }
    }
}
