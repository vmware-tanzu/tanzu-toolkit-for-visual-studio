using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using Tanzu.Toolkit.Models;

namespace Tanzu.Toolkit.ViewModels.Tests
{
    [TestClass]
    public class AppViewModelTests : ViewModelTestSupport
    {
        private AppViewModel _sut;
        private List<string> _receivedEvents;

        [TestInitialize]
        public void TestInit()
        {
            RenewMockServices();
            _receivedEvents = [];
        }

        [TestMethod]
        public void Constructor_SetsDisplayTextToAppName()
        {
            var appName = "junk";
            var fakeApp = new CloudFoundryApp(appName, null, null, null);

            _sut = new AppViewModel(fakeApp, Services);

            Assert.AreEqual(appName, _sut.DisplayText);
        }

        [TestMethod]
        public void IsStopped_ReturnsTrue_WhenAppStateIsSTOPPED()
        {
            var fakeApp = new CloudFoundryApp("fake name", "fake guid", null, null) { State = "STOPPED" };

            _sut = new AppViewModel(fakeApp, Services);

            Assert.IsTrue(_sut.IsStopped);
        }

        [TestMethod]
        public void IsStopped_ReturnsFalse_WhenAppStateIsNotSTOPPED()
        {
            var fakeApp = new CloudFoundryApp("fake name", "fake guid", null, null) { State = "anything-other-than-STOPPED" };

            _sut = new AppViewModel(fakeApp, Services);

            Assert.IsFalse(_sut.IsStopped);
        }

        [TestMethod]
        public void RefreshApp_RaisesPropertyChangedEventForIsStopped()
        {
            var fakeApp = new CloudFoundryApp("fake app name", "fake app guid", null, null);
            _sut = new AppViewModel(fakeApp, Services);

            _sut.PropertyChanged += (sender, e) => { _receivedEvents.Add(e.PropertyName); };

            _sut.RefreshAppState();

            Assert.HasCount(1, _receivedEvents);
            Assert.AreEqual("IsStopped", _receivedEvents[0]);
        }
    }
}