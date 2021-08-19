using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Tanzu.Toolkit.Models;
using Tanzu.Toolkit.Services;
using static Tanzu.Toolkit.Services.OutputHandler.OutputHandler;

namespace Tanzu.Toolkit.ViewModels.Tests
{
    [TestClass]
    public class DeploymentDialogViewModelTests : ViewModelTestSupport
    {
        private const string _fakeAppName = "fake app name";
        private const string _fakeProjPath = "this\\is\\a\\fake\\path\\to\\a\\project\\directory";
        private const string FakeTargetFrameworkMoniker = "junk";

        private static readonly CloudFoundryInstance _fakeCfInstance = new CloudFoundryInstance("", "");
        private static readonly CloudFoundryOrganization _fakeOrg = new CloudFoundryOrganization("", "", _fakeCfInstance);
        private readonly CloudFoundrySpace _fakeSpace = new CloudFoundrySpace("", "", _fakeOrg);
        private DeploymentDialogViewModel _sut;
        private List<string> _receivedEvents;
        private readonly bool _defaultFullFWFlag = false;

        [TestInitialize]
        public void TestInit()
        {
            RenewMockServices();

            // * return empty dictionary of CloudFoundryInstances
            MockCloudFoundryService.SetupGet(mock =>
                mock.CloudFoundryInstances)
                    .Returns(new Dictionary<string, CloudFoundryInstance>());

            // * return fake view/viewmodel for output window
            MockViewLocatorService.Setup(mock =>
                mock.NavigateTo(nameof(OutputViewModel), null))
                    .Returns(new FakeOutputView());

            // * return fake view/viewmodel for tasExplorer window
            MockViewLocatorService.Setup(mock =>
                mock.NavigateTo(nameof(TasExplorerViewModel), null))
                    .Returns(new FakeTasExplorerView());

            _sut = new DeploymentDialogViewModel(Services, _fakeProjPath, FakeTargetFrameworkMoniker);

            _receivedEvents = new List<string>();
            _sut.PropertyChanged += (sender, e) =>
            {
                _receivedEvents.Add(e.PropertyName);
            };
        }

        [TestCleanup]
        public void TestCleanup()
        {
            MockCloudFoundryService.VerifyAll();
            MockViewLocatorService.VerifyAll();
            MockDialogService.VerifyAll();
        }

        [TestMethod]
        public void DeploymentDialogViewModel_GetsListOfCfsFromCfService_WhenConstructed()
        {
            var vm = new DeploymentDialogViewModel(Services, _fakeProjPath, FakeTargetFrameworkMoniker);

            Assert.IsNotNull(vm.CfInstanceOptions);
            Assert.AreEqual(0, vm.CfInstanceOptions.Count);

            MockCloudFoundryService.VerifyGet(mock => mock.CloudFoundryInstances);
        }

        [TestMethod]
        public void DeploymentDialogViewModel_SetsCfOrgOptionsToEmptyList_WhenConstructed()
        {
            var vm = new DeploymentDialogViewModel(Services, _fakeProjPath, FakeTargetFrameworkMoniker);

            Assert.IsNotNull(vm.CfOrgOptions);
            Assert.AreEqual(0, vm.CfOrgOptions.Count);
        }

        [TestMethod]
        public void DeploymentDialogViewModel_SetsCfSpaceOptionsToEmptyList_WhenConstructed()
        {
            var vm = new DeploymentDialogViewModel(Services, _fakeProjPath, FakeTargetFrameworkMoniker);

            Assert.IsNotNull(vm.CfSpaceOptions);
            Assert.AreEqual(0, vm.CfSpaceOptions.Count);
        }

        [TestMethod]
        public void DeployApp_UpdatesDeploymentStatus_WhenAppNameEmpty()
        {
            var receivedEvents = new List<string>();
            _sut.PropertyChanged += (sender, e) =>
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
            Assert.IsTrue(_sut.DeploymentStatus.Contains(DeploymentDialogViewModel.AppNameEmptyMsg));
        }

