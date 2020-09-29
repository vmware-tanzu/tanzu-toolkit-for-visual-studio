using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace TanzuForVS.ViewModels
{
    [TestClass]
    public class CloudExplorerViewModelTests : ViewModelTestSupport
    {

        [TestInitialize]
        public void TestInit()
        {
        }

        [TestMethod]
        public void CanOpenLoginView_ReturnsExcpected()
        {
            var vm = new CloudExplorerViewModel(services);
            Assert.IsTrue(vm.CanOpenLoginView(null));
        }

        [TestMethod]
        public void OpenLoginView_CallsDialogService_ShowDialog()
        {
            var vm = new CloudExplorerViewModel(services);
            vm.OpenLoginView(null);
            mockDialogService.Verify(ds => ds.ShowDialog(typeof(LoginDialogViewModel).Name, null), Times.Once);
        }
    }
}
