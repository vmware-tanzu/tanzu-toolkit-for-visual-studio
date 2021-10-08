using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tanzu.Toolkit.ViewModels;
using Tanzu.Toolkit.WpfViews.Commands;
using Moq;

namespace Tanzu.Toolkit.WpfViews.Tests
{
    [TestClass]
    public class LoginViewTests : ViewTestSupport
    {
        private LoginViewModel vm;

        [TestInitialize]
        public void TestInit()
        {
            vm = new LoginViewModel(services);
        }

        [TestMethod]
        public void Constructor_Initializes()
        {
            vm.ConnectionName = "My CF";
            vm.Target = "http://test/";
            var view = new LoginView(vm, mockThemeService.Object);

            // Verify DataContext initalized
            Assert.AreSame(vm, view.DataContext);

            // Verify Login command points to view model
            var command = view.AddCloudCommand as AsyncDelegatingCommand;
            Assert.IsNotNull(command);
            Assert.AreEqual(vm, command.Action.Target);

            // Verify ViewModel callback for password
            Assert.IsNotNull(vm.GetPassword);

            // Verify bindings
            Assert.AreEqual(vm.ConnectionName, view.tbConnectionName.Text);
            Assert.AreEqual(vm.Target, view.tbUrl.Text);
        }

        [TestMethod]
        public void Constructor_SetsWindowTheme_UsingThemeService()
        {
            var view = new LoginView(vm, mockThemeService.Object);
            mockThemeService.Verify(mock => mock.SetTheme(view), Times.Once);
        }
    }
}