        [TestMethod]
        public void DeployApp_UpdatesDeploymentStatus_WhenTargetCfEmpty()
        {
            var receivedEvents = new List<string>();
            _sut.PropertyChanged += (sender, e) =>
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
            Assert.IsTrue(_sut.DeploymentStatus.Contains(DeploymentDialogViewModel.TargetEmptyMsg));
        }

        [TestMethod]
        public void DeployApp_UpdatesDeploymentStatus_WhenTargetOrgEmpty()
        {
            var receivedEvents = new List<string>();
            _sut.PropertyChanged += (sender, e) =>
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
            Assert.IsTrue(_sut.DeploymentStatus.Contains(DeploymentDialogViewModel.OrgEmptyMsg));
        }

        [TestMethod]
        public void DeployApp_UpdatesDeploymentStatus_WhenTargetSpaceEmpty()
        {
            var receivedEvents = new List<string>();
            _sut.PropertyChanged += (sender, e) =>
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
            Assert.IsTrue(_sut.DeploymentStatus.Contains(DeploymentDialogViewModel.SpaceEmptyMsg));
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
            MockDialogService.Verify(mock => mock.CloseDialog(dw, true), Times.Once);
        }

        [TestMethod]
        public async Task DeploymentDialogViewModel_IndicatesFullFWDeployment_WhenTFMStartsWith_NETFramework()
        {
            string targetFrameworkMoniker = ".NETFramework";
            _sut = new DeploymentDialogViewModel(Services, _fakeProjPath, targetFrameworkMoniker)
            {
                AppName = _fakeAppName,
                SelectedCf = _fakeCfInstance,
                SelectedOrg = _fakeOrg,
                SelectedSpace = _fakeSpace,
            };

            bool expectedFullFWFlag = true;

            MockCloudFoundryService.Setup(m => m.
                DeployAppAsync(_fakeCfInstance, _fakeOrg, _fakeSpace, _fakeAppName, _fakeProjPath,
                    expectedFullFWFlag,
                    It.IsAny<StdOutDelegate>(),
                    It.IsAny<StdErrDelegate>(), null))
                .ReturnsAsync(FakeSuccessDetailedResult);

            await _sut.StartDeployment();

            MockCloudFoundryService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("StartDeployment")]
        public async Task StartDeploymentTask_UpdatesDeploymentInProgress_WhenComplete()
        {
            var receivedEvents = new List<string>();
            _sut.PropertyChanged += (sender, e) =>
            {
                receivedEvents.Add(e.PropertyName);
            };

            MockCloudFoundryService.Setup(mock =>
                mock.DeployAppAsync(_fakeCfInstance, _fakeOrg, _fakeSpace, _fakeAppName, _fakeProjPath, _defaultFullFWFlag,
                                    It.IsAny<StdOutDelegate>(),
                                    It.IsAny<StdErrDelegate>(), null))
                    .ReturnsAsync(FakeSuccessDetailedResult);

            _sut.AppName = _fakeAppName;
            _sut.SelectedCf = _fakeCfInstance;
            _sut.SelectedOrg = _fakeOrg;
            _sut.SelectedSpace = _fakeSpace;

            _sut.DeploymentInProgress = true;

            await _sut.StartDeployment();

            Assert.IsFalse(_sut.DeploymentInProgress);
        }

        [TestMethod]
        [TestCategory("StartDeployment")]
        public async Task StartDeploymentTask_PassesOutputViewModelAppendLineMethod_AsCallbacks()
        {
            _sut.AppName = _fakeAppName;
            _sut.SelectedCf = _fakeCfInstance;
            _sut.SelectedOrg = _fakeOrg;
            _sut.SelectedSpace = _fakeSpace;

            StdOutDelegate expectedStdOutCallback = _sut.OutputViewModel.AppendLine;
            StdErrDelegate expectedStdErrCallback = _sut.OutputViewModel.AppendLine;

            MockCloudFoundryService.Setup(mock => mock.
              DeployAppAsync(_fakeCfInstance, _fakeOrg, _fakeSpace, _fakeAppName, _fakeProjPath, _defaultFullFWFlag,
                             expectedStdOutCallback,
                             expectedStdErrCallback, null))
                .ReturnsAsync(FakeSuccessDetailedResult);

            await _sut.StartDeployment();
        }

