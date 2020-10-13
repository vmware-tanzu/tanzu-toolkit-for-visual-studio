using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using TanzuForVS.Services.Models;

namespace TanzuForVS.ViewModels
{
    [TestClass]
    public class CloudExplorerViewModelTests : ViewModelTestSupport
    {
        private CloudExplorerViewModel vm;

        [TestInitialize]
        public void TestInit()
        {
            mockCloudFoundryService.SetupGet(mock => mock.CloudItems).Returns(new Dictionary<string, CloudItem>());
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

    }
}
