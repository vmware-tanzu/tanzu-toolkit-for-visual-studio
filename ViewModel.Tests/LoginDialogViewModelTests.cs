using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Security;
using System.Threading.Tasks;
using TanzuForVS.Services.CloudFoundry;

namespace TanzuForVS.ViewModels
{
    [TestClass]
    public class LoginDialogViewModelTests : ViewModelTestSupport
    {

        [TestInitialize]
        public void TestInit()
        {
        }

        [TestMethod]
        public void ConnectToCloudFoundry_SetsErrorMessage_WhenTargetUriNull()
        {
            var vm = new LoginDialogViewModel(services);
            bool ErrorMessagePropertyChangedCalled = false;
            vm.Target = null;

            vm.PropertyChanged += (s, args) =>
            {
                if ("ErrorMessage" == args.PropertyName) ErrorMessagePropertyChangedCalled = true;
            };

            vm.ConnectToCloudFoundry(null);

            Assert.IsTrue(ErrorMessagePropertyChangedCalled);
            Assert.IsTrue(vm.HasErrors);
            Assert.AreEqual(LoginDialogViewModel.TargetEmptyMessage, vm.ErrorMessage);
            Assert.IsFalse(vm.IsLoggedIn);

            mockDialogService.Verify(ds => ds.CloseDialog(It.IsAny<object>(), true), Times.Never);
            mockDialogService.Verify(ds => ds.ShowDialog(It.IsAny<string>(), null), Times.Never);
            mockCloudFoundryService.Verify(mock => mock.ConnectToCFAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SecureString>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        }

        [TestMethod]
        public async Task ConnectToCloudFoundry_SetsErrorMessage_WhenTargetUriMalformed()
        {
            var vm = new LoginDialogViewModel(services);
            bool ErrorMessagePropertyChangedCalled = false;
            vm.Target = "some-poorly-formatted-uri";

            vm.PropertyChanged += (s, args) =>
            {
                if ("ErrorMessage" == args.PropertyName) ErrorMessagePropertyChangedCalled = true;
            };

            await vm.ConnectToCloudFoundry(null);

            Assert.IsTrue(ErrorMessagePropertyChangedCalled);
            Assert.IsTrue(vm.HasErrors);
            Assert.AreEqual(LoginDialogViewModel.TargetInvalidFormatMessage, vm.ErrorMessage);
            Assert.IsFalse(vm.IsLoggedIn);

            mockDialogService.Verify(ds => ds.CloseDialog(It.IsAny<object>(), true), Times.Never);
            mockDialogService.Verify(ds => ds.ShowDialog(It.IsAny<string>(), null), Times.Never);
            mockCloudFoundryService.Verify(mock => mock.ConnectToCFAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SecureString>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        }

        [TestMethod]
        public async Task ConnectToCloudFoundry_SetsErrorMessage_WhenLoginRequestFails()
        {
            var vm = new LoginDialogViewModel(services);
            var fakeSecurePw = new SecureString();
            vm.Target = "http://my.fake.target";
            vm.Username = "junk";
            vm.GetPassword = () => { return fakeSecurePw; };
            const string expectedErrorMessage = "my fake error message";

            mockCloudFoundryService.Setup(mock => mock.ConnectToCFAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SecureString>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(new ConnectResult(false, expectedErrorMessage));

            await vm.ConnectToCloudFoundry(null);

            Assert.IsTrue(vm.HasErrors);
            Assert.AreEqual(expectedErrorMessage, vm.ErrorMessage);
            mockDialogService.Verify(ds => ds.CloseDialog(It.IsAny<object>(), true), Times.Never);
        }
    }
}
