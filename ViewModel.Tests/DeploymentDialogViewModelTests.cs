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
        private const string fakeTargetFrameworkMoniker = "junk";
        private DeploymentDialogViewModel sut;
        private List<string> _receivedEvents;
        private bool defaultFullFWFlag = false;

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

            sut = new DeploymentDialogViewModel(services, _fakeProjPath, fakeTargetFrameworkMoniker);

            _receivedEvents = new List<string>();
            sut.PropertyChanged += delegate (object sender, PropertyChangedEventArgs e)
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
            var vm = new DeploymentDialogViewModel(services, _fakeProjPath, fakeTargetFrameworkMoniker);

            Assert.IsNotNull(vm.CfInstanceOptions);
            Assert.AreEqual(0, vm.CfInstanceOptions.Count);

            mockCloudFoundryService.VerifyGet(mock => mock.CloudFoundryInstances);
        }

        [TestMethod()]
        public void DeploymentDialogViewModel_SetsCfOrgOptionsToEmptyList_WhenConstructed()
        {
            var vm = new DeploymentDialogViewModel(services, _fakeProjPath, fakeTargetFrameworkMoniker);

            Assert.IsNotNull(vm.CfOrgOptions);
            Assert.AreEqual(0, vm.CfOrgOptions.Count);
        }

        [TestMethod()]
        public void DeploymentDialogViewModel_SetsCfSpaceOptionsToEmptyList_WhenConstructed()
        {
            var vm = new DeploymentDialogViewModel(services, _fakeProjPath, fakeTargetFrameworkMoniker);

            Assert.IsNotNull(vm.CfSpaceOptions);
            Assert.AreEqual(0, vm.CfSpaceOptions.Count);
        }


        [TestMethod]
        public void DeployApp_UpdatesDeploymentStatus_WhenAppNameEmpty()
        {
            var receivedEvents = new List<string>();
            sut.PropertyChanged += delegate (object sender, PropertyChangedEventArgs e)
            {
                receivedEvents.Add(e.PropertyName);
            };

            sut.AppName = string.Empty;
            sut.SelectedCf = _fakeCfInstance;
            sut.SelectedOrg = _fakeOrg;
            sut.SelectedSpace = _fakeSpace;

            sut.DeployApp(null);

            Assert.IsTrue(receivedEvents.Contains("DeploymentStatus"));
            Assert.IsTrue(sut.DeploymentStatus.Contains("An error occurred:"));
            Assert.IsTrue(sut.DeploymentStatus.Contains(DeploymentDialogViewModel.appNameEmptyMsg));
        }

        [TestMethod]
        public void DeployApp_UpdatesDeploymentStatus_WhenTargetCfEmpty()
        {
            var receivedEvents = new List<string>();
            sut.PropertyChanged += delegate (object sender, PropertyChangedEventArgs e)
            {
                receivedEvents.Add(e.PropertyName);
            };

            sut.AppName = "fake app name";
            sut.SelectedCf = null;
            sut.SelectedOrg = _fakeOrg;
            sut.SelectedSpace = _fakeSpace;

            sut.DeployApp(null);

            Assert.IsTrue(receivedEvents.Contains("DeploymentStatus"));
            Assert.IsTrue(sut.DeploymentStatus.Contains("An error occurred:"));
            Assert.IsTrue(sut.DeploymentStatus.Contains(DeploymentDialogViewModel.targetEmptyMsg));
        }

        [TestMethod]
        public void DeployApp_UpdatesDeploymentStatus_WhenTargetOrgEmpty()
        {
            var receivedEvents = new List<string>();
            sut.PropertyChanged += delegate (object sender, PropertyChangedEventArgs e)
            {
                receivedEvents.Add(e.PropertyName);
            };

            sut.AppName = "fake app name";
            sut.SelectedCf = _fakeCfInstance;
            sut.SelectedOrg = null;
            sut.SelectedSpace = _fakeSpace;

            sut.DeployApp(null);

            Assert.IsTrue(receivedEvents.Contains("DeploymentStatus"));
            Assert.IsTrue(sut.DeploymentStatus.Contains("An error occurred:"));
            Assert.IsTrue(sut.DeploymentStatus.Contains(DeploymentDialogViewModel.orgEmptyMsg));
        }

        [TestMethod]
        public void DeployApp_UpdatesDeploymentStatus_WhenTargetSpaceEmpty()
        {
            var receivedEvents = new List<string>();
            sut.PropertyChanged += delegate (object sender, PropertyChangedEventArgs e)
            {
                receivedEvents.Add(e.PropertyName);
            };

            sut.AppName = "fake app name";
            sut.SelectedCf = _fakeCfInstance;
            sut.SelectedOrg = _fakeOrg;
            sut.SelectedSpace = null;

            sut.DeployApp(null);

            Assert.IsTrue(receivedEvents.Contains("DeploymentStatus"));
            Assert.IsTrue(sut.DeploymentStatus.Contains("An error occurred:"));
            Assert.IsTrue(sut.DeploymentStatus.Contains(DeploymentDialogViewModel.spaceEmptyMsg));
        }

        [TestMethod]
        public void DeployApp_ClosesDeploymentDialog()
        {
            var dw = new object();

            sut.AppName = _fakeAppName;
            sut.SelectedCf = _fakeCfInstance;
            sut.SelectedOrg = _fakeOrg;
            sut.SelectedSpace = _fakeSpace;

            sut.DeployApp(dw);
            mockDialogService.Verify(mock => mock.CloseDialog(dw, true), Times.Once);
        }

        [TestMethod()]
        public async Task DeploymentDialogViewModel_IndicatesFullFWDeployment_WhenTFMStartsWith_NETFramework()
        {
            string targetFrameworkMoniker = ".NETFramework";
            sut = new DeploymentDialogViewModel(services, _fakeProjPath, targetFrameworkMoniker)
            {
                AppName = _fakeAppName,
                SelectedCf = _fakeCfInstance,
                SelectedOrg = _fakeOrg,
                SelectedSpace = _fakeSpace
            };

            bool expectedFullFWFlag = true;

            mockCloudFoundryService.Setup(m => m.
                DeployAppAsync(_fakeCfInstance, _fakeOrg, _fakeSpace, _fakeAppName, _fakeProjPath,
                    expectedFullFWFlag,
                    It.IsAny<StdOutDelegate>(),
                    It.IsAny<StdErrDelegate>()))
                .ReturnsAsync(fakeSuccessDetailedResult);
            
            await sut.StartDeployment();

            mockCloudFoundryService.VerifyAll();
        }

        [TestMethod]
        public async Task StartDeploymentTask_UpdatesDeploymentInProgress_WhenComplete()
        {
            var receivedEvents = new List<string>();
            sut.PropertyChanged += delegate (object sender, PropertyChangedEventArgs e)
            {
                receivedEvents.Add(e.PropertyName);
            };

            mockCloudFoundryService.Setup(mock =>
                mock.DeployAppAsync(_fakeCfInstance, _fakeOrg, _fakeSpace, _fakeAppName, _fakeProjPath, defaultFullFWFlag,
                                    It.IsAny<StdOutDelegate>(),
                                    It.IsAny<StdErrDelegate>()))
                    .ReturnsAsync(fakeSuccessDetailedResult);

            sut.AppName = _fakeAppName;
            sut.SelectedCf = _fakeCfInstance;
            sut.SelectedOrg = _fakeOrg;
            sut.SelectedSpace = _fakeSpace;

            sut.DeploymentInProgress = true;

            await sut.StartDeployment();

            Assert.IsFalse(sut.DeploymentInProgress);
        }

        [TestMethod]
        public async Task StartDeploymentTask_PassesOutputViewModelAppendLineMethod_AsCallbacks()
        {
            sut.AppName = _fakeAppName;
            sut.SelectedCf = _fakeCfInstance;
            sut.SelectedOrg = _fakeOrg;
            sut.SelectedSpace = _fakeSpace;

            StdOutDelegate expectedStdOutCallback = sut.outputViewModel.AppendLine;
            StdErrDelegate expectedStdErrCallback = sut.outputViewModel.AppendLine;

            mockCloudFoundryService.Setup(mock => mock.
              DeployAppAsync(_fakeCfInstance, _fakeOrg, _fakeSpace, _fakeAppName, _fakeProjPath, defaultFullFWFlag,
                             expectedStdOutCallback,
                             expectedStdErrCallback))
                .ReturnsAsync(fakeSuccessDetailedResult);

            await sut.StartDeployment();
        }

        [TestMethod]
        public async Task StartDeploymentTask_LogsError_WhenDeployResultReportsFailure()
        {
            sut.AppName = _fakeAppName;
            sut.SelectedCf = _fakeCfInstance;
            sut.SelectedOrg = _fakeOrg;
            sut.SelectedSpace = _fakeSpace;

            StdOutDelegate expectedStdOutCallback = sut.outputViewModel.AppendLine;
            StdErrDelegate expectedStdErrCallback = sut.outputViewModel.AppendLine;

            mockCloudFoundryService.Setup(mock => mock.
                DeployAppAsync(_fakeCfInstance, _fakeOrg, _fakeSpace, _fakeAppName, _fakeProjPath, defaultFullFWFlag,
                             expectedStdOutCallback,
                             expectedStdErrCallback))
                    .ReturnsAsync(fakeFailureDetailedResult);

            var expectedErrorTitle = $"{DeploymentDialogViewModel.deploymentErrorMsg} {_fakeAppName}.";
            var expectedErrorMsg = $"{fakeFailureDetailedResult.Explanation}";

            mockDialogService.Setup(mock => mock.
                DisplayErrorDialog(expectedErrorTitle, expectedErrorMsg));

            await sut.StartDeployment();

            var logPropVal1 = "{AppName}";
            var logPropVal2 = "{TargetApi}";
            var logPropVal3 = "{TargetOrg}";
            var logPropVal4 = "{TargetSpace}";
            var logPropVal5 = "{DplmtResult}";
            var expectedLogMsg = $"DeploymentDialogViewModel initiated app deployment of {logPropVal1} to target {logPropVal2}.{logPropVal3}.{logPropVal4}; deployment result reported failure: {logPropVal5}.";

            mockLogger.Verify(m => m.
                Error(expectedLogMsg, _fakeAppName, _fakeCfInstance.ApiAddress, _fakeOrg.OrgName, _fakeSpace.SpaceName, fakeFailureDetailedResult.ToString()),
                    Times.Once);
        }
        
        [TestMethod]
        public async Task StartDeploymentTask_DisplaysErrorDialog_WhenDeployResultReportsFailure()
        {
            sut.AppName = _fakeAppName;
            sut.SelectedCf = _fakeCfInstance;
            sut.SelectedOrg = _fakeOrg;
            sut.SelectedSpace = _fakeSpace;

            StdOutDelegate expectedStdOutCallback = sut.outputViewModel.AppendLine;
            StdErrDelegate expectedStdErrCallback = sut.outputViewModel.AppendLine;

            mockCloudFoundryService.Setup(mock => mock.
                DeployAppAsync(_fakeCfInstance, _fakeOrg, _fakeSpace, _fakeAppName, _fakeProjPath, defaultFullFWFlag,
                             expectedStdOutCallback,
                             expectedStdErrCallback))
                    .ReturnsAsync(fakeFailureDetailedResult);

            var expectedErrorTitle = $"{DeploymentDialogViewModel.deploymentErrorMsg} {_fakeAppName}.";
            var expectedErrorMsg = $"{fakeFailureDetailedResult.Explanation}";

            mockDialogService.Setup(mock => mock.
                DisplayErrorDialog(expectedErrorTitle, expectedErrorMsg));

            await sut.StartDeployment();
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

            sut.SelectedCf = fakeCfInstance;

            Assert.AreEqual(0, sut.CfOrgOptions.Count);

            await sut.UpdateCfOrgOptions();

            Assert.AreEqual(1, sut.CfOrgOptions.Count);
            Assert.AreEqual(fakeCfOrg, sut.CfOrgOptions[0]);

            Assert.IsTrue(_receivedEvents.Contains("CfOrgOptions"));
        }

        [TestMethod]
        public async Task UpdateCfOrgOptions_SetsCfOrgOptionsToEmptyList_WhenSelectedCfIsNull()
        {
            sut.SelectedCf = null;

            await sut.UpdateCfOrgOptions();

            Assert.AreEqual(0, sut.CfOrgOptions.Count);

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

            sut.SelectedCf = fakeCfInstance;
            var initialOrgOptions = sut.CfOrgOptions;

            await sut.UpdateCfOrgOptions();

            mockDialogService.VerifyAll();
            Assert.AreEqual(initialOrgOptions, sut.CfOrgOptions);
        }
        
        [TestMethod]
        public async Task UpdateCfOrgOptions_LogsError_WhenOrgsResponseReportsFailure()
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

            sut.SelectedCf = fakeCfInstance;
            var initialOrgOptions = sut.CfOrgOptions;

            await sut.UpdateCfOrgOptions();

            mockLogger.Verify(m => m.
                Error($"{DeploymentDialogViewModel.getOrgsFailureMsg}. {fakeFailedOrgsResponse}"),
                    Times.Once);
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

            sut.SelectedCf = fakeCfInstance;
            sut.SelectedOrg = fakeCfOrg;

            Assert.AreEqual(0, sut.CfSpaceOptions.Count);

            await sut.UpdateCfSpaceOptions();

            Assert.AreEqual(1, sut.CfSpaceOptions.Count);
            Assert.AreEqual(fakeCfSpace, sut.CfSpaceOptions[0]);

            Assert.IsTrue(_receivedEvents.Contains("CfSpaceOptions"));
        }

        [TestMethod]
        public async Task UpdateCfSpaceOptions_SetsCfSpaceOptionsToEmptyList_WhenSelectedCfIsNull()
        {
            sut.SelectedCf = null;

            await sut.UpdateCfSpaceOptions();

            Assert.AreEqual(0, sut.CfSpaceOptions.Count);

            Assert.IsTrue(_receivedEvents.Contains("CfSpaceOptions"));
        }

        [TestMethod]
        public async Task UpdateCfSpaceOptions_SetsCfSpaceOptionsToEmptyList_WhenSelectedOrgIsNull()
        {
            sut.SelectedOrg = null;

            await sut.UpdateCfSpaceOptions();

            Assert.AreEqual(0, sut.CfSpaceOptions.Count);

            Assert.IsTrue(_receivedEvents.Contains("CfSpaceOptions"));
        }

        [TestMethod]
        public async Task UpdateCfSpaceOptions_LogsError_WhenSpacesResponseReportsFailure()
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

            sut.SelectedCf = fakeCfInstance;
            sut.SelectedOrg = fakeCfOrg;
            var initialSpaceOptions = sut.CfSpaceOptions;

            await sut.UpdateCfSpaceOptions();

            mockLogger.Verify(m => m.
                Error($"{DeploymentDialogViewModel.getSpacesFailureMsg}. {fakeFailedSpacesResponse}"),
                    Times.Once);
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

            sut.SelectedCf = fakeCfInstance;
            sut.SelectedOrg = fakeCfOrg;
            var initialSpaceOptions = sut.CfSpaceOptions;

            await sut.UpdateCfSpaceOptions();

            mockDialogService.VerifyAll();
            Assert.AreEqual(initialSpaceOptions, sut.CfSpaceOptions);
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