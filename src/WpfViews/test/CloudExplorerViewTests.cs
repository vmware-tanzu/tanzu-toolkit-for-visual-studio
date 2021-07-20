using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Tanzu.Toolkit.Models;
using Tanzu.Toolkit.ViewModels;
using Tanzu.Toolkit.WpfViews.Commands;

namespace Tanzu.Toolkit.WpfViews.Tests
{
    [TestClass]
    public class CloudExplorerViewTests : ViewTestSupport
    {
        private CloudExplorerViewModel vm;

        [TestInitialize]
        public void TestInit()
        {
            var fakeCfInstances = new Dictionary<string, CloudFoundryInstance>();
            mockCloudFoundryService.SetupGet(mock => mock.CloudFoundryInstances).Returns(fakeCfInstances);
            
            vm = new CloudExplorerViewModel(services);
        }

        [TestMethod]
        public void Constructor_Initializes()
        {
            var view = new CloudExplorerView(vm, mockThemeService.Object, mockViewService.Object);

            // Verify DataContext initalized
            Assert.AreSame(vm, view.DataContext);

            // Verify commands point to view model
            var openLoginCommand = view.OpenLoginFormCommand as DelegatingCommand;
            var stopAppCommand = view.StopCfAppCommand as AsyncDelegatingCommand;
            Assert.IsNotNull(openLoginCommand);
            Assert.IsNotNull(stopAppCommand);
            Assert.AreEqual(vm, openLoginCommand.Action.Target);
            Assert.AreEqual(vm, stopAppCommand.Action.Target);
        }

        [TestMethod]
        public void Constructor_SetsWindowTheme_UsingThemeService()
        {
            var view = new CloudExplorerView(vm, mockThemeService.Object, mockViewService.Object);
            mockThemeService.Verify(mock => mock.SetTheme(view), Times.Once);
        }
    }
}
