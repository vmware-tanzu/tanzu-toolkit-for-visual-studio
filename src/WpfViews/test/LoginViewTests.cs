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
            var _sut = new LoginView(vm, mockThemeService.Object);

            // Verify DataContext initalized
            Assert.AreSame(vm, _sut.DataContext);

            // Verify Login command points to view model
            var command = _sut.AddCloudCommand as AsyncDelegatingCommand;
            Assert.IsNotNull(command);
            Assert.AreEqual(vm, command.Action.Target);

            // Verify ViewModel callbacks for password
            Assert.IsNotNull(vm.GetPassword);
            Assert.IsNotNull(vm.PasswordEmpty);
            Assert.AreEqual(vm.GetPassword, _sut.GetPassword);
            Assert.AreEqual(vm.PasswordEmpty, _sut.PasswordBoxEmpty);

            // Verify bindings
            Assert.AreEqual(vm.ConnectionName, _sut.tbConnectionName.Text);
            Assert.AreEqual(vm.Target, _sut.tbUrl.Text);
        }

        [TestMethod]
        public void Constructor_SetsWindowTheme_UsingThemeService()
        {
            var view = new LoginView(vm, mockThemeService.Object);
            mockThemeService.Verify(mock => mock.SetTheme(view), Times.Once);
        }
    }
}
