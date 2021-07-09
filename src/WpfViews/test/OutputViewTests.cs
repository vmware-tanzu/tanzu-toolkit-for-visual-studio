using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Tanzu.Toolkit.ViewModels;

namespace Tanzu.Toolkit.WpfViews.Tests
{
    [TestClass]
    public class OutputViewTests : ViewTestSupport
    {
        private OutputViewModel vm;

        [TestInitialize]
        public void TestInit()
        {
            vm = new OutputViewModel(services);
        }

        [TestMethod]
        public void Constructor_Initializes()
        {
            var view = new OutputView(vm, services, mockThemeService.Object);

            // Verify DataContext initalized
            Assert.AreSame(vm, view.DataContext);
        }

        [TestMethod]
        public void Constructor_SetsWindowTheme_UsingThemeService()
        {
            var view = new OutputView(vm, services, mockThemeService.Object);
            mockThemeService.Verify(mock => mock.SetTheme(view), Times.Once);
        }
    }
}
