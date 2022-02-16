using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.Net;
using System.Security;
using System.Threading.Tasks;
using Tanzu.Toolkit.Models;
using Tanzu.Toolkit.Services;

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
                ClearPassword = () => { },
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

        [TestCleanup]
        public void TestCleanup()
        {
            MockCloudFoundryService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("ctor")]
        public void Constructor_SetsApiAddressIsValid_ToTrue()
        {
            Assert.IsTrue(_sut.ApiAddressIsValid);
        }

        [TestMethod]
        [TestCategory("LogIn")]
        public async Task LogIn_SetsErrorMessage_WhenLoginRequestFails()
        {
            const string expectedErrorMessage = "my fake error message";

            MockCloudFoundryService.Setup(mock => mock.LoginWithCredentials(FakeTarget, FakeUsername, FakeSecurePw, _sut.ProceedWithInvalidCertificate))
                .ReturnsAsync(new DetailedResult(false, expectedErrorMessage));

            await _sut.LogIn(null);

            Assert.IsTrue(_sut.HasErrors);
            Assert.AreEqual(expectedErrorMessage, _sut.ErrorMessage);
            MockDialogService.Verify(ds => ds.CloseDialog(It.IsAny<object>(), true), Times.Never);
        }

        [TestMethod]
        [TestCategory("LogIn")]
        public async Task LogIn_SetsConnectionOnTasExplorer_WhenLoginRequestSucceeds()
        {
            MockCloudFoundryService.Setup(mock => mock.
              LoginWithCredentials(FakeTarget, FakeUsername, FakeSecurePw, _sut.ProceedWithInvalidCertificate))
                .ReturnsAsync(new DetailedResult(true, null));

            MockTasExplorerViewModel.Setup(m => m.SetConnection(It.IsAny<CloudFoundryInstance>())).Verifiable();

            await _sut.LogIn(null);

            Assert.IsFalse(_sut.HasErrors);
            MockDialogService.Verify(mock => mock.CloseDialog(It.IsAny<object>(), It.IsAny<bool>()), Times.Once);
            MockDialogService.Verify(ds => ds.CloseDialog(It.IsAny<object>(), true), Times.Once);

            MockTasExplorerViewModel.Verify(m => m.SetConnection(It.Is<CloudFoundryInstance>(cf => cf.ApiAddress == FakeTarget)), Times.Once);
        }

        [TestMethod]
        [TestCategory("LogIn")]
        public async Task LogIn_ClosesDialog_WhenLoginRequestSucceeds()
        {
            MockCloudFoundryService.Setup(mock => mock.LoginWithCredentials(FakeTarget, FakeUsername, FakeSecurePw, _sut.ProceedWithInvalidCertificate))
               .ReturnsAsync(new DetailedResult(true, null));

            await _sut.LogIn(null);

            Assert.IsFalse(_sut.HasErrors);
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
            Assert.IsTrue(_sut.ValidateApiAddressFormat(_sut.Target));

            Assert.IsTrue(_sut.CanLogIn());
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

            Assert.IsTrue(string.IsNullOrWhiteSpace(_sut.Target));

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

            Assert.IsFalse(_sut.ValidateApiAddressFormat(_sut.Target));

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
            Assert.IsTrue(_sut.ValidateApiAddressFormat(_sut.Target));

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
            Assert.IsTrue(_sut.ValidateApiAddressFormat(_sut.Target));

            Assert.IsFalse(_sut.CanLogIn());
        }

        [TestMethod]
        [TestCategory("VerifyApiAddress")]
        [DataRow("_", false, LoginViewModel.TargetInvalidFormatMessage)]
        [DataRow("www.api.com", false, LoginViewModel.TargetInvalidFormatMessage)]
        [DataRow("http://www.api.com", true, null)]
        [DataRow("https://my.cool.url", true, null)]
        public void VerifyApiAddress_SetsApiAddressIsValid_AndSetsApiAddressError(string apiAddr, bool expectedValidity, string expectedError)
        {
            _sut.ValidateApiAddressFormat(apiAddr);
            Assert.AreEqual(expectedValidity, _sut.ApiAddressIsValid);
            Assert.AreEqual(expectedError, _sut.ApiAddressError);
            Assert.IsTrue(_receivedEvents.Contains("ApiAddressIsValid"));
        }

        [TestMethod]
        [DataRow("https://www.api.com", "My Cool TAS", "My Cool TAS")]
        [DataRow("https://www.api.com", "asdf1234", "asdf1234")]
        [DataRow("https://www.api.com", "", "www.api.com")]
        [DataRow("https://www.api.com", " ", "www.api.com")]
        [DataRow("https://www.api.com", null, "www.api.com")]
        [DataRow("www.api.com", "My Cool TAS", "My Cool TAS")]
        [DataRow("http://www.api.com", "My Cool TAS", "My Cool TAS")]
        [DataRow("http://www.api.com", null, "www.api.com")]
        [DataRow("https://www.api.com/some/endpoint", "My Cool TAS", "My Cool TAS")]
        [DataRow("https://www.api.com/some/endpoint", null, "www.api.com")]
        [DataRow("https://www.api.com:80", "My Cool TAS", "My Cool TAS")]
        [DataRow("https://www.api.com:80", null, "www.api.com")]
        [DataRow("non-parseable address", "", "Tanzu Application Service")]
        [DataRow("non-parseable address", " ", "Tanzu Application Service")]
        [DataRow("non-parseable address", null, "Tanzu Application Service")]
        [DataRow("www.api.com", "", "Tanzu Application Service")]
        [DataRow("www.api.com", " ", "Tanzu Application Service")]
        [DataRow("www.api.com", null, "Tanzu Application Service")]
        public void SetConnection_SetsConnectionOnTasExplorer(string apiAddressFromDialog, string connectionNameFromDialog, string expectedTasConnectionName)
        {
            _sut.Target = apiAddressFromDialog;
            _sut.ConnectionName = connectionNameFromDialog;

            MockTasExplorerViewModel.Setup(m => m.SetConnection(It.IsAny<CloudFoundryInstance>()));

            _sut.SetConnection();

            MockTasExplorerViewModel.Verify(m => m.SetConnection(It.Is<CloudFoundryInstance>(cf => cf.InstanceName == expectedTasConnectionName && cf.ApiAddress == apiAddressFromDialog)), Times.Once);
        }

        [TestMethod]
        [TestCategory("CanOpenSsoDialog")]
        [DataRow("http://some.api.address")]
        [DataRow("https://some.api.address")]
        public void CanOpenSsoDialog_ReturnsTrue_WhenTargetApiAddressIsValid(string targetApiAddress)
        {
            _sut.Target = targetApiAddress;

            _sut.ApiAddressIsValid = true;

            Assert.IsTrue(_sut.CanOpenSsoDialog());
        }

        [TestMethod]
        [TestCategory("CanOpenSsoDialog")]
        [DataRow(null)]
        [DataRow("")]
        [DataRow(" ")]
        public void CanOpenSsoDialog_ReturnsFalse_WhenTargetApiAddressIsNullOrWhitespace(string targetApiAddress)
        {
            _sut.Target = targetApiAddress;

            _sut.ApiAddressIsValid = true;

            Assert.IsFalse(_sut.CanOpenSsoDialog());
        }

        [TestMethod]
        [TestCategory("CanOpenSsoDialog")]
        public void CanOpenSsoDialog_ReturnsFalse_WhenTargetApiAddressIsInvalid()
        {
            _sut.Target = "junk";

            _sut.ApiAddressIsValid = false;

            Assert.IsFalse(_sut.CanOpenSsoDialog());
        }

        [TestMethod]
        [TestCategory("OpenSsoDialog")]
        [DataRow(null)]
        [DataRow("")]
        [DataRow(" ")]
        public async Task OpenSsoDialog_SetsErrorMessage_WhenTargetApiAddressIsNullOrWhitespace(string targetApiAddress)
        {
            _sut.Target = targetApiAddress;

            Assert.IsFalse(_sut.HasErrors);
            Assert.IsNull(_sut.ErrorMessage);

            await _sut.OpenSsoDialog();

            Assert.IsTrue(_sut.HasErrors);
            Assert.IsNotNull(_sut.ErrorMessage);
            Assert.IsTrue(_sut.ErrorMessage.Contains("Must specify an API address to log in via SSO"));
        }

        [TestMethod]
        [TestCategory("OpenSsoDialog")]
        public async Task OpenSsoDialog_ShowsSsoDialog_WhenSsoPromptRequestSucceeds()
        {
            var fakeTargetApiAddress = "junk";
            var fakeSsoPrompt = "some prompt that shows sso url";

            var fakeSsoPromptResponse = new DetailedResult<string>
            {
                Succeeded = true,
                Content = fakeSsoPrompt,
            };

            _sut.Target = fakeTargetApiAddress;

            MockCloudFoundryService.Setup(m => m.GetSsoPrompt(fakeTargetApiAddress, false))
                .ReturnsAsync(fakeSsoPromptResponse);

            await _sut.OpenSsoDialog();

            MockSsoViewModel.VerifySet(m => m.ApiAddress = fakeTargetApiAddress);
            MockSsoViewModel.Verify(m => m.ShowWithPrompt(fakeSsoPrompt, _sut), Times.Once);
        }

        [TestMethod]
        [TestCategory("CloseDialog")]
        public void CloseDialog_WrapsDialogServiceCloseDialogByName()
        {
            _sut.CloseDialog();

            MockDialogService.Verify(m => m.CloseDialogByName(nameof(LoginViewModel), null), Times.Once);
        }

        [TestMethod]
        [TestCategory("NavigateToAuthPage")]
        public async Task NavigateToAuthPage_SetsPageNumberTo2_AndSetsSsoEnabledOnTargetToTrue_WhenSsoPromptSuccessfullyRetrieved()
        {
            var fakeSsoPrompt = "junk";

            var fakeSsoPromptResult = new DetailedResult<string>
            {
                Succeeded = true,
                Content = fakeSsoPrompt,
            };

            MockCloudFoundryService.Setup(m => m.VerfiyNewApiConnection(_sut.Target, _sut.ProceedWithInvalidCertificate))
                .Returns(FakeSuccessDetailedResult);

            MockCloudFoundryService.Setup(m => m.GetSsoPrompt(_sut.Target, false))
                .ReturnsAsync(fakeSsoPromptResult);

            Assert.AreEqual(1, _sut.PageNum);

            await _sut.VerifyApiAddress();

            Assert.AreEqual(2, _sut.PageNum);
            Assert.IsTrue(_sut.SsoEnabledOnTarget);
        }
        
        [TestMethod]
        [TestCategory("NavigateToAuthPage")]
        public async Task NavigateToAuthPage_SetsCertificateInvalidToTrue_WhenCertValidationFails()
        {
            var fakeCertValidationResult = new DetailedResult
            {
                Succeeded = false,
                FailureType = FailureType.InvalidCertificate,
            };

            MockCloudFoundryService.Setup(m => m.VerfiyNewApiConnection(_sut.Target, _sut.ProceedWithInvalidCertificate))
                .Returns(fakeCertValidationResult);

            Assert.AreEqual(1, _sut.PageNum);

            await _sut.VerifyApiAddress();

            Assert.AreEqual(1, _sut.PageNum);
            Assert.IsTrue(_sut.CertificateInvalid);
        }

        [TestMethod]
        [TestCategory("NavigateToAuthPage")]
        public async Task NavigateToAuthPage_SetsPageNumberTo2_AndSetsSsoEnabledOnTargetToFalse_WhenSsoPromptAbsentFromResponse()
        {
            var fakeCertValidationResult = new DetailedResult
            {
                Succeeded = true,
            };

            var fakeSsoPromptResult = new DetailedResult<string>
            {
                Succeeded = false,
                FailureType = FailureType.MissingSsoPrompt,
            };

            MockCloudFoundryService.Setup(m => m.VerfiyNewApiConnection(_sut.Target, _sut.ProceedWithInvalidCertificate))
                .Returns(fakeCertValidationResult);

            MockCloudFoundryService.Setup(m => m.GetSsoPrompt(_sut.Target, false))
                .ReturnsAsync(fakeSsoPromptResult);

            Assert.AreEqual(1, _sut.PageNum);

            await _sut.VerifyApiAddress();

            Assert.AreEqual(2, _sut.PageNum);
            Assert.IsFalse(_sut.SsoEnabledOnTarget);
        }

        [TestMethod]
        [TestCategory("NavigateToAuthPage")]
        public async Task NavigateToAuthPage_DoesNotChangePageNumber_AndSetsApiAddressError_WhenSsoPromptRequestFails()
        {
            var fakeCertValidationResult = new DetailedResult
            {
                Succeeded = true,
            };

            var fakeSsoPromptResult = new DetailedResult<string>
            {
                Succeeded = false,
                FailureType = FailureType.None,
            };

            MockCloudFoundryService.Setup(m => m.VerfiyNewApiConnection(_sut.Target, _sut.ProceedWithInvalidCertificate))
                .Returns(fakeCertValidationResult);

            MockCloudFoundryService.Setup(m => m.GetSsoPrompt(_sut.Target, false))
                .ReturnsAsync(fakeSsoPromptResult);

            Assert.AreEqual(1, _sut.PageNum);

            await _sut.VerifyApiAddress();

            Assert.AreEqual(1, _sut.PageNum);
            Assert.IsFalse(_sut.ApiAddressIsValid);
            Assert.AreEqual($"Unable to establish a connection with {_sut.Target}", _sut.ApiAddressError);
        }

        [TestMethod]
        [TestCategory("NavigateToTargetPage")]
        public void NavigateToTargetPage_SetsPageNumTo1()
        {
            _sut.PageNum = 1234;

            Assert.AreNotEqual(1, _sut.PageNum);

            _sut.NavigateToTargetPage();

            Assert.AreEqual(1, _sut.PageNum);
        }

        [TestMethod]
        [TestCategory("CanProceedToAuthentication")]
        [DataRow("https://some.legit.address")]
        [DataRow("http://my.cool.api")]
        public void CanProceedToAuthentication_ReturnsTrue(string targetApiAddress)
        {
            _sut.Target = targetApiAddress;

            Assert.IsTrue(_sut.CanProceedToAuthentication());
        }

        [TestMethod]
        [TestCategory("CanProceedToAuthentication")]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow(null)]
        public void CanProceedToAuthentication_ReturnsFalse(string targetApiAddress)
        {
            _sut.Target = targetApiAddress;

            Assert.IsFalse(_sut.CanProceedToAuthentication());
        }

        [TestMethod]
        [TestCategory("CanProceedToAuthentication")]
        public void CanProceedToAuthentication_ReturnsFalse_WhenApiAddressIsValidMarkedAsFalse()
        {
            _sut.Target = "https://some.legit.address";
            _sut.ApiAddressIsValid = false;

            Assert.IsFalse(_sut.CanProceedToAuthentication());
        }
    }
}