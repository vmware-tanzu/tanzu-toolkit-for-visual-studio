using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tanzu.Toolkit.Models;
using Tanzu.Toolkit.Services;
using Tanzu.Toolkit.ViewModels.RemoteDebug;

namespace Tanzu.Toolkit.ViewModels.Tests
{
    [TestClass]
    public class RemoteDebugViewModelTests : ViewModelTestSupport
    {
        private const string _fakeTargetFrameworkMoniker = "some fake tfm";
        private const string _fakePathToLaunchFile = "fake\\path\\to\\launch\\file";
        private RemoteDebugViewModel _sut;

        [TestInitialize]
        public void TestInit()
        {
            MockTasExplorerViewModel.SetupGet(m => m.TasConnection).Returns((CfInstanceViewModel)null);
            MockThreadingService.Setup(m => m.StartBackgroundTask(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>())).Verifiable();

            _sut = new RemoteDebugViewModel(_fakeAppName, _fakeProjectPath, _fakeTargetFrameworkMoniker, _fakePathToLaunchFile, null, Services);
        }

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
        public void Constructor_SetsIsLoggedIn(bool mockingLoggedIn)
        {
            if (mockingLoggedIn)
            {
                MockLoggedIn();
            }
            Assert.AreEqual(mockingLoggedIn, _sut.IsLoggedIn);
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

        [TestMethod]
        [TestCategory("ctor")]
        public void Constructor_PromptsAppSelection_WhenIsLoggedIn()
        {
            var fakeOrgsResponse = new DetailedResult<List<CloudFoundryOrganization>>
            {
                Succeeded = true,
                Content = _emptyListOfOrgs,
            };

            MockCloudFoundryService.Setup(m => m.GetOrgsForCfInstanceAsync(
                It.IsAny<CloudFoundryInstance>(), It.IsAny<bool>(), It.IsAny<int>()))
                .ReturnsAsync(fakeOrgsResponse);

            MockLoggedIn();

            Assert.IsTrue(_sut.IsLoggedIn);
            Assert.AreEqual("Select app to debug:", _sut.DialogMessage);
            Assert.IsNull(_sut.LoadingMessage);
        }

        [TestMethod]
        [TestCategory("ctor")]
        public void Constructor_DoesNotPromptAppSelection_WhenNotLoggedIn()
        {
            MockTasExplorerViewModel.SetupGet(m => m.TasConnection).Returns((CfInstanceViewModel)null);
            MockCloudFoundryService.Invocations.Clear(); // reset any invocations from test init construction

            _sut = new RemoteDebugViewModel(_fakeAppName, _fakeProjectPath, _fakeTargetFrameworkMoniker, _fakePathToLaunchFile, null, Services);

            Assert.IsNull(_sut._tasExplorer.TasConnection);

            MockCloudFoundryService.Verify(m => m.GetOrgsForCfInstanceAsync(It.IsAny<CloudFoundryInstance>(), It.IsAny<bool>(), It.IsAny<int>()), Times.Never);
            
            Assert.IsNull(_sut.LoadingMessage);
            Assert.IsNull(_sut.DialogMessage);
        }

        private void MockLoggedIn()
        {
            var fakeTasConnection = new FakeCfInstanceViewModel(_fakeCfInstance, Services);
            MockThreadingService.Invocations.Clear(); // reset invocations from test init
            MockTasExplorerViewModel.SetupGet(m => m.TasConnection).Returns(fakeTasConnection);

            _sut = new RemoteDebugViewModel(_fakeAppName, _fakeProjectPath, _fakeTargetFrameworkMoniker, _fakePathToLaunchFile, null, Services);
        }
    }
}
