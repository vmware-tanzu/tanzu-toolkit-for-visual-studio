using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tanzu.Toolkit.Models;

namespace Tanzu.Toolkit.ViewModels.Tests
{
    [TestClass]
    public class AppViewModelTests : ViewModelTestSupport
    {
        private AppViewModel _avm;
        [TestInitialize]
        public void TestInit()
        {
            RenewMockServices();
        }

        [TestMethod]
        public void Constructor_SetsDisplayTextToAppName()
        {
            string appName = "junk";
            var fakeApp = new CloudFoundryApp(appName, null, null, null);

            _avm = new AppViewModel(fakeApp, Services);

            Assert.AreEqual(appName, _avm.DisplayText);
        }

        [TestMethod]
        public void IsStopped_ReturnsTrue_WhenAppStateIsSTOPPED()
        {
            var fakeApp = new CloudFoundryApp("fake name", "fake guid", null, null)
            {
                State = "STOPPED",
            };

            _avm = new AppViewModel(fakeApp, Services);

            Assert.IsTrue(_avm.IsStopped);
        }

        [TestMethod]
        public void IsStopped_ReturnsFalse_WhenAppStateIsNotSTOPPED()
        {
            var fakeApp = new CloudFoundryApp("fake name", "fake guid", null, null)
            {
                State = "anything-other-than-STOPPED",
            };

            _avm = new AppViewModel(fakeApp, Services);

            Assert.IsFalse(_avm.IsStopped);
        }
    }
}
