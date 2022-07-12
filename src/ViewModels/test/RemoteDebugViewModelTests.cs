using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Tanzu.Toolkit.ViewModels.RemoteDebug;

namespace Tanzu.Toolkit.ViewModels.Tests
{
    [TestClass]
    public class RemoteDebugViewModelTests : ViewModelTestSupport
    {
        private const string _fakeTargetFrameworkMoniker = "some fake tfm";
        private const string _fakePathToLaunchFile = "fake\\path\\to\\launch\\file";
        private RemoteDebugViewModel _sut;
        private Action<string, string> _fakeDebugCallback;

        [TestInitialize]
        public void TestInit()
        {
            var fakeTasConnection = new FakeCfInstanceViewModel(_fakeCfInstance, Services);

            _sut = new RemoteDebugViewModel(_fakeAppName, _fakeProjectPath, _fakeTargetFrameworkMoniker, _fakePathToLaunchFile, _fakeDebugCallback, Services);
            MockTasExplorerViewModel.SetupGet(m => m.TasConnection).Returns(fakeTasConnection);
        }

        [TestCleanup]
        public void TestCleanup() { }

        [TestMethod]
        [TestCategory("ctor")]
        public void Constructor_SetsAppToDebugToNull()
        {
            Assert.IsNull(_sut.AppToDebug);
        }

        [TestMethod]
        [TestCategory("ctor")]
        public void Constructor_SetsLoadingMessageToNull()
        {
            Assert.IsNull(_sut.LoadingMessage);
        }

        [TestMethod]
        [TestCategory("ctor")]
        [DataRow(true)]
        [DataRow(false)]
        public void Constructor_SetsIsLoggedIn(bool mockingLoggedOut)
        {
            var expectedValueForIsLoggedIn = !mockingLoggedOut;
            if (mockingLoggedOut)
            {
                MockTasExplorerViewModel.SetupGet(m => m.TasConnection).Returns((CfInstanceViewModel)null);
            }
            _sut = new RemoteDebugViewModel(_fakeAppName, _fakeProjectPath, _fakeTargetFrameworkMoniker, _fakePathToLaunchFile, _fakeDebugCallback, Services);
            Assert.AreEqual(expectedValueForIsLoggedIn, _sut.IsLoggedIn);
        }

        [TestMethod]
        [TestCategory("ctor")]
        public void Constructor_SetsWaitingOnAppConfirmationToFalse()
        {
            Assert.IsFalse(_sut.WaitingOnAppConfirmation);
        }

        [TestMethod]
        [TestCategory("ctor")]
        public void Constructor_SetsCanCancelToTrue()
        {
            Assert.IsTrue(_sut.CanCancel);
        }

        [TestMethod]
        [TestCategory("ctor")]
        public void Constructor_SetsCancelDebuggingAction()
        {
            Assert.IsNotNull(_sut.CancelDebugging);
        }
    }
}
