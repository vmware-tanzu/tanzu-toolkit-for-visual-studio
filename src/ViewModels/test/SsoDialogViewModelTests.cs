using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Threading.Tasks;
using Tanzu.Toolkit.ViewModels.SsoDialog;

namespace Tanzu.Toolkit.ViewModels.Tests
{
    [TestClass]
    public class SsoDialogViewModelTests : ViewModelTestSupport
    {
        private SsoDialogViewModel _sut;
        private FakeCfInstanceViewModel _fakeCfInstanceViewModel;

        [TestInitialize]
        public void TestInit()
        {
            RenewMockServices();

            _fakeCfInstanceViewModel = new FakeCfInstanceViewModel(FakeCfInstance, Services);
            _sut = new SsoDialogViewModel(MockTasExplorerViewModel.Object, Services)
            {
                ApiAddress = FakeCfApiAddress,
            };
        }

        [TestCleanup]
        public void TestCleanup()
        {
            MockCloudFoundryService.VerifyAll();
            MockDialogService.VerifyAll();
            MockLoginViewModel.VerifyAll();
        }

        [TestMethod]
        [TestCategory("ctor")]
        public void Constructor_SetsHasErrorsToFalse()
        {
            Assert.IsFalse(_sut.HasErrors);
        }

        [TestMethod]
        [TestCategory("CanLoginWithPasscode")]
        public void CanLoginWithPasscode_ReturnsTrue()
        {
            Assert.IsTrue(_sut.CanLoginWithPasscode());
        }

        [TestMethod]
        [TestCategory("LoginWithPasscodeAsync")]
        public async Task LoginWithPasscodeAsync_SetsErrorMessage_WhenPasscodeIsNullOrWhitespace()
        {
            _sut.Passcode = string.Empty;

            Assert.IsFalse(_sut.HasErrors);
            Assert.IsNull(_sut.ErrorMessage);

            await _sut.LoginWithPasscodeAsync();

            Assert.IsTrue(_sut.HasErrors);
            Assert.IsNotNull(_sut.ErrorMessage);
        }

        [TestMethod]
        [TestCategory("LoginWithPasscodeAsync")]
        public async Task LoginWithPasscodeAsync_SetsErrorMessage_WhenLoginAttemptFails()
        {
            const string fakePasscode = "fake sso passcode!";

            MockTasExplorerViewModel.SetupGet(m => m.TasConnection).Returns(_fakeCfInstanceViewModel);

            MockCloudFoundryService.Setup(m => m.LoginWithSsoPasscode(_sut.ApiAddress, fakePasscode))
                .ReturnsAsync(FakeFailureDetailedResult);

            _sut.Passcode = fakePasscode;

            Assert.IsFalse(_sut.HasErrors);
            Assert.IsNull(_sut.ErrorMessage);

            await _sut.LoginWithPasscodeAsync();

            Assert.IsTrue(_sut.HasErrors);
            Assert.IsNotNull(_sut.ErrorMessage);
            Assert.AreEqual(FakeFailureDetailedResult.Explanation, _sut.ErrorMessage);
        }

        [TestMethod]
        [TestCategory("LoginWithPasscodeAsync")]
        public async Task LoginWithPasscodeAsync_SetsConnection_AndClosesSsoDialog_AndClosesLoginDialog_WhenLoginAttemptSucceeds()
        {
            const string fakePasscode = "fake sso passcode!";
            var fakeSsoDialogWindow = new object();
            var fakeParentViewModel = MockLoginViewModel.Object;

            MockTasExplorerViewModel.SetupGet(m => m.TasConnection).Returns(_fakeCfInstanceViewModel);

            MockCloudFoundryService.Setup(m => m.LoginWithSsoPasscode(_sut.ApiAddress, fakePasscode))
                .ReturnsAsync(FakeSuccessDetailedResult);

            _sut.Passcode = fakePasscode;
            _sut._loginViewModel = fakeParentViewModel;

            Assert.IsFalse(_sut.HasErrors);
            Assert.IsNull(_sut.ErrorMessage);

            await _sut.LoginWithPasscodeAsync(fakeSsoDialogWindow);

            Assert.IsFalse(_sut.HasErrors);
            Assert.IsNull(_sut.ErrorMessage);

            MockDialogService.Verify(m => m.CloseDialog(fakeSsoDialogWindow, true), Times.Once);
            MockTasExplorerViewModel.Verify(m => m.SetConnection(MockLoginViewModel.Object.TargetCf), Times.Once);
            MockLoginViewModel.Verify(m => m.CloseDialog(), Times.Once);
        }
    }
}