        [TestMethod]
        [TestCategory("StartDeployment")]
        public async Task StartDeploymentTask_LogsError_WhenDeployResultReportsFailure()
        {
            _sut.AppName = _fakeAppName;
            _sut.SelectedCf = _fakeCfInstance;
            _sut.SelectedOrg = _fakeOrg;
            _sut.SelectedSpace = _fakeSpace;

            StdOutDelegate expectedStdOutCallback = _sut.OutputViewModel.AppendLine;
            StdErrDelegate expectedStdErrCallback = _sut.OutputViewModel.AppendLine;

            MockCloudFoundryService.Setup(mock => mock.
                DeployAppAsync(_fakeCfInstance, _fakeOrg, _fakeSpace, _fakeAppName, _fakeProjPath, _defaultFullFWFlag,
                             expectedStdOutCallback,
                             expectedStdErrCallback, null))
                    .ReturnsAsync(FakeFailureDetailedResult);

            var expectedErrorTitle = $"{DeploymentDialogViewModel.DeploymentErrorMsg} {_fakeAppName}.";
            var expectedErrorMsg = $"{FakeFailureDetailedResult.Explanation}";

            MockErrorDialogService.Setup(mock => mock.
                DisplayErrorDialog(expectedErrorTitle, expectedErrorMsg));

            await _sut.StartDeployment();

            var logPropVal1 = "{AppName}";
            var logPropVal2 = "{TargetApi}";
            var logPropVal3 = "{TargetOrg}";
            var logPropVal4 = "{TargetSpace}";
            var logPropVal5 = "{DplmtResult}";
            var expectedLogMsg = $"DeploymentDialogViewModel initiated app deployment of {logPropVal1} to target {logPropVal2}.{logPropVal3}.{logPropVal4}; deployment result reported failure: {logPropVal5}.";

            MockLogger.Verify(m => m.
                Error(expectedLogMsg, _fakeAppName, _fakeCfInstance.ApiAddress, _fakeOrg.OrgName, _fakeSpace.SpaceName, FakeFailureDetailedResult.ToString()),
                    Times.Once);
        }

        [TestMethod]
        [TestCategory("StartDeployment")]
        public async Task StartDeploymentTask_DisplaysErrorDialog_WhenDeployResultReportsFailure()
        {
            _sut.AppName = _fakeAppName;
            _sut.SelectedCf = _fakeCfInstance;
            _sut.SelectedOrg = _fakeOrg;
            _sut.SelectedSpace = _fakeSpace;

            StdOutDelegate expectedStdOutCallback = _sut.OutputViewModel.AppendLine;
            StdErrDelegate expectedStdErrCallback = _sut.OutputViewModel.AppendLine;

            MockCloudFoundryService.Setup(mock => mock.
                DeployAppAsync(_fakeCfInstance, _fakeOrg, _fakeSpace, _fakeAppName, _fakeProjPath, _defaultFullFWFlag,
                             expectedStdOutCallback,
                             expectedStdErrCallback, null))
                    .ReturnsAsync(FakeFailureDetailedResult);

            var expectedErrorTitle = $"{DeploymentDialogViewModel.DeploymentErrorMsg} {_fakeAppName}.";
            var expectedErrorMsg = $"{FakeFailureDetailedResult.Explanation}";

            MockErrorDialogService.Setup(mock => mock.
                DisplayErrorDialog(expectedErrorTitle, expectedErrorMsg));

            await _sut.StartDeployment();
        }

