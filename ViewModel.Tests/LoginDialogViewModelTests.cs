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
        static LoginDialogViewModel vm;
        static readonly string fakeInstanceName = "My Fake CF";
        static readonly string fakeTarget = "http://my.fake.target";
        static readonly string fakeUsername = "correct-username";
        static readonly SecureString fakeSecurePw = new SecureString();
        static readonly string fakeHttpProxy = "junk";
        static readonly bool skipSsl = true;

        [TestInitialize]
        public void TestInit()
        {
            vm = new LoginDialogViewModel(services);
            vm.InstanceName = fakeInstanceName;
            vm.Target = fakeTarget;
            vm.Username = fakeUsername;
            vm.GetPassword = () => { return fakeSecurePw; };
            vm.HttpProxy = fakeHttpProxy;
            vm.SkipSsl = skipSsl;
        }

        [TestMethod]
        public void ConnectToCloudFoundry_SetsErrorMessage_WhenTargetUriNull()
        {
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
            const string expectedErrorMessage = "my fake error message";

            mockCloudFoundryService.Setup(mock => mock.ConnectToCFAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SecureString>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(new ConnectResult(false, expectedErrorMessage));

            await vm.ConnectToCloudFoundry(null);

            Assert.IsTrue(vm.HasErrors);
            Assert.AreEqual(expectedErrorMessage, vm.ErrorMessage);
            mockCloudFoundryService.Verify(mock => mock.ConnectToCFAsync(fakeTarget, fakeUsername, fakeSecurePw, fakeHttpProxy, skipSsl), Times.Once);
            mockDialogService.Verify(ds => ds.CloseDialog(It.IsAny<object>(), true), Times.Never);
        }

        [TestMethod]
        public async Task ConnectToCloudFoundry_ClosesDialog_WhenLoginRequestSucceeds()
        {
            mockCloudFoundryService.Setup(mock => mock.ConnectToCFAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SecureString>(), It.IsAny<string>(), It.IsAny<bool>()))
               .ReturnsAsync(new ConnectResult(true, null));

            await vm.ConnectToCloudFoundry(null);

            Assert.IsFalse(vm.HasErrors);
            Assert.IsTrue(vm.IsLoggedIn);
            mockCloudFoundryService.Verify(mock => mock.ConnectToCFAsync(fakeTarget, fakeUsername, fakeSecurePw, fakeHttpProxy, skipSsl), Times.Once);
            mockDialogService.Verify(ds => ds.CloseDialog(It.IsAny<object>(), true), Times.Once);
        }

    }
}
