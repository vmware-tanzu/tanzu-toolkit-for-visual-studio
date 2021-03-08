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
        private List<string> _receivedEvents;

        [TestInitialize]
        public void TestInit()
        {
            //* return empty dictionary of CloudFoundryInstances
            mockCloudFoundryService.SetupGet(mock =>
                mock.CloudFoundryInstances)
                    .Returns(new Dictionary<string, CloudFoundryInstance>());

            //* return fake view/viewmodel for output window
            mockViewLocatorService.Setup(mock =>
                mock.NavigateTo(nameof(OutputViewModel), null))
                    .Returns(new FakeOutputView());

            _sut = new DeploymentDialogViewModel(services, _fakeProjPath);

            _receivedEvents = new List<string>();
            _sut.PropertyChanged += delegate (object sender, PropertyChangedEventArgs e)
            {
                _receivedEvents.Add(e.PropertyName);
            };
        }

        [TestCleanup]
        public void TestCleanup()
        {
            mockCloudFoundryService.VerifyAll();
            mockViewLocatorService.VerifyAll();
            mockDialogService.VerifyAll();
        }

        [TestMethod()]
        public void DeploymentDialogViewModel_GetsListOfCfsFromCfService_WhenConstructed()
        {
            var vm = new DeploymentDialogViewModel(services, _fakeProjPath);

            Assert.IsNotNull(vm.CfInstanceOptions);
            Assert.AreEqual(0, vm.CfInstanceOptions.Count);

            mockCloudFoundryService.VerifyGet(mock => mock.CloudFoundryInstances);
        }
        
        [TestMethod()]
        public void DeploymentDialogViewModel_SetsCfOrgOptionsToEmptyList_WhenConstructed()
        {
            var vm = new DeploymentDialogViewModel(services, _fakeProjPath);

            Assert.IsNotNull(vm.CfOrgOptions);
            Assert.AreEqual(0, vm.CfOrgOptions.Count);
        }
        
        [TestMethod()]
        public void DeploymentDialogViewModel_SetsCfSpaceOptionsToEmptyList_WhenConstructed()
        {
            var vm = new DeploymentDialogViewModel(services, _fakeProjPath);

            Assert.IsNotNull(vm.CfSpaceOptions);
            Assert.AreEqual(0, vm.CfSpaceOptions.Count);
        }

        [TestMethod]
        public void DeployApp_UpdatesDeploymentStatus_WhenAppNameEmpty()
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

            _sut.DeployApp(null);

            Assert.IsTrue(receivedEvents.Contains("DeploymentStatus"));
            Assert.IsTrue(_sut.DeploymentStatus.Contains("An error occurred:"));
            Assert.IsTrue(_sut.DeploymentStatus.Contains(DeploymentDialogViewModel.appNameEmptyMsg));
        }

        [TestMethod]
        public void DeployApp_UpdatesDeploymentStatus_WhenTargetCfEmpty()
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

            _sut.DeployApp(null);

            Assert.IsTrue(receivedEvents.Contains("DeploymentStatus"));
            Assert.IsTrue(_sut.DeploymentStatus.Contains("An error occurred:"));
            Assert.IsTrue(_sut.DeploymentStatus.Contains(DeploymentDialogViewModel.targetEmptyMsg));
        }

        [TestMethod]
        public void DeployApp_UpdatesDeploymentStatus_WhenTargetOrgEmpty()
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

            _sut.DeployApp(null);

            Assert.IsTrue(receivedEvents.Contains("DeploymentStatus"));
            Assert.IsTrue(_sut.DeploymentStatus.Contains("An error occurred:"));
            Assert.IsTrue(_sut.DeploymentStatus.Contains(DeploymentDialogViewModel.orgEmptyMsg));
        }

        [TestMethod]
        public void DeployApp_UpdatesDeploymentStatus_WhenTargetSpaceEmpty()
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

            _sut.DeployApp(null);

            Assert.IsTrue(receivedEvents.Contains("DeploymentStatus"));
            Assert.IsTrue(_sut.DeploymentStatus.Contains("An error occurred:"));
            Assert.IsTrue(_sut.DeploymentStatus.Contains(DeploymentDialogViewModel.spaceEmptyMsg));
        }

        [TestMethod]
        public void DeployApp_ClosesDeploymentDialog()
        {
            var dw = new object();

            _sut.AppName = _fakeAppName;
            _sut.SelectedCf = _fakeCfInstance;
            _sut.SelectedOrg = _fakeOrg;
            _sut.SelectedSpace = _fakeSpace;

            _sut.DeployApp(dw);
            mockDialogService.Verify(mock => mock.CloseDialog(dw, true), Times.Once);
        }

        [TestMethod]
        public async Task StartDeploymentTask_UpdatesDeploymentInProgress_WhenComplete()
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
                mock.DeployAppAsync(_fakeCfInstance, _fakeOrg, _fakeSpace, _fakeAppName, _fakeProjPath, It.IsAny<StdOutDelegate>(), It.IsAny<StdErrDelegate>()))
                    .ThrowsAsync(fakeException);

            _sut.AppName = _fakeAppName;
            _sut.SelectedCf = _fakeCfInstance;
            _sut.SelectedOrg = _fakeOrg;
            _sut.SelectedSpace = _fakeSpace;

            Exception shouldStayNull = null;
            try
            {
                _sut.DeploymentInProgress = true;
                await _sut.StartDeployment();
            }
            catch (Exception ex)
            {
                shouldStayNull = ex;
            }

            Assert.IsNull(shouldStayNull);
            Assert.IsFalse(_sut.DeploymentInProgress);

            mockCloudFoundryService.VerifyAll(); // ensure DeployAppAsync was called with proper params
            mockViewLocatorService.VerifyAll(); // ensure we're using a mock output view/viewmodel
        }

        [TestMethod]
        public async Task StartDeploymentTask_PassesOutputViewModelAppendLineMethod_AsCallbacks()
        {
            _sut.AppName = _fakeAppName;
            _sut.SelectedCf = _fakeCfInstance;
            _sut.SelectedOrg = _fakeOrg;
            _sut.SelectedSpace = _fakeSpace;

            StdOutDelegate expectedStdOutCallback = _sut.outputViewModel.AppendLine;
            StdErrDelegate expectedStdErrCallback = _sut.outputViewModel.AppendLine;

            await _sut.StartDeployment();

            mockCloudFoundryService.Verify(mock => mock.
              DeployAppAsync(_fakeCfInstance, _fakeOrg, _fakeSpace, _fakeAppName, _fakeProjPath,
                             expectedStdOutCallback,
                             expectedStdErrCallback),
                Times.Once);
        }

        [TestMethod]
        public async Task UpdateCfOrgOptions_RaisesPropertyChangedEvent_WhenOrgsRequestSucceeds()
        {
            var fakeOrgsList = new List<CloudFoundryOrganization> { fakeCfOrg };

            var fakeSuccessfulOrgsResponse = new DetailedResult<List<CloudFoundryOrganization>>(
                content: fakeOrgsList,
                succeeded: true,
                explanation: null,
                cmdDetails: fakeSuccessCmdResult);

            mockCloudFoundryService.Setup(mock => mock.
                GetOrgsForCfInstanceAsync(fakeCfInstance, true))
                    .ReturnsAsync(fakeSuccessfulOrgsResponse);

            _sut.SelectedCf = fakeCfInstance;

            Assert.AreEqual(0, _sut.CfOrgOptions.Count);

            await _sut.UpdateCfOrgOptions();

            Assert.AreEqual(1, _sut.CfOrgOptions.Count);
            Assert.AreEqual(fakeCfOrg, _sut.CfOrgOptions[0]);

            Assert.IsTrue(_receivedEvents.Contains("CfOrgOptions"));
        }
    
        [TestMethod]
        public async Task UpdateCfOrgOptions_SetsCfOrgOptionsToEmptyList_WhenSelectedCfIsNull()
        {
            _sut.SelectedCf = null;

            await _sut.UpdateCfOrgOptions();

            Assert.AreEqual(0, _sut.CfOrgOptions.Count);

            Assert.IsTrue(_receivedEvents.Contains("CfOrgOptions"));
        }

        [TestMethod]
        public async Task UpdateCfOrgOptions_DisplaysErrorDialog_WhenOrgsResponseReportsFailure()
        {
            var fakeExplanation = "junk";

            var fakeFailedOrgsResponse = new DetailedResult<List<CloudFoundryOrganization>>(
                content: null,
                succeeded: false,
                explanation: fakeExplanation,
                cmdDetails: fakeFailureCmdResult);

            mockCloudFoundryService.Setup(mock => mock.
                GetOrgsForCfInstanceAsync(fakeCfInstance, true))
                    .ReturnsAsync(fakeFailedOrgsResponse);

            mockDialogService.Setup(mock => mock.
                DisplayErrorDialog(DeploymentDialogViewModel.getOrgsFailureMsg, fakeExplanation));

            _sut.SelectedCf = fakeCfInstance;
            var initialOrgOptions = _sut.CfOrgOptions;

            await _sut.UpdateCfOrgOptions();

            mockDialogService.VerifyAll();
            Assert.AreEqual(initialOrgOptions, _sut.CfOrgOptions);
        }
    
        
        [TestMethod]
        public async Task UpdateCfSpaceOptions_RaisesPropertyChangedEvent_WhenSpacesRequestSucceeds()
        {
            var fakeSpacesList = new List<CloudFoundrySpace> { fakeCfSpace };

            var fakeSuccessfulSpacesResponse = new DetailedResult<List<CloudFoundrySpace>>(
                content: fakeSpacesList,
                succeeded: true,
                explanation: null,
                cmdDetails: fakeSuccessCmdResult);

            mockCloudFoundryService.Setup(mock => mock.
                GetSpacesForOrgAsync(fakeCfOrg, true))
                    .ReturnsAsync(fakeSuccessfulSpacesResponse);

            _sut.SelectedCf = fakeCfInstance;
            _sut.SelectedOrg = fakeCfOrg;

            Assert.AreEqual(0, _sut.CfSpaceOptions.Count);

            await _sut.UpdateCfSpaceOptions();

            Assert.AreEqual(1, _sut.CfSpaceOptions.Count);
            Assert.AreEqual(fakeCfSpace, _sut.CfSpaceOptions[0]);

            Assert.IsTrue(_receivedEvents.Contains("CfSpaceOptions"));
        }
    
        [TestMethod]
        public async Task UpdateCfSpaceOptions_SetsCfSpaceOptionsToEmptyList_WhenSelectedCfIsNull()
        {
            _sut.SelectedCf = null;

            await _sut.UpdateCfSpaceOptions();

            Assert.AreEqual(0, _sut.CfSpaceOptions.Count);

            Assert.IsTrue(_receivedEvents.Contains("CfSpaceOptions"));
        }
        
        [TestMethod]
        public async Task UpdateCfSpaceOptions_SetsCfSpaceOptionsToEmptyList_WhenSelectedOrgIsNull()
        {
            _sut.SelectedOrg = null;

            await _sut.UpdateCfSpaceOptions();

            Assert.AreEqual(0, _sut.CfSpaceOptions.Count);

            Assert.IsTrue(_receivedEvents.Contains("CfSpaceOptions"));
        }

        [TestMethod]
        public async Task UpdateCfSpaceOptions_DisplaysErrorDialog_WhenSpacesResponseReportsFailure()
        {
            var fakeExplanation = "junk";

            var fakeFailedSpacesResponse = new DetailedResult<List<CloudFoundrySpace>>(
                content: null,
                succeeded: false,
                explanation: fakeExplanation,
                cmdDetails: fakeFailureCmdResult);

            mockCloudFoundryService.Setup(mock => mock.
                GetSpacesForOrgAsync(fakeCfOrg, true))
                    .ReturnsAsync(fakeFailedSpacesResponse);

            mockDialogService.Setup(mock => mock.
                DisplayErrorDialog(DeploymentDialogViewModel.getSpacesFailureMsg, fakeExplanation));

            _sut.SelectedCf = fakeCfInstance;
            _sut.SelectedOrg = fakeCfOrg;
            var initialSpaceOptions = _sut.CfSpaceOptions;

            await _sut.UpdateCfSpaceOptions();

            mockDialogService.VerifyAll();
            Assert.AreEqual(initialSpaceOptions, _sut.CfSpaceOptions);
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

    class FakeOutputView : ViewModelTestSupport, IView
    {
        public IViewModel ViewModel { get; }

        public FakeOutputView()
        {
            ViewModel = new OutputViewModel(services);
        }
    }
}