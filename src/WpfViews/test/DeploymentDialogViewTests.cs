using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Tanzu.Toolkit.Models;
using Tanzu.Toolkit.ViewModels;
using Tanzu.Toolkit.WpfViews.Commands;

namespace Tanzu.Toolkit.WpfViews.Tests
{
    [TestClass]
    public class DeploymentDialogViewTests : ViewTestSupport
    {
        private DeploymentDialogViewModel vm;
        private string _fakeDirPath = "junk";
        private string _fakeTFM = "junk";

        [TestInitialize]
        public void TestInit()
        {
            var fakeCfInstance = new CloudFoundryInstance("fake cf", "http://fake.api.address");

            mockTasExplorerViewModel.SetupGet(m => m.TasConnection).Returns(new CfInstanceViewModel(fakeCfInstance, null, services));
            mockViewLocatorService.Setup(m => m.NavigateTo(It.Is<string>(s => s.Contains("TasExplorer")), null)).Returns(new FakeTasExplorerView());

            vm = new DeploymentDialogViewModel(services, _fakeDirPath, _fakeTFM);
        }

        [TestMethod]
        [TestCategory("ctor")]
        public void Constructor_Initializes()
        {
            var view = new DeploymentDialogView(vm, mockThemeService.Object);

            // Verify DataContext initalized
            Assert.AreSame(vm, view.DataContext);

            // Verify commands point to view model
            var openLoginCommand = view.OpenLoginDialogCommand as DelegatingCommand;
            var uploadAppCommand = view.UploadAppCommand as DelegatingCommand;
            Assert.IsNotNull(openLoginCommand);
            Assert.IsNotNull(uploadAppCommand);
            Assert.AreEqual(vm, openLoginCommand.Action.Target);
            Assert.AreEqual(vm, uploadAppCommand.Action.Target);
        }

        [TestMethod]
        [TestCategory("ctor")]
        public void Constructor_SetsWindowTheme_UsingThemeService()
        {
            var view = new DeploymentDialogView(vm, mockThemeService.Object);
            mockThemeService.Verify(mock => mock.SetTheme(view), Times.Once);
        }
    }

    class FakeTasExplorerView : ViewTestSupport, IView
    {
        public FakeTasExplorerView()
        {
        }

        public IViewModel ViewModel => mockTasExplorerViewModel.Object;

        public void Show()
        {
            throw new System.NotImplementedException();
        }
    }
}
