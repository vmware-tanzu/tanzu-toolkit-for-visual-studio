using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using TanzuForVS.Models;
using TanzuForVS.ViewModels;
using TanzuForVS.WpfViews.Commands;

namespace TanzuForVS.WpfViews.Tests
{
    [TestClass]
    public class CloudExplorerViewTests : ViewTestSupport
    {
        [TestMethod]
        public void Constructor_Initializes()
        {
            var fakeCfInstances = new Dictionary<string, CloudFoundryInstance>();
            mockCloudFoundryService.SetupGet(mock => mock.CloudFoundryInstances).Returns(fakeCfInstances);
            var vm = new CloudExplorerViewModel(services);
            var view = new CloudExplorerView(vm);

            // Verify DataContext initalized
            Assert.AreSame(vm, view.DataContext);

            // Verify commands point to view model
            var openLoginCommand = view.OpenLoginFormCommand as DelegatingCommand;
            var stopAppCommand = view.StopCfAppCommand as AsyncDelegatingCommand;
            Assert.IsNotNull(openLoginCommand);
            Assert.IsNotNull(stopAppCommand);
            Assert.AreEqual(vm, openLoginCommand.action.Target);
            Assert.AreEqual(vm, stopAppCommand.action.Target);
        }
    }
}
