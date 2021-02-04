using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Security;
using System.Threading.Tasks;
using Tanzu.Toolkit.VisualStudio.Services.CloudFoundry;

namespace Tanzu.Toolkit.VisualStudio.ViewModels.Tests
{
    [TestClass]
    public class AddCloudDialogViewModelTests : ViewModelTestSupport
    {
        static AddCloudDialogViewModel vm;
        static readonly string fakeInstanceName = "My Fake CF";
        static readonly string fakeTarget = "http://my.fake.target";
        static readonly string fakeUsername = "correct-username";
        static readonly SecureString fakeSecurePw = new SecureString();
        static readonly string fakeHttpProxy = "junk";
        static readonly bool skipSsl = true;
        static readonly string fakeToken = "junk";

        [TestInitialize]
        public void TestInit()
        {
            vm = new AddCloudDialogViewModel(services);
            vm.InstanceName = fakeInstanceName;
            vm.Target = fakeTarget;
            vm.Username = fakeUsername;
            vm.GetPassword = () => { return fakeSecurePw; };
            vm.HttpProxy = fakeHttpProxy;
            vm.SkipSsl = skipSsl;
        }

        [TestMethod]
        public void AddCloudFoundryInstance_SetsErrorMessage_WhenTargetUriNull()
        {
            bool ErrorMessagePropertyChangedCalled = false;
            vm.Target = null;

            vm.PropertyChanged += (s, args) =>
            {
                if ("ErrorMessage" == args.PropertyName) ErrorMessagePropertyChangedCalled = true;
            };

            vm.AddCloudFoundryInstance(null);

            Assert.IsTrue(ErrorMessagePropertyChangedCalled);
            Assert.IsTrue(vm.HasErrors);
            Assert.AreEqual(AddCloudDialogViewModel.TargetEmptyMessage, vm.ErrorMessage);

            mockDialogService.Verify(ds => ds.CloseDialog(It.IsAny<object>(), true), Times.Never);
            mockDialogService.Verify(ds => ds.ShowDialog(It.IsAny<string>(), null), Times.Never);
            mockCloudFoundryService.Verify(mock => mock.ConnectToCFAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SecureString>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        }

        [TestMethod]
        public async Task AddCloudFoundryInstance_SetsErrorMessage_WhenTargetUriMalformed()
        {
            bool ErrorMessagePropertyChangedCalled = false;
            vm.Target = "some-poorly-formatted-uri";

            vm.PropertyChanged += (s, args) =>
            {
                if ("ErrorMessage" == args.PropertyName) ErrorMessagePropertyChangedCalled = true;
            };

            await vm.AddCloudFoundryInstance(null);

            Assert.IsTrue(ErrorMessagePropertyChangedCalled);
            Assert.IsTrue(vm.HasErrors);
            Assert.AreEqual(AddCloudDialogViewModel.TargetInvalidFormatMessage, vm.ErrorMessage);

            mockDialogService.Verify(ds => ds.CloseDialog(It.IsAny<object>(), true), Times.Never);
            mockDialogService.Verify(ds => ds.ShowDialog(It.IsAny<string>(), null), Times.Never);
            mockCloudFoundryService.Verify(mock => mock.ConnectToCFAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SecureString>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        }

        [TestMethod]
        public async Task AddCloudFoundryInstance_SetsErrorMessage_WhenLoginRequestFails()
        {
            const string expectedErrorMessage = "my fake error message";

            mockCloudFoundryService.Setup(mock => mock.ConnectToCFAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SecureString>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(new ConnectResult(false, expectedErrorMessage, null));

            await vm.AddCloudFoundryInstance(null);

            Assert.IsTrue(vm.HasErrors);
            Assert.AreEqual(expectedErrorMessage, vm.ErrorMessage);
            mockCloudFoundryService.Verify(mock => mock.ConnectToCFAsync(fakeTarget, fakeUsername, fakeSecurePw, fakeHttpProxy, skipSsl), Times.Once);
            mockDialogService.Verify(ds => ds.CloseDialog(It.IsAny<object>(), true), Times.Never);
        }

        [TestMethod]
        public async Task AddCloudFoundryInstance_ClosesDialog_WhenLoginRequestSucceeds()
        {
            mockCloudFoundryService.Setup(mock => mock.ConnectToCFAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SecureString>(), It.IsAny<string>(), It.IsAny<bool>()))
               .ReturnsAsync(new ConnectResult(true, null, fakeToken));

            await vm.AddCloudFoundryInstance(null);

            Assert.IsFalse(vm.HasErrors);
            mockCloudFoundryService.Verify(mock => mock.ConnectToCFAsync(fakeTarget, fakeUsername, fakeSecurePw, fakeHttpProxy, skipSsl), Times.Once);
            mockDialogService.Verify(mock => mock.CloseDialog(It.IsAny<object>(), It.IsAny<bool>()), Times.Once);
            mockDialogService.Verify(ds => ds.CloseDialog(It.IsAny<object>(), true), Times.Once);
        }

        [TestMethod]
        public async Task AddCloudFoundryInstance_DoesNotCloseDialog_WhenLoginRequestFails()
        {
            const string expectedErrorMessage = "my fake error message";
            const string cloudName = "my fake instance name";

            mockCloudFoundryService.Setup(mock => mock.ConnectToCFAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SecureString>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(new ConnectResult(false, expectedErrorMessage, null));

            vm.InstanceName = cloudName;
            await vm.AddCloudFoundryInstance(null);

            Assert.IsNotNull(vm.ErrorMessage);
            mockDialogService.Verify(mock => mock.CloseDialog(It.IsAny<object>(), It.IsAny<bool>()), Times.Never);
            mockCloudFoundryService.Verify(mock => mock.AddCloudFoundryInstance(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            mockCloudFoundryService.VerifyAll();
        }

        [TestMethod]
        public async Task AddCloudFoundryInstance_SetsErrorMessage_WhenAddCloudFoundryInstanceThrowsException()
        {
            string duplicateName = "I was already added";
            string errorMsg = "fake error message thrown by CF service";

            mockCloudFoundryService.Setup(mock => mock.ConnectToCFAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SecureString>(), It.IsAny<string>(), It.IsAny<bool>()))
               .ReturnsAsync(new ConnectResult(true, null, fakeToken));

            mockCloudFoundryService.Setup(mock => mock.AddCloudFoundryInstance(duplicateName, fakeTarget, fakeToken))
                .Throws(new Exception(errorMsg));

            vm.InstanceName = duplicateName;
            await vm.AddCloudFoundryInstance(null);

            Assert.IsTrue(vm.HasErrors);
            Assert.AreEqual(errorMsg, vm.ErrorMessage);
            mockCloudFoundryService.VerifyAll();
            mockDialogService.Verify(mock => mock.CloseDialog(It.IsAny<object>(), It.IsAny<bool>()), Times.Never);
        }

    }
}
