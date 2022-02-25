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

            _receivedEvents = new List<string>();
            FakeSecurePw = new NetworkCredential("", FakePwStr).SecurePassword;

            _sut = new LoginViewModel(Services)
            {
                ClearPassword = () => { },
                ConnectionName = FakeConnectionName,
                TargetApiAddress = FakeTarget,
                Username = FakeUsername,
                GetPassword = () => { return FakeSecurePw; },
                PasswordEmpty = () => { return false; },
                SkipSsl = SkipSsl,
                CfClient = MockCloudFoundryService.Object,
            };

            _sut.PropertyChanged += (sender, e) =>
            {
                _receivedEvents.Add(e.PropertyName);
            };

            _sut.CfClient.ConfigureForCf(FakeCfInstance);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            MockCloudFoundryService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("ctor")]
        public void Constructor_SetsIsApiAddressFormatValid_ToFalse()
        {
            Assert.IsFalse(_sut.IsApiAddressFormatValid);
        }

        [TestMethod]
        [TestCategory("LogIn")]
        public async Task LogIn_SetsErrorMessage_WhenLoginRequestFails()
        {
            const string expectedErrorMessage = "my fake error message";

            MockCloudFoundryService.Setup(mock => mock.LoginWithCredentials(FakeUsername, FakeSecurePw))
                .ReturnsAsync(new DetailedResult(false, expectedErrorMessage));

            await _sut.LogIn();

            Assert.IsTrue(_sut.HasErrors);
            Assert.AreEqual(expectedErrorMessage, _sut.ErrorMessage);
            MockDialogService.Verify(ds => ds.CloseDialog(It.IsAny<object>(), true), Times.Never);
        }

        [TestMethod]
        [TestCategory("LogIn")]
        public async Task LogIn_SetsConnectionOnTasExplorer_WhenLoginRequestSucceeds()
        {
            _sut.TargetCf = FakeCfInstance;

            MockCloudFoundryService.Setup(mock => mock.
              LoginWithCredentials(FakeUsername, FakeSecurePw))
                .ReturnsAsync(new DetailedResult(true, null));

            MockTasExplorerViewModel.Setup(m => m.SetConnection(_sut.TargetCf)).Verifiable();

            await _sut.LogIn();

            Assert.IsFalse(_sut.HasErrors);
            MockDialogService.Verify(mock => mock.CloseDialog(It.IsAny<object>(), It.IsAny<bool>()), Times.Once);
            MockDialogService.Verify(ds => ds.CloseDialog(It.IsAny<object>(), true), Times.Once);

            MockTasExplorerViewModel.VerifyAll();
        }

        [TestMethod]
        [TestCategory("LogIn")]
        public async Task LogIn_ClosesDialog_WhenLoginRequestSucceeds()
        {
            MockCloudFoundryService.Setup(mock => mock.LoginWithCredentials(FakeUsername, FakeSecurePw))
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
            Assert.IsNotNull(_sut.TargetApiAddress);
            Assert.IsNotNull(_sut.Username);
            Assert.IsFalse(_sut.PasswordEmpty());
            Assert.IsTrue(_sut.ValidateApiAddressFormat(_sut.TargetApiAddress));

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
            _sut.TargetApiAddress = invalidTargetApiAddress;

            Assert.IsTrue(string.IsNullOrWhiteSpace(_sut.TargetApiAddress));

            Assert.IsFalse(_sut.CanLogIn());
        }

        [TestMethod]
        [TestCategory("CanLogIn")]
        [DataRow("asdf")]
        [DataRow("^%$(**")]
        [DataRow("a.bad.url")]
        public void CanLogIn_ReturnsFalse_WhenTargetFailsVerification(string invalidTargetApiAddress)
        {
            _sut.TargetApiAddress = invalidTargetApiAddress;

            Assert.IsFalse(_sut.ValidateApiAddressFormat(_sut.TargetApiAddress));

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
            Assert.IsNotNull(_sut.TargetApiAddress);
            Assert.IsTrue(string.IsNullOrWhiteSpace(_sut.Username));
            Assert.IsFalse(_sut.PasswordEmpty());
            Assert.IsTrue(_sut.ValidateApiAddressFormat(_sut.TargetApiAddress));

            Assert.IsFalse(_sut.CanLogIn());
        }

        [TestMethod]
        [TestCategory("CanLogIn")]
        public void CanLogIn_ReturnsFalse_WhenPasswordEmpty()
        {
            // mock out testimony from login view that password box is empty
            _sut.PasswordEmpty = () => { return true; };

            Assert.IsNotNull(_sut.ConnectionName);
            Assert.IsNotNull(_sut.TargetApiAddress);
            Assert.IsNotNull(_sut.Username);
            Assert.IsTrue(_sut.PasswordEmpty());
            Assert.IsTrue(_sut.ValidateApiAddressFormat(_sut.TargetApiAddress));

            Assert.IsFalse(_sut.CanLogIn());
        }

        [TestMethod]
        [TestCategory("CanOpenSsoDialog")]
        [DataRow("http://some.api.address")]
        [DataRow("https://some.api.address")]
        public void CanOpenSsoDialog_ReturnsTrue_WhenTargetApiAddressIsValid(string targetApiAddress)
        {
            _sut.TargetApiAddress = targetApiAddress;

            _sut.IsApiAddressFormatValid = true;

            Assert.IsTrue(_sut.CanOpenSsoDialog());
        }

        [TestMethod]
        [TestCategory("CanOpenSsoDialog")]
        [DataRow(null)]
        [DataRow("")]
        [DataRow(" ")]
        public void CanOpenSsoDialog_ReturnsFalse_WhenTargetApiAddressIsNullOrWhitespace(string targetApiAddress)
        {
            _sut.TargetApiAddress = targetApiAddress;

            _sut.IsApiAddressFormatValid = true;

            Assert.IsFalse(_sut.CanOpenSsoDialog());
        }

        [TestMethod]
        [TestCategory("CanOpenSsoDialog")]
        public void CanOpenSsoDialog_ReturnsFalse_WhenTargetApiAddressIsInvalid()
        {
            _sut.TargetApiAddress = "junk";

            _sut.IsApiAddressFormatValid = false;

            Assert.IsFalse(_sut.CanOpenSsoDialog());
        }

        [TestMethod]
        [TestCategory("OpenSsoDialog")]
        [DataRow(null)]
        [DataRow("")]
        [DataRow(" ")]
        public async Task OpenSsoDialog_SetsErrorMessage_WhenTargetApiAddressIsNullOrWhitespace(string targetApiAddress)
        {
            _sut.TargetApiAddress = targetApiAddress;

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

            _sut.TargetApiAddress = fakeTargetApiAddress;

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
        [TestCategory("ConnectToCf")]
        public async Task ConnectToCf_SetsPageNumberTo2_AndSetsSsoEnabledOnTargetToTrue_WhenSsoPromptSuccessfullyRetrieved()
        {
            var fakeSsoPrompt = "junk";
            var fakeSsoPromptResult = new DetailedResult<string>
            {
                Succeeded = true,
                Content = fakeSsoPrompt,
            };

            MockCloudFoundryService.Setup(m => m.TargetCfApi(_sut.TargetApiAddress, _sut.SkipSsl))
                .Returns(FakeSuccessDetailedResult);
            MockCloudFoundryService.Setup(m => m.GetSsoPrompt(_sut.TargetApiAddress, _sut.SkipSsl))
                .ReturnsAsync(fakeSsoPromptResult);
            MockCloudFoundryService.Setup(m => m.ConfigureForCf(It.IsAny<CloudFoundryInstance>())).Returns(FakeSuccessDetailedResult);

            Assert.AreEqual(1, _sut.PageNum);

            await _sut.ConnectToCf();

            Assert.AreEqual(2, _sut.PageNum);
            Assert.IsTrue(_sut.SsoEnabledOnTarget);
        }

        [TestMethod]
        [TestCategory("ConnectToCf")]
        public async Task ConnectToCf_SetsCertificateInvalidToTrue_WhenCertValidationFails()
        {
            var fakeCertValidationResult = new DetailedResult
            {
                Succeeded = false,
                FailureType = FailureType.InvalidCertificate,
            };

            MockCloudFoundryService.Setup(m => m.TargetCfApi(_sut.TargetApiAddress, _sut.SkipSsl))
                .Returns(fakeCertValidationResult);
            MockCloudFoundryService.Setup(m => m.ConfigureForCf(It.IsAny<CloudFoundryInstance>())).Returns(FakeSuccessDetailedResult);

            Assert.AreEqual(1, _sut.PageNum);

            await _sut.ConnectToCf();

            Assert.AreEqual(1, _sut.PageNum);
            Assert.IsTrue(_sut.CertificateInvalid);
        }

        [TestMethod]
        [TestCategory("ConnectToCf")]
        public async Task ConnectToCf_SetsPageNumberTo2_AndSetsSsoEnabledOnTargetToFalse_WhenSsoPromptAbsentFromResponse()
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

            MockCloudFoundryService.Setup(m => m.TargetCfApi(_sut.TargetApiAddress, _sut.SkipSsl))
                .Returns(fakeCertValidationResult);
            MockCloudFoundryService.Setup(m => m.GetSsoPrompt(_sut.TargetApiAddress, _sut.SkipSsl))
                .ReturnsAsync(fakeSsoPromptResult);
            MockCloudFoundryService.Setup(m => m.ConfigureForCf(It.IsAny<CloudFoundryInstance>())).Returns(FakeSuccessDetailedResult);

            Assert.AreEqual(1, _sut.PageNum);

            await _sut.ConnectToCf();

            Assert.AreEqual(2, _sut.PageNum);
            Assert.IsFalse(_sut.SsoEnabledOnTarget);
        }

        [TestMethod]
        [TestCategory("ConnectToCf")]
        public async Task ConnectToCf_DoesNotChangePageNumber_AndSetsApiAddressError_WhenSsoPromptRequestFails()
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

            MockCloudFoundryService.Setup(m => m.TargetCfApi(_sut.TargetApiAddress, _sut.SkipSsl))
                .Returns(fakeCertValidationResult);
            MockCloudFoundryService.Setup(m => m.GetSsoPrompt(_sut.TargetApiAddress, _sut.SkipSsl))
                .ReturnsAsync(fakeSsoPromptResult);
            MockCloudFoundryService.Setup(m => m.ConfigureForCf(It.IsAny<CloudFoundryInstance>())).Returns(FakeSuccessDetailedResult);

            Assert.AreEqual(1, _sut.PageNum);

            await _sut.ConnectToCf();

            Assert.AreEqual(1, _sut.PageNum);
            Assert.IsFalse(_sut.IsApiAddressFormatValid);
            Assert.AreEqual($"Unable to establish a connection with {_sut.TargetApiAddress}", _sut.ApiAddressError);
        }


        [TestMethod]
        [TestCategory("ConnectToCf")]
        [DataRow("_", false, LoginViewModel.TargetInvalidFormatMessage)]
        [DataRow("www.api.com", false, LoginViewModel.TargetInvalidFormatMessage)]
        [DataRow("http://www.api.com", true, null)]
        [DataRow("https://my.cool.url", true, null)]
        public void ConnectToCf_SetsIsApiAddressFormatValid_AndSetsApiAddressError(string apiAddr, bool expectedValidity, string expectedError)
        {
            _sut.ValidateApiAddressFormat(apiAddr);
            Assert.AreEqual(expectedValidity, _sut.IsApiAddressFormatValid);
            Assert.AreEqual(expectedError, _sut.ApiAddressError);
            Assert.IsTrue(_receivedEvents.Contains("IsApiAddressFormatValid"));
        }

        [TestMethod]
        [TestCategory("ConnectToCf")]
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
        public async Task ConnectToCf_SetsTargetCfName_WhenVerificationSucceeds(string apiAddressFromDialog, string connectionNameFromDialog, string expectedCfName)
        {
            _sut.TargetApiAddress = apiAddressFromDialog;
            _sut.ConnectionName = connectionNameFromDialog;
            var fakeSsoPrompt = "junk";
            var fakeSsoPromptResult = new DetailedResult<string>
            {
                Succeeded = true,
                Content = fakeSsoPrompt,
            };

            MockCloudFoundryService.Setup(m => m.TargetCfApi(_sut.TargetApiAddress, _sut.SkipSsl))
                .Returns(FakeSuccessDetailedResult);
            MockCloudFoundryService.Setup(m => m.GetSsoPrompt(_sut.TargetApiAddress, _sut.SkipSsl))
                .ReturnsAsync(fakeSsoPromptResult);
            MockCloudFoundryService.Setup(m => m.ConfigureForCf(It.IsAny<CloudFoundryInstance>()))
                .Returns(FakeSuccessDetailedResult);

            Assert.IsNull(_sut.TargetCf);

            await _sut.ConnectToCf();

            Assert.IsNotNull(_sut.TargetCf);
            Assert.AreEqual(expectedCfName, _sut.TargetCf.InstanceName);
        }

        [TestMethod]
        [TestCategory("ConnectToCf")]
        [DataRow(true)]
        [DataRow(false)]
        public async Task ConnectToCf_SetsTargetCfSkipCertValidationValue(bool skipSsl)
        {
            _sut.SkipSsl = skipSsl;
            var fakeSsoPrompt = "junk";
            var fakeSsoPromptResult = new DetailedResult<string>
            {
                Succeeded = true,
                Content = fakeSsoPrompt,
            };

            MockCloudFoundryService.Setup(m => m.TargetCfApi(_sut.TargetApiAddress, _sut.SkipSsl))
                .Returns(FakeSuccessDetailedResult);
            MockCloudFoundryService.Setup(m => m.GetSsoPrompt(_sut.TargetApiAddress, _sut.SkipSsl))
                .ReturnsAsync(fakeSsoPromptResult);
            MockCloudFoundryService.Setup(m => m.ConfigureForCf(It.IsAny<CloudFoundryInstance>()))
                .Returns(FakeSuccessDetailedResult);

            Assert.IsNull(_sut.TargetCf);

            await _sut.ConnectToCf();

            Assert.IsNotNull(_sut.TargetCf);
            Assert.AreEqual(skipSsl, _sut.TargetCf.SkipSslCertValidation);
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
            _sut.TargetApiAddress = targetApiAddress;
            Assert.IsTrue(_sut.ValidateApiAddressFormat(_sut.TargetApiAddress));
            Assert.IsTrue(_sut.CanProceedToAuthentication());
        }

        [TestMethod]
        [TestCategory("CanProceedToAuthentication")]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow(null)]
        public void CanProceedToAuthentication_ReturnsFalse(string targetApiAddress)
        {
            _sut.TargetApiAddress = targetApiAddress;
            Assert.IsFalse(_sut.CanProceedToAuthentication());
        }

        [TestMethod]
        [TestCategory("CanProceedToAuthentication")]
        public void CanProceedToAuthentication_ReturnsFalse_WhenApiAddressIsValidMarkedAsFalse()
        {
            _sut.TargetApiAddress = "https://some.legit.address";
            _sut.IsApiAddressFormatValid = false;
            Assert.IsFalse(_sut.CanProceedToAuthentication());
        }

    }
}