using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

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
        public void ConnectToCloudFoundry_WhenTargetUriNull()
        {
            var vm = new LoginDialogViewModel(services);
            bool ErrorMessagePropertyChangedCalled = false;
            vm.Target = null;

            vm.PropertyChanged += (s, args) =>
            {
                Assert.AreEqual("ErrorMessage", args.PropertyName);
                ErrorMessagePropertyChangedCalled = true;
            };

            vm.ConnectToCloudFoundry(null);

            Assert.IsTrue(ErrorMessagePropertyChangedCalled);
            Assert.AreEqual(LoginDialogViewModel.TargetEmptyMessage, vm.ErrorMessage);
            Assert.IsFalse(vm.IsLoggedIn);

            mockDialogService.Verify(ds => ds.CloseDialog(It.IsAny<object>(), true), Times.Never);
            mockDialogService.Verify(ds => ds.ShowDialog(It.IsAny<string>(), null), Times.Never);
        }
    }
}
