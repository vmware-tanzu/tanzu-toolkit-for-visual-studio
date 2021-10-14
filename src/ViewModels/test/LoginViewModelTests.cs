using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.Net;
using System.Security;
using System.Threading.Tasks;
using Tanzu.Toolkit.Models;
using Tanzu.Toolkit.Services.CloudFoundry;

namespace Tanzu.Toolkit.ViewModels.Tests
{
    [TestClass]
    public class LoginViewModelTests : ViewModelTestSupport
    {
        private const string FakeConnectionName = "My Fake CF";
        private const string FakeTarget = "http://my.fake.target";
        private const string FakeUsername = "correct-username";
        private const bool SkipSsl = true;
        private const string FakePwStr = "fakePw";
        private static SecureString FakeSecurePw;
        private List<string> _receivedEvents;

        private static LoginViewModel _sut;

        [TestInitialize]
        public void TestInit()
        {
            RenewMockServices();

            FakeSecurePw = new NetworkCredential("", FakePwStr).SecurePassword;

            _sut = new LoginViewModel(Services)
            {
                ConnectionName = FakeConnectionName,
                Target = FakeTarget,
                Username = FakeUsername,
                GetPassword = () => { return FakeSecurePw; },
                PasswordEmpty = () => { return false; },
                SkipSsl = SkipSsl,
            };

            _receivedEvents = new List<string>();
            _sut.PropertyChanged += (sender, e) =>
            {
                _receivedEvents.Add(e.PropertyName);
            };
        }

        [TestMethod]
        [TestCategory("LogIn")]
        public async Task LogIn_SetsErrorMessage_WhenLoginRequestFails()
        {
            const string expectedErrorMessage = "my fake error message";

            MockCloudFoundryService.Setup(mock => mock.ConnectToCFAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SecureString>(), It.IsAny<bool>()))
                .ReturnsAsync(new ConnectResult(false, expectedErrorMessage));

            await _sut.LogIn(null);

            Assert.IsTrue(_sut.HasErrors);
            Assert.AreEqual(expectedErrorMessage, _sut.ErrorMessage);
            MockCloudFoundryService.Verify(mock => mock.ConnectToCFAsync(FakeTarget, FakeUsername, FakeSecurePw, SkipSsl), Times.Once);
            MockDialogService.Verify(ds => ds.CloseDialog(It.IsAny<object>(), true), Times.Never);
        }

        [TestMethod]
        [TestCategory("LogIn")]
        public async Task LogIn_SetsConnectionOnTasExplorer_WhenLoginRequestSucceeds()
        {
            MockCloudFoundryService.Setup(mock => mock.
              ConnectToCFAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SecureString>(), It.IsAny<bool>()))
                .ReturnsAsync(new ConnectResult(true, null));

            MockTasExplorerViewModel.Setup(m => m.SetConnection(It.IsAny<CloudFoundryInstance>())).Verifiable();

            await _sut.LogIn(null);

            Assert.IsFalse(_sut.HasErrors);
            MockCloudFoundryService.Verify(mock => mock.ConnectToCFAsync(FakeTarget, FakeUsername, FakeSecurePw, SkipSsl), Times.Once);
            MockDialogService.Verify(mock => mock.CloseDialog(It.IsAny<object>(), It.IsAny<bool>()), Times.Once);
            MockDialogService.Verify(ds => ds.CloseDialog(It.IsAny<object>(), true), Times.Once);

            MockTasExplorerViewModel.Verify(m => m.SetConnection(It.Is<CloudFoundryInstance>(cf => cf.ApiAddress == FakeTarget)), Times.Once);
        }

        [TestMethod]
        [TestCategory("LogIn")]
        public async Task LogIn_ClosesDialog_WhenLoginRequestSucceeds()
        {
            MockCloudFoundryService.Setup(mock => mock.ConnectToCFAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SecureString>(), It.IsAny<bool>()))
               .ReturnsAsync(new ConnectResult(true, null));

            await _sut.LogIn(null);

            Assert.IsFalse(_sut.HasErrors);
            MockCloudFoundryService.Verify(mock => mock.ConnectToCFAsync(FakeTarget, FakeUsername, FakeSecurePw, SkipSsl), Times.Once);
            MockDialogService.Verify(mock => mock.CloseDialog(It.IsAny<object>(), It.IsAny<bool>()), Times.Once);
            MockDialogService.Verify(ds => ds.CloseDialog(It.IsAny<object>(), true), Times.Once);
        }

        [TestMethod]
        [TestCategory("CanLogIn")]
        public void CanLogIn_ReturnsTrue_WhenAllFieldsFilled()
        {
            Assert.IsNotNull(_sut.ConnectionName);
            Assert.IsNotNull(_sut.Target);
            Assert.IsNotNull(_sut.Username);
            Assert.IsFalse(_sut.PasswordEmpty());
            Assert.IsTrue(_sut.VerifyApiAddress(_sut.Target));

            Assert.IsTrue(_sut.CanLogIn());
        }

        [TestMethod]
        [TestCategory("CanLogIn")]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow("  ")]
        [DataRow(null)]
        public void CanLogIn_ReturnsFalse_WhenConnectionNameEmpty(string invalidConnectionName)
        {
            _sut.ConnectionName = invalidConnectionName;

            Assert.IsTrue(string.IsNullOrWhiteSpace(_sut.ConnectionName));
            Assert.IsNotNull(_sut.Target);
            Assert.IsNotNull(_sut.Username);
            Assert.IsFalse(_sut.PasswordEmpty());
            Assert.IsTrue(_sut.VerifyApiAddress(_sut.Target));

            Assert.IsFalse(_sut.CanLogIn());
        }

        [TestMethod]
        [TestCategory("CanLogIn")]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow("  ")]
        [DataRow(null)]
        public void CanLogIn_ReturnsFalse_WhenTargetEmpty(string invalidTargetApiAddress)
        {
            _sut.Target = invalidTargetApiAddress;

            Assert.IsNotNull(_sut.ConnectionName);
            Assert.IsTrue(string.IsNullOrWhiteSpace(_sut.Target));
            Assert.IsNotNull(_sut.Username);
            Assert.IsFalse(_sut.PasswordEmpty());
            Assert.IsFalse(_sut.VerifyApiAddress(_sut.Target));

            Assert.IsFalse(_sut.CanLogIn());
        }

        [TestMethod]
        [TestCategory("CanLogIn")]
        [DataRow("asdf")]
        [DataRow("^%$(**")]
        [DataRow("a.bad.url")]
        public void CanLogIn_ReturnsFalse_WhenTargetFailsVerification(string invalidTargetApiAddress)
        {
            _sut.Target = invalidTargetApiAddress;

            Assert.IsNotNull(_sut.ConnectionName);
            Assert.IsFalse(_sut.VerifyApiAddress(_sut.Target));
            Assert.IsNotNull(_sut.Username);
            Assert.IsFalse(_sut.PasswordEmpty());

            Assert.IsFalse(_sut.CanLogIn());
        }

        [TestMethod]
        [TestCategory("CanLogIn")]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow("  ")]
        [DataRow(null)]
        public void CanLogIn_ReturnsFalse_WhenUsernameEmpty(string invalidUsername)
        {
            _sut.Username = invalidUsername;

            Assert.IsNotNull(_sut.ConnectionName);
            Assert.IsNotNull(_sut.Target);
            Assert.IsTrue(string.IsNullOrWhiteSpace(_sut.Username));
            Assert.IsFalse(_sut.PasswordEmpty());
            Assert.IsTrue(_sut.VerifyApiAddress(_sut.Target));

            Assert.IsFalse(_sut.CanLogIn());
        }

        [TestMethod]
        [TestCategory("CanLogIn")]
        public void CanLogIn_ReturnsFalse_WhenPasswordEmpty()
        {
            // mock out testimony from login view that password box is empty
            _sut.PasswordEmpty = () => { return true; };

            Assert.IsNotNull(_sut.ConnectionName);
            Assert.IsNotNull(_sut.Target);
            Assert.IsNotNull(_sut.Username);
            Assert.IsTrue(_sut.PasswordEmpty());
            Assert.IsTrue(_sut.VerifyApiAddress(_sut.Target));

            Assert.IsFalse(_sut.CanLogIn());
        }

        [TestMethod]
        [TestCategory("VerifyApiAddress")]
        [DataRow("", false, LoginViewModel.TargetEmptyMessage)]
        [DataRow(" ", false, LoginViewModel.TargetEmptyMessage)]
        [DataRow("_", false, LoginViewModel.TargetInvalidFormatMessage)]
        [DataRow(null, false, LoginViewModel.TargetEmptyMessage)]
        [DataRow("www.api.com", false, LoginViewModel.TargetInvalidFormatMessage)]
        [DataRow("http://www.api.com", true, null)]
        [DataRow("https://my.cool.url", true, null)]
        public void VerifyApiAddress_SetsApiAddressIsValid_AndSetsApiAddressError(string apiAddr, bool expectedValidity, string expectedError)
        {
            _sut.VerifyApiAddress(apiAddr);
            Assert.AreEqual(expectedValidity, _sut.ApiAddressIsValid);
            Assert.AreEqual(expectedError, _sut.ApiAddressError);
            Assert.IsTrue(_receivedEvents.Contains("ApiAddressIsValid"));
        }
    }
}