using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Tanzu.Toolkit.VisualStudio.Models;
using Tanzu.Toolkit.VisualStudio.Services;
using static Tanzu.Toolkit.VisualStudio.Services.OutputHandler.OutputHandler;

namespace Tanzu.Toolkit.VisualStudio.ViewModels.Tests
{
    [TestClass()]
    public class DeploymentDialogViewModelTests : ViewModelTestSupport
    {
        private static CloudFoundryInstance _fakeCfInstance = new CloudFoundryInstance("", "", "");
        private static CloudFoundryOrganization _fakeOrg = new CloudFoundryOrganization("", "", _fakeCfInstance);
        private CloudFoundrySpace _fakeSpace = new CloudFoundrySpace("", "", _fakeOrg);
        private const string _fakeAppName = "fake app name";
        private const string _fakeProjPath = "this\\is\\a\\fake\\path\\to\\a\\project\\directory";
        private DeploymentDialogViewModel _sut;

        [TestInitialize]
        public void TestInit()
        {
            mockCloudFoundryService.SetupGet(mock => mock.CloudFoundryInstances).Returns(new Dictionary<string, CloudFoundryInstance>());

            _sut = new DeploymentDialogViewModel(services, _fakeProjPath);
        }

        [TestMethod()]
        public void DeploymentDialogViewModel_GetsListOfCfsFromCfService_WhenConstructed()
        {
            var vm = new DeploymentDialogViewModel(services, _fakeProjPath);

            mockCloudFoundryService.VerifyAll();
        }

        [TestMethod()]
        public async Task DeploymentDialogViewModel_DeployAppStatusIsUpdated_WhenDeployAppAsyncSucceeds()
        {
            _sut.AppName = _fakeAppName;
            _sut.SelectedCf = _fakeCfInstance;
            _sut.SelectedOrg = _fakeOrg;
            _sut.SelectedSpace = _fakeSpace;

            mockCloudFoundryService.Setup(mock => mock.DeployAppAsync(_fakeCfInstance,
                _fakeOrg, _fakeSpace, _fakeAppName, _fakeProjPath, It.IsAny<StdOutDelegate>()))
                .ReturnsAsync(new DetailedResult(true));

            bool DeploymentStatusPropertyChangedCalled = false;

            _sut.PropertyChanged += (s, args) =>
            {
                if ("DeploymentStatus" == args.PropertyName) DeploymentStatusPropertyChangedCalled = true;
            };

            await _sut.DeployApp(null);

            Assert.IsTrue(DeploymentStatusPropertyChangedCalled);
            Assert.IsTrue(_sut.DeploymentStatus.Contains(DeploymentDialogViewModel.deploymentSuccessMsg));
            mockCloudFoundryService.VerifyAll();
        }

        [TestMethod()]
        public async Task DeploymentDialogViewModel_DeployAppStatusIsUpdated_WhenDeployAppAsyncFails()
        {
            _sut.AppName = _fakeAppName;
            _sut.SelectedCf = _fakeCfInstance;
            _sut.SelectedOrg = _fakeOrg;
            _sut.SelectedSpace = _fakeSpace;

            const string fakeFailureMsg = "it failed";
            mockCloudFoundryService.Setup(mock => mock.DeployAppAsync(_fakeCfInstance,
               _fakeOrg, _fakeSpace, _fakeAppName, _fakeProjPath, It.IsAny<StdOutDelegate>()))
               .ReturnsAsync(new DetailedResult(false, fakeFailureMsg));

            bool DeploymentStatusPropertyChangedCalled = false;

            _sut.PropertyChanged += (s, args) =>
            {
                if ("DeploymentStatus" == args.PropertyName) DeploymentStatusPropertyChangedCalled = true;
            };

            await _sut.DeployApp(null);

            Assert.IsTrue(DeploymentStatusPropertyChangedCalled);
            Assert.IsTrue(_sut.DeploymentStatus.Contains(fakeFailureMsg));
            mockCloudFoundryService.VerifyAll();
        }

        [TestMethod]
        public async Task DeployApp_UpdatesDeploymentStatus_WithExceptionMessage_WhenAnExceptionIsThrown()
        {
            var fakeExMsg = "I was thrown by cf service!";
            var fakeExTrace = "this is a stack trace: a<b<c<d<e";
            var fakeException = new FakeException(fakeExMsg, fakeExTrace);

            var receivedEvents = new List<string>();
            _sut.PropertyChanged += delegate (object sender, PropertyChangedEventArgs e)
            {
                receivedEvents.Add(e.PropertyName);
            };

            mockCloudFoundryService.Setup(mock =>
                mock.DeployAppAsync(_fakeCfInstance, _fakeOrg, _fakeSpace, _fakeAppName, _fakeProjPath, It.IsAny<StdOutDelegate>()))
                    .ThrowsAsync(fakeException);

            _sut.AppName = _fakeAppName;
            _sut.SelectedCf = _fakeCfInstance;
            _sut.SelectedOrg = _fakeOrg;
            _sut.SelectedSpace = _fakeSpace;

            await _sut.DeployApp(null);

            Assert.IsTrue(receivedEvents.Contains("DeploymentStatus"));
            Assert.IsTrue(_sut.DeploymentStatus.Contains(fakeExMsg));
            Assert.IsFalse(_sut.DeploymentStatus.Contains(fakeExTrace));
        }

        [TestMethod]
        public async Task DeployApp_UpdatesDeploymentStatus_WhenAppNameEmpty()
        {
            var receivedEvents = new List<string>();
            _sut.PropertyChanged += delegate (object sender, PropertyChangedEventArgs e)
            {
                receivedEvents.Add(e.PropertyName);
            };

            _sut.AppName = string.Empty;
            _sut.SelectedCf = _fakeCfInstance;
            _sut.SelectedOrg = _fakeOrg;
            _sut.SelectedSpace = _fakeSpace;

            await _sut.DeployApp(null);

            Assert.IsTrue(receivedEvents.Contains("DeploymentStatus"));
            Assert.IsTrue(_sut.DeploymentStatus.Contains("An error occurred:"));
            Assert.IsTrue(_sut.DeploymentStatus.Contains(DeploymentDialogViewModel.appNameEmptyMsg));
        }

        [TestMethod]
        public async Task DeployApp_UpdatesDeploymentStatus_WhenTargetCfEmpty()
        {
            var receivedEvents = new List<string>();
            _sut.PropertyChanged += delegate (object sender, PropertyChangedEventArgs e)
            {
                receivedEvents.Add(e.PropertyName);
            };

            _sut.AppName = "fake app name";
            _sut.SelectedCf = null;
            _sut.SelectedOrg = _fakeOrg;
            _sut.SelectedSpace = _fakeSpace;

            await _sut.DeployApp(null);

            Assert.IsTrue(receivedEvents.Contains("DeploymentStatus"));
            Assert.IsTrue(_sut.DeploymentStatus.Contains("An error occurred:"));
            Assert.IsTrue(_sut.DeploymentStatus.Contains(DeploymentDialogViewModel.targetEmptyMsg));
        }

        [TestMethod]
        public async Task DeployApp_UpdatesDeploymentStatus_WhenTargetOrgEmpty()
        {
            var receivedEvents = new List<string>();
            _sut.PropertyChanged += delegate (object sender, PropertyChangedEventArgs e)
            {
                receivedEvents.Add(e.PropertyName);
            };

            _sut.AppName = "fake app name";
            _sut.SelectedCf = _fakeCfInstance;
            _sut.SelectedOrg = null;
            _sut.SelectedSpace = _fakeSpace;

            await _sut.DeployApp(null);

            Assert.IsTrue(receivedEvents.Contains("DeploymentStatus"));
            Assert.IsTrue(_sut.DeploymentStatus.Contains("An error occurred:"));
            Assert.IsTrue(_sut.DeploymentStatus.Contains(DeploymentDialogViewModel.orgEmptyMsg));
        }

