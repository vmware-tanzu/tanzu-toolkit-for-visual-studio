﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tanzu.Toolkit.VisualStudio.Models;

namespace Tanzu.Toolkit.VisualStudio.ViewModels.Tests
{
    [TestClass]

    public class AppViewModelTests : ViewModelTestSupport
    {
        private AppViewModel avm;

        [TestMethod]
        public void Constructor_SetsDisplayTextToAppName()
        {
            string appName = "junk";
            var fakeApp = new CloudFoundryApp(appName, null, null, null);

            avm = new AppViewModel(fakeApp, services);

            Assert.AreEqual(appName, avm.DisplayText);
        }

        [TestMethod]
        public void IsStopped_ReturnsTrue_WhenAppStateIsSTOPPED()
        {
            var fakeApp = new CloudFoundryApp("fake name", "fake guid", null, null)
            {
                State = "STOPPED"
            };

            avm = new AppViewModel(fakeApp, services);

            Assert.IsTrue(avm.IsStopped);
        }

        [TestMethod]
        public void IsStopped_ReturnsFalse_WhenAppStateIsNotSTOPPED()
        {
            var fakeApp = new CloudFoundryApp("fake name", "fake guid", null, null)
            {
                State = "anything-other-than-STOPPED"
            };

            avm = new AppViewModel(fakeApp, services);

            Assert.IsFalse(avm.IsStopped);
        }
    }
}
