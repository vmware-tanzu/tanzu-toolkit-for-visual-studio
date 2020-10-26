using Microsoft.VisualStudio.TestTools.UnitTesting;
using TanzuForVS.Models;

namespace TanzuForVS.ViewModels
{
    [TestClass]

    public class AppViewModelTests : ViewModelTestSupport
    {
        private AppViewModel avm;

        [TestMethod]
        public void Constructor_SetsDisplayTextToAppName()
        {
            string appName = "junk";
            var fakeApp = new CloudFoundryApp(appName, null, null);

            avm = new AppViewModel(fakeApp, services);

            Assert.AreEqual(appName, avm.DisplayText);
        }
    }
}