        [TestMethod]
        public async Task DeployApp_UpdatesDeploymentStatus_WhenTargetSpaceEmpty()
        {
            var receivedEvents = new List<string>();
            _sut.PropertyChanged += delegate (object sender, PropertyChangedEventArgs e)
            {
                receivedEvents.Add(e.PropertyName);
            };

            _sut.AppName = "fake app name";
            _sut.SelectedCf = _fakeCfInstance;
            _sut.SelectedOrg = _fakeOrg;
            _sut.SelectedSpace = null;

            await _sut.DeployApp(null);

            Assert.IsTrue(receivedEvents.Contains("DeploymentStatus"));
            Assert.IsTrue(_sut.DeploymentStatus.Contains("An error occurred:"));
            Assert.IsTrue(_sut.DeploymentStatus.Contains(DeploymentDialogViewModel.spaceEmptyMsg));
        }

        [TestMethod]
        public async Task DeployApp_UpdatesDeploymentStatus_WithStdOutput()
        {
            string _fakeStdOutput = "Let's pretend this line was printed to StdOut";
            bool DeploymentStatusPropertyChangedCalled = false;

            _sut.PropertyChanged += (s, args) =>
            {
                if ("DeploymentStatus" == args.PropertyName) DeploymentStatusPropertyChangedCalled = true;
            };

            //* mock cf service to:
            //*     (A) invoke the delegate method it was passed (via Moq Callback)
            //*     (B) return a "success" DetailedResult so DeployApp can proceed
            mockCloudFoundryService.Setup(mock => mock.DeployAppAsync(_fakeCfInstance,
                _fakeOrg, _fakeSpace, _fakeAppName, _fakeProjPath, It.IsAny<StdOutDelegate>()))
                .Callback<CloudFoundryInstance, CloudFoundryOrganization, CloudFoundrySpace, string, string, StdOutDelegate>(
                    (cf, org, space, appname, path, del) => del.Invoke(_fakeStdOutput))
                .ReturnsAsync(new DetailedResult(true));

            _sut.AppName = _fakeAppName;
            _sut.SelectedCf = _fakeCfInstance;
            _sut.SelectedOrg = _fakeOrg;
            _sut.SelectedSpace = _fakeSpace;

            await _sut.DeployApp(null);

            Assert.IsTrue(DeploymentStatusPropertyChangedCalled);
            Assert.IsTrue(_sut.DeploymentStatus.Contains(_fakeStdOutput));
            mockCloudFoundryService.VerifyAll();
        }
        [TestMethod]
        public async Task DeployApp_UpdatesDeploymentStatus_WithStdError()
        {
            string _fakeStdError = "Let's pretend this line was printed to StdErr";
            bool DeploymentStatusPropertyChangedCalled = false;

            _sut.PropertyChanged += (s, args) =>
            {
                if ("DeploymentStatus" == args.PropertyName) DeploymentStatusPropertyChangedCalled = true;
            };

            //* mock cf service to:
            //*     (A) invoke the delegate method it was passed (via Moq Callback)
            //*     (B) return a "failure" DetailedResult 
            mockCloudFoundryService.Setup(mock => mock.DeployAppAsync(_fakeCfInstance,
                _fakeOrg, _fakeSpace, _fakeAppName, _fakeProjPath, It.IsAny<StdOutDelegate>()))
                .Callback<CloudFoundryInstance, CloudFoundryOrganization, CloudFoundrySpace, string, string, StdOutDelegate>(
                    (cf, org, space, appname, path, del) => del.Invoke(_fakeStdError))
                .ReturnsAsync(new DetailedResult(false));

            _sut.AppName = _fakeAppName;
            _sut.SelectedCf = _fakeCfInstance;
            _sut.SelectedOrg = _fakeOrg;
            _sut.SelectedSpace = _fakeSpace;

            await _sut.DeployApp(null);

            Assert.IsTrue(DeploymentStatusPropertyChangedCalled);
            Assert.IsTrue(_sut.DeploymentStatus.Contains(_fakeStdError));
            mockCloudFoundryService.VerifyAll();
        }

    }

    class FakeException : Exception
    {
        private readonly string message;
        private readonly string stackTrace;

        public FakeException(string message = "", string stackTrace = "")
        {
            this.message = message;
            this.stackTrace = stackTrace;
        }

        public override string Message
        {
            get
            {
                return message;
            }
        }

        public override string StackTrace
        {
            get
            {
                return stackTrace;
            }
        }
    }
}