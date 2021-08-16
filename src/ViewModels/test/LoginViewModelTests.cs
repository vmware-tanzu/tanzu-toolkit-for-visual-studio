using System;
using System.Security;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Tanzu.Toolkit.Services.CloudFoundry;

namespace Tanzu.Toolkit.ViewModels.Tests
{
    [TestClass]
    public class LoginViewModelTests : ViewModelTestSupport
    {
        private const string FakeInstanceName = "My Fake CF";
        private const string FakeTarget = "http://my.fake.target";
        private const string FakeUsername = "correct-username";
        private const string FakeHttpProxy = "junk";
        private const bool SkipSsl = true;
        private const string FakeToken = "junk";
        private static readonly SecureString FakeSecurePw = new SecureString();

        private static LoginViewModel _sut;

        [TestInitialize]
        public void TestInit()
        {
            RenewMockServices();

            _sut = new LoginViewModel(Services)
            {
                InstanceName = FakeInstanceName,
                Target = FakeTarget,
                Username = FakeUsername,
                GetPassword = () => { return FakeSecurePw; },
                HttpProxy = FakeHttpProxy,
                SkipSsl = SkipSsl,
            };
        }

        [TestMethod]
        public async Task AddCloudFoundryInstance_SetsErrorMessage_WhenTargetUriNull()
        {
            bool errorMessagePropertyChangedCalled = false;
            _sut.Target = null;

            _sut.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == "ErrorMessage")
                {
                    errorMessagePropertyChangedCalled = true;
                }
            };

            await _sut.AddCloudFoundryInstance(null);

            Assert.IsTrue(errorMessagePropertyChangedCalled);
            Assert.IsTrue(_sut.HasErrors);
            Assert.AreEqual(LoginViewModel.TargetEmptyMessage, _sut.ErrorMessage);

            MockDialogService.Verify(ds => ds.CloseDialog(It.IsAny<object>(), true), Times.Never);
            MockDialogService.Verify(ds => ds.ShowDialog(It.IsAny<string>(), null), Times.Never);
            MockCloudFoundryService.Verify(mock => mock.ConnectToCFAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SecureString>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        }

        [TestMethod]
        public async Task AddCloudFoundryInstance_SetsErrorMessage_WhenTargetUriMalformed()
        {
            bool errorMessagePropertyChangedCalled = false;
            _sut.Target = "some-poorly-formatted-uri";

            _sut.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == "ErrorMessage")
                {
                    errorMessagePropertyChangedCalled = true;
                }
            };

            await _sut.AddCloudFoundryInstance(null);

            Assert.IsTrue(errorMessagePropertyChangedCalled);
            Assert.IsTrue(_sut.HasErrors);
            Assert.AreEqual(LoginViewModel.TargetInvalidFormatMessage, _sut.ErrorMessage);

            MockDialogService.Verify(ds => ds.CloseDialog(It.IsAny<object>(), true), Times.Never);
            MockDialogService.Verify(ds => ds.ShowDialog(It.IsAny<string>(), null), Times.Never);
            MockCloudFoundryService.Verify(mock => mock.ConnectToCFAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SecureString>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        }

        [TestMethod]
        public async Task AddCloudFoundryInstance_SetsErrorMessage_WhenLoginRequestFails()
        {
            const string expectedErrorMessage = "my fake error message";

            MockCloudFoundryService.Setup(mock => mock.ConnectToCFAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SecureString>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(new ConnectResult(false, expectedErrorMessage));

            await _sut.AddCloudFoundryInstance(null);

            Assert.IsTrue(_sut.HasErrors);
            Assert.AreEqual(expectedErrorMessage, _sut.ErrorMessage);
            MockCloudFoundryService.Verify(mock => mock.ConnectToCFAsync(FakeTarget, FakeUsername, FakeSecurePw, FakeHttpProxy, SkipSsl), Times.Once);
            MockDialogService.Verify(ds => ds.CloseDialog(It.IsAny<object>(), true), Times.Never);
        }

        [TestMethod]
        public async Task AddCloudFoundryInstance_ClosesDialog_WhenLoginRequestSucceeds()
        {
            MockCloudFoundryService.Setup(mock => mock.ConnectToCFAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SecureString>(), It.IsAny<string>(), It.IsAny<bool>()))
               .ReturnsAsync(new ConnectResult(true, null));

            await _sut.AddCloudFoundryInstance(null);

            Assert.IsFalse(_sut.HasErrors);
            MockCloudFoundryService.Verify(mock => mock.ConnectToCFAsync(FakeTarget, FakeUsername, FakeSecurePw, FakeHttpProxy, SkipSsl), Times.Once);
            MockDialogService.Verify(mock => mock.CloseDialog(It.IsAny<object>(), It.IsAny<bool>()), Times.Once);
            MockDialogService.Verify(ds => ds.CloseDialog(It.IsAny<object>(), true), Times.Once);
        }

        [TestMethod]
        public async Task AddCloudFoundryInstance_DoesNotCloseDialog_WhenLoginRequestFails()
        {
            const string expectedErrorMessage = "my fake error message";
            const string cloudName = "my fake instance name";

            MockCloudFoundryService.Setup(mock => mock.ConnectToCFAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SecureString>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(new ConnectResult(false, expectedErrorMessage));

            _sut.InstanceName = cloudName;
            await _sut.AddCloudFoundryInstance(null);

            Assert.IsNotNull(_sut.ErrorMessage);
            MockDialogService.Verify(mock => mock.CloseDialog(It.IsAny<object>(), It.IsAny<bool>()), Times.Never);
            MockCloudFoundryService.Verify(mock => mock.AddCloudFoundryInstance(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            MockCloudFoundryService.VerifyAll();
        }

        [TestMethod]
        public async Task AddCloudFoundryInstance_SetsErrorMessage_WhenAddCloudFoundryInstanceThrowsException()
        {
            string duplicateName = "I was already added";
            string errorMsg = "fake error message thrown by CF service";

            MockCloudFoundryService.Setup(mock => mock.ConnectToCFAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SecureString>(), It.IsAny<string>(), It.IsAny<bool>()))
               .ReturnsAsync(new ConnectResult(true, null));

            MockCloudFoundryService.Setup(mock => mock.AddCloudFoundryInstance(duplicateName, FakeTarget))
                .Throws(new Exception(errorMsg));

            _sut.InstanceName = duplicateName;
            await _sut.AddCloudFoundryInstance(null);

            Assert.IsTrue(_sut.HasErrors);
            Assert.AreEqual(errorMsg, _sut.ErrorMessage);
            MockCloudFoundryService.VerifyAll();
            MockDialogService.Verify(mock => mock.CloseDialog(It.IsAny<object>(), It.IsAny<bool>()), Times.Never);
        }
    }
}