        [TestMethod]
        [TestCategory("StartDeployment")]
        public async Task StartDeployment_SetsAuthRequiredToTrueOnTasExplorer_WhenFailureTypeIsInvalidRefreshToken()
        {
            _sut.AppName = _fakeAppName;
            _sut.SelectedCf = _fakeCfInstance;
            _sut.SelectedOrg = _fakeOrg;
            _sut.SelectedSpace = _fakeSpace;

            StdOutDelegate expectedStdOutCallback = _sut.OutputViewModel.AppendLine;
            StdErrDelegate expectedStdErrCallback = _sut.OutputViewModel.AppendLine;

            var invalidRefreshTokenFailure = new DetailedResult(false, "junk error", FakeFailureCmdResult)
            {
                FailureType = FailureType.InvalidRefreshToken
            };

            MockCloudFoundryService.Setup(mock => mock.
                DeployAppAsync(_fakeCfInstance, _fakeOrg, _fakeSpace, _fakeAppName, _fakeProjPath, _defaultFullFWFlag,
                             expectedStdOutCallback,
                             expectedStdErrCallback, null))
                    .ReturnsAsync(invalidRefreshTokenFailure);

            Assert.IsFalse(_sut.TasExplorerViewModel.AuthenticationRequired);

            await _sut.StartDeployment();

            Assert.IsTrue(_sut.TasExplorerViewModel.AuthenticationRequired);
        }

        [TestMethod]
        public async Task UpdateCfOrgOptions_RaisesPropertyChangedEvent_WhenOrgsRequestSucceeds()
        {
            var fakeOrgsList = new List<CloudFoundryOrganization> { FakeCfOrg };

            var fakeSuccessfulOrgsResponse = new DetailedResult<List<CloudFoundryOrganization>>(
                content: fakeOrgsList,
                succeeded: true,
                explanation: null,
                cmdDetails: FakeSuccessCmdResult);

            MockCloudFoundryService.Setup(mock => mock.
                GetOrgsForCfInstanceAsync(FakeCfInstance, true, It.IsAny<int>()))
                    .ReturnsAsync(fakeSuccessfulOrgsResponse);

            _sut.SelectedCf = FakeCfInstance;

            Assert.AreEqual(0, _sut.CfOrgOptions.Count);

            await _sut.UpdateCfOrgOptions();

            Assert.AreEqual(1, _sut.CfOrgOptions.Count);
            Assert.AreEqual(FakeCfOrg, _sut.CfOrgOptions[0]);

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
                cmdDetails: FakeFailureCmdResult);

            MockCloudFoundryService.Setup(mock => mock.
                GetOrgsForCfInstanceAsync(FakeCfInstance, true, It.IsAny<int>()))
                    .ReturnsAsync(fakeFailedOrgsResponse);

            MockErrorDialogService.Setup(mock => mock.
                DisplayErrorDialog(DeploymentDialogViewModel.GetOrgsFailureMsg, fakeExplanation));

            _sut.SelectedCf = FakeCfInstance;
            var initialOrgOptions = _sut.CfOrgOptions;

            await _sut.UpdateCfOrgOptions();

            MockDialogService.VerifyAll();
            Assert.AreEqual(initialOrgOptions, _sut.CfOrgOptions);
        }

        [TestMethod]
        public async Task UpdateCfOrgOptions_LogsError_WhenOrgsResponseReportsFailure()
        {
            var fakeExplanation = "junk";

            var fakeFailedOrgsResponse = new DetailedResult<List<CloudFoundryOrganization>>(
                content: null,
                succeeded: false,
                explanation: fakeExplanation,
                cmdDetails: FakeFailureCmdResult);

            MockCloudFoundryService.Setup(mock => mock.
                GetOrgsForCfInstanceAsync(FakeCfInstance, true, It.IsAny<int>()))
                    .ReturnsAsync(fakeFailedOrgsResponse);

            MockErrorDialogService.Setup(mock => mock.
                DisplayErrorDialog(DeploymentDialogViewModel.GetOrgsFailureMsg, fakeExplanation));

            _sut.SelectedCf = FakeCfInstance;
            var initialOrgOptions = _sut.CfOrgOptions;

            await _sut.UpdateCfOrgOptions();

            MockLogger.Verify(m => m.
                Error($"{DeploymentDialogViewModel.GetOrgsFailureMsg}. {fakeFailedOrgsResponse}"),
                    Times.Once);
        }

        [TestMethod]
        public async Task UpdateCfSpaceOptions_RaisesPropertyChangedEvent_WhenSpacesRequestSucceeds()
        {
            var fakeSpacesList = new List<CloudFoundrySpace> { FakeCfSpace };

            var fakeSuccessfulSpacesResponse = new DetailedResult<List<CloudFoundrySpace>>(
                content: fakeSpacesList,
                succeeded: true,
                explanation: null,
                cmdDetails: FakeSuccessCmdResult);

            MockCloudFoundryService.Setup(mock => mock.
                GetSpacesForOrgAsync(FakeCfOrg, true, It.IsAny<int>()))
                    .ReturnsAsync(fakeSuccessfulSpacesResponse);

            _sut.SelectedCf = FakeCfInstance;
            _sut.SelectedOrg = FakeCfOrg;

            Assert.AreEqual(0, _sut.CfSpaceOptions.Count);

            await _sut.UpdateCfSpaceOptions();

            Assert.AreEqual(1, _sut.CfSpaceOptions.Count);
            Assert.AreEqual(FakeCfSpace, _sut.CfSpaceOptions[0]);

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
        public async Task UpdateCfSpaceOptions_LogsError_WhenSpacesResponseReportsFailure()
        {
            var fakeExplanation = "junk";

            var fakeFailedSpacesResponse = new DetailedResult<List<CloudFoundrySpace>>(
                content: null,
                succeeded: false,
                explanation: fakeExplanation,
                cmdDetails: FakeFailureCmdResult);

            MockCloudFoundryService.Setup(mock => mock.
                GetSpacesForOrgAsync(FakeCfOrg, true, It.IsAny<int>()))
                    .ReturnsAsync(fakeFailedSpacesResponse);

            MockErrorDialogService.Setup(mock => mock.
                DisplayErrorDialog(DeploymentDialogViewModel.GetSpacesFailureMsg, fakeExplanation));

            _sut.SelectedCf = FakeCfInstance;
            _sut.SelectedOrg = FakeCfOrg;
            var initialSpaceOptions = _sut.CfSpaceOptions;

            await _sut.UpdateCfSpaceOptions();

            MockLogger.Verify(m => m.
                Error($"{DeploymentDialogViewModel.GetSpacesFailureMsg}. {fakeFailedSpacesResponse}"),
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
                cmdDetails: FakeFailureCmdResult);

            MockCloudFoundryService.Setup(mock => mock.
                GetSpacesForOrgAsync(FakeCfOrg, true, It.IsAny<int>()))
                    .ReturnsAsync(fakeFailedSpacesResponse);

            MockErrorDialogService.Setup(mock => mock.
                DisplayErrorDialog(DeploymentDialogViewModel.GetSpacesFailureMsg, fakeExplanation));

            _sut.SelectedCf = FakeCfInstance;
            _sut.SelectedOrg = FakeCfOrg;
            var initialSpaceOptions = _sut.CfSpaceOptions;

            await _sut.UpdateCfSpaceOptions();

            MockDialogService.VerifyAll();
            Assert.AreEqual(initialSpaceOptions, _sut.CfSpaceOptions);
        }
    }

    public class FakeOutputView : ViewModelTestSupport, IView
    {
        public IViewModel ViewModel { get; }

        public bool ShowMethodWasCalled { get; private set; }

        public FakeOutputView()
        {
            ViewModel = new OutputViewModel(Services);
            ShowMethodWasCalled = false;
        }

        public void Show()
        {
            ShowMethodWasCalled = true;
        }
    }

    public class FakeTasExplorerView : ViewModelTestSupport, IView
    {
        public IViewModel ViewModel { get; }

        public bool ShowMethodWasCalled { get; private set; }

        public FakeTasExplorerView()
        {
            MockCloudFoundryService.SetupGet(mock => mock.CloudFoundryInstances).Returns(new Dictionary<string, CloudFoundryInstance>());
            ViewModel = new TasExplorerViewModel(Services);
            ShowMethodWasCalled = false;
        }

        public void Show()
        {
            ShowMethodWasCalled = true;
        }
    }
}