using System.Collections.Generic;
using System.IO;
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
        private const string _fakeStack = "windows";
        private const string _fakeProjPath = "this\\is\\a\\fake\\path\\to\\a\\project\\directory";
        private const string _realPathToFakeDeploymentDir = "TestFakes";
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

            // * return fake view/viewmodel for output window
            MockViewLocatorService.Setup(mock =>
                mock.NavigateTo(nameof(OutputViewModel), null))
                    .Returns(new FakeOutputView());

            Assert.IsTrue(Directory.Exists(_realPathToFakeDeploymentDir));

            _sut = new DeploymentDialogViewModel(Services, null, _realPathToFakeDeploymentDir, FakeTargetFrameworkMoniker);

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
            MockTasExplorerViewModel.VerifyAll();
            MockErrorDialogService.VerifyAll();
        }


        [TestMethod]
        [TestCategory("ctor")]
        public void DeploymentDialogViewModel_SetsCfOrgOptionsToEmptyList_WhenConstructed()
        {
            var vm = new DeploymentDialogViewModel(Services, null, _fakeProjPath, FakeTargetFrameworkMoniker);

            Assert.IsNotNull(vm.CfOrgOptions);
            Assert.AreEqual(0, vm.CfOrgOptions.Count);
        }

        [TestMethod]
        [TestCategory("ctor")]
        public void DeploymentDialogViewModel_SetsCfSpaceOptionsToEmptyList_WhenConstructed()
        {
            var vm = new DeploymentDialogViewModel(Services, null, _fakeProjPath, FakeTargetFrameworkMoniker);

            Assert.IsNotNull(vm.CfSpaceOptions);
            Assert.AreEqual(0, vm.CfSpaceOptions.Count);
        }

        [TestMethod]
        [TestCategory("ctor")]
        [DataRow("fake cf name")]
        [DataRow("junk name")]
        [DataRow("asdf")]
        public void Constructor_SetsTargetNameToTasConnectionDisplayText_WhenTasConnectionIsNotNull(string connectionName)
        {
            var fakeCf = new CloudFoundryInstance(connectionName, FakeCfApiAddress);
            var fakeTasConnection = new FakeCfInstanceViewModel(fakeCf, Services);
            MockTasExplorerViewModel.SetupGet(m => m.TasConnection).Returns(fakeTasConnection);

            _sut = new DeploymentDialogViewModel(Services, null, _fakeProjPath, FakeTargetFrameworkMoniker);

            // sanity check
            Assert.IsNotNull(_sut.TasExplorerViewModel.TasConnection);
            Assert.AreEqual(connectionName, fakeTasConnection.DisplayText);

            Assert.IsNotNull(_sut.TargetName);
            Assert.AreEqual(fakeTasConnection.DisplayText, _sut.TargetName);
        }

        [TestMethod]
        [TestCategory("ctor")]
        public void Constructor_SetsIsLoggedInToTrue_WhenTasConnectionIsNotNull()
        {
            MockTasExplorerViewModel.SetupGet(m => m.TasConnection).Returns(new FakeCfInstanceViewModel(FakeCfInstance, Services));

            _sut = new DeploymentDialogViewModel(Services, null, _fakeProjPath, FakeTargetFrameworkMoniker);

            Assert.IsTrue(_sut.IsLoggedIn);
        }

        [TestMethod]
        [TestCategory("ctor")]
        public void Constructor_SetsTargetNameToNull_WhenTasConnectionIsNull()
        {
            // sanity check
            Assert.IsNull(_sut.TasExplorerViewModel.TasConnection);

            Assert.IsNull(_sut.TargetName);
        }
        
        [TestMethod]
        [TestCategory("ctor")]
        public void Constructor_UpdatesCfOrgOptions_WhenTasConnectionIsNotNull()
        {
            MockTasExplorerViewModel.SetupGet(m => m.TasConnection).Returns(new FakeCfInstanceViewModel(FakeCfInstance, Services));

            _sut = new DeploymentDialogViewModel(Services, null, _fakeProjPath, FakeTargetFrameworkMoniker);

            Assert.IsNotNull(_sut.TasExplorerViewModel.TasConnection);
            MockThreadingService.Verify(m => m.StartTask(_sut.UpdateCfOrgOptions), Times.Once);
        }

        [TestMethod]
        [TestCategory("ctor")]
        [TestCategory("SelectedDeploymentDirectoryPath")]
        public void Constructor_SetsDefaultDirectoryPath_EqualToProjectDirPath()
        {
            _sut = new DeploymentDialogViewModel(Services, null, _realPathToFakeDeploymentDir, FakeTargetFrameworkMoniker);

            Assert.AreEqual(_sut.PathToProjectRootDir, _sut.SelectedDeploymentDirectoryPath);
            Assert.AreEqual(_sut.PathToProjectRootDir, _sut.DirectoryPathLabel);
        }

        [TestMethod]
        [TestCategory("DeployApp")]
        public void DeployApp_UpdatesDeploymentStatus_WhenAppNameEmpty()
        {
            var receivedEvents = new List<string>();
            _sut.PropertyChanged += (sender, e) =>
            {
                receivedEvents.Add(e.PropertyName);
            };

            _sut.AppName = string.Empty;
            _sut.SelectedOrg = _fakeOrg;
            _sut.SelectedSpace = _fakeSpace;

            _sut.DeployApp(null);

            Assert.IsTrue(receivedEvents.Contains("DeploymentStatus"));
            Assert.IsTrue(_sut.DeploymentStatus.Contains("An error occurred:"));
            Assert.IsTrue(_sut.DeploymentStatus.Contains(DeploymentDialogViewModel.AppNameEmptyMsg));
        }

        [TestMethod]
        [TestCategory("DeployApp")]
        public void DeployApp_UpdatesDeploymentStatus_WhenTargetCfEmpty()
        {
            var receivedEvents = new List<string>();
            _sut.PropertyChanged += (sender, e) =>
            {
                receivedEvents.Add(e.PropertyName);
            };

            _sut.AppName = "fake app name";
            _sut.SelectedOrg = _fakeOrg;
            _sut.SelectedSpace = _fakeSpace;

            _sut.DeployApp(null);

            Assert.IsTrue(receivedEvents.Contains("DeploymentStatus"));
            Assert.IsTrue(_sut.DeploymentStatus.Contains("An error occurred:"));
            Assert.IsTrue(_sut.DeploymentStatus.Contains(DeploymentDialogViewModel.TargetEmptyMsg));
        }

        [TestMethod]
        [TestCategory("DeployApp")]
        public void DeployApp_UpdatesDeploymentStatus_WhenTargetOrgEmpty()
        {
            MockTasExplorerViewModel.SetupGet(m => m.TasConnection).Returns(new FakeCfInstanceViewModel(FakeCfInstance, Services));

            var receivedEvents = new List<string>();
            _sut.PropertyChanged += (sender, e) =>
            {
                receivedEvents.Add(e.PropertyName);
            };

            _sut.AppName = "fake app name";
            _sut.SelectedOrg = null;
            _sut.SelectedSpace = _fakeSpace;

            _sut.DeployApp(null);

            Assert.IsTrue(receivedEvents.Contains("DeploymentStatus"));
            Assert.IsTrue(_sut.DeploymentStatus.Contains("An error occurred:"));
            Assert.IsTrue(_sut.DeploymentStatus.Contains(DeploymentDialogViewModel.OrgEmptyMsg));
        }

        [TestMethod]
        [TestCategory("DeployApp")]
        public void DeployApp_UpdatesDeploymentStatus_WhenTargetSpaceEmpty()
        {
            MockTasExplorerViewModel.SetupGet(m => m.TasConnection).Returns(new FakeCfInstanceViewModel(FakeCfInstance, Services));

            var receivedEvents = new List<string>();
            _sut.PropertyChanged += (sender, e) =>
            {
                receivedEvents.Add(e.PropertyName);
            };

            _sut.AppName = "fake app name";
            _sut.SelectedOrg = _fakeOrg;
            _sut.SelectedSpace = null;

            _sut.DeployApp(null);

            Assert.IsTrue(receivedEvents.Contains("DeploymentStatus"));
            Assert.IsTrue(_sut.DeploymentStatus.Contains("An error occurred:"));
            Assert.IsTrue(_sut.DeploymentStatus.Contains(DeploymentDialogViewModel.SpaceEmptyMsg));
        }

        [TestMethod]
        [TestCategory("DeployApp")]
        public void DeployApp_ClosesDeploymentDialog()
        {
            MockTasExplorerViewModel.SetupGet(m => m.TasConnection).Returns(new FakeCfInstanceViewModel(FakeCfInstance, Services));

            var dw = new object();

            _sut.AppName = _fakeAppName;
            _sut.SelectedOrg = _fakeOrg;
            _sut.SelectedSpace = _fakeSpace;

            _sut.DeployApp(dw);
            MockDialogService.Verify(mock => mock.CloseDialog(dw, true), Times.Once);
        }

        [TestMethod]
        [TestCategory("StartDeployment")]
        public async Task StartDeploymentTask_IndicatesFullFWDeployment_WhenTFMStartsWith_NETFramework()
        {
            string targetFrameworkMoniker = ".NETFramework";
            _sut = new DeploymentDialogViewModel(Services, null, _realPathToFakeDeploymentDir, targetFrameworkMoniker)
            {
                AppName = _fakeAppName,
                SelectedOrg = _fakeOrg,
                SelectedSpace = _fakeSpace,
            };

            bool expectedFullFWFlag = true;

            MockCloudFoundryService.Setup(m => m.
                DeployAppAsync(_fakeCfInstance,
                               _fakeOrg,
                               _fakeSpace,
                               _fakeAppName,
                               _realPathToFakeDeploymentDir,
                               expectedFullFWFlag,
                               It.IsAny<StdOutDelegate>(),
                               It.IsAny<StdErrDelegate>(),
                               null,
                               true, null, null))
                .ReturnsAsync(FakeSuccessDetailedResult);

            await _sut.StartDeployment();

            MockCloudFoundryService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("StartDeployment")]
        public async Task StartDeploymentTask_UpdatesDeploymentInProgress_WhenComplete()
        {
            MockCloudFoundryService.Setup(mock =>
                mock.DeployAppAsync(_fakeCfInstance,
                                    _fakeOrg,
                                    _fakeSpace,
                                    _fakeAppName,
                                    _realPathToFakeDeploymentDir,
                                    _defaultFullFWFlag,
                                    It.IsAny<StdOutDelegate>(),
                                    It.IsAny<StdErrDelegate>(),
                                    null,
                                    true, null, null))
                    .ReturnsAsync(FakeSuccessDetailedResult);

            _sut.AppName = _fakeAppName;
            _sut.SelectedOrg = _fakeOrg;
            _sut.SelectedSpace = _fakeSpace;

            _sut.DeploymentInProgress = true;

            await _sut.StartDeployment();

            Assert.IsFalse(_sut.DeploymentInProgress);
        }

        [TestMethod]
        [TestCategory("StartDeployment")]
        [DataRow("windows")]
        [DataRow("linux")]
        public async Task StartDeploymentTask_PassesSelectedStack_ForDeployment(string stack)
        {
            MockCloudFoundryService.Setup(mock =>
                mock.DeployAppAsync(_fakeCfInstance,
                                    _fakeOrg,
                                    _fakeSpace,
                                    _fakeAppName,
                                    _realPathToFakeDeploymentDir,
                                    _defaultFullFWFlag,
                                    It.IsAny<StdOutDelegate>(),
                                    It.IsAny<StdErrDelegate>(),
                                    stack,
                                    true, null, null))
                    .ReturnsAsync(FakeSuccessDetailedResult);

            _sut.AppName = _fakeAppName;
            _sut.SelectedOrg = _fakeOrg;
            _sut.SelectedSpace = _fakeSpace;
            _sut.SelectedStack = stack;
            Assert.IsNotNull(_sut.SelectedStack);

            await _sut.StartDeployment();
        }
        
        [TestMethod]
        [TestCategory("StartDeployment")]
        public async Task StartDeploymentTask_PassesDirectoryPath_ForDeployment()
        {
            var realPathToFakeDeploymentDir = _realPathToFakeDeploymentDir;

            MockCloudFoundryService.Setup(mock =>
                mock.DeployAppAsync(_fakeCfInstance,
                                    _fakeOrg,
                                    _fakeSpace,
                                    _fakeAppName,
                                    realPathToFakeDeploymentDir,
                                    _defaultFullFWFlag,
                                    It.IsAny<StdOutDelegate>(),
                                    It.IsAny<StdErrDelegate>(),
                                    _fakeStack,
                                    true, null, null))
                    .ReturnsAsync(FakeSuccessDetailedResult);

            _sut.AppName = _fakeAppName;
            _sut.SelectedOrg = _fakeOrg;
            _sut.SelectedSpace = _fakeSpace;
            _sut.SelectedStack = _fakeStack;
            _sut.SelectedDeploymentDirectoryPath = realPathToFakeDeploymentDir;

            Assert.IsNotNull(_sut.SelectedStack);

            await _sut.StartDeployment();
        }

        [TestMethod]
        [TestCategory("StartDeployment")]
        [DataRow(true)]
        [DataRow(false)]
        public async Task StartDeployment_PassesSourceDeploymentBool_ForDeployment(bool isSourceDeployment)
        {
            MockCloudFoundryService.Setup(mock =>
                mock.DeployAppAsync(_fakeCfInstance,
                                    _fakeOrg,
                                    _fakeSpace,
                                    _fakeAppName,
                                    _realPathToFakeDeploymentDir,
                                    _defaultFullFWFlag,
                                    It.IsAny<StdOutDelegate>(),
                                    It.IsAny<StdErrDelegate>(),
                                    _fakeStack,
                                    isSourceDeployment,
                                    null, null))
                    .ReturnsAsync(FakeSuccessDetailedResult);

            _sut.AppName = _fakeAppName;
            _sut.SelectedOrg = _fakeOrg;
            _sut.SelectedSpace = _fakeSpace;
            _sut.SelectedStack = _fakeStack;
            _sut.SelectedDeploymentDirectoryPath = _realPathToFakeDeploymentDir;
            _sut.SourceDeployment = isSourceDeployment;

            Assert.AreEqual(isSourceDeployment, _sut.SourceDeployment);

            await _sut.StartDeployment();

            MockCloudFoundryService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("StartDeployment")]
        public async Task StartDeploymentTask_PassesOutputViewModelAppendLineMethod_AsCallbacks()
        {
            _sut.AppName = _fakeAppName;
            _sut.SelectedOrg = _fakeOrg;
            _sut.SelectedSpace = _fakeSpace;

            StdOutDelegate expectedStdOutCallback = _sut.OutputViewModel.AppendLine;
            StdErrDelegate expectedStdErrCallback = _sut.OutputViewModel.AppendLine;

            MockCloudFoundryService.Setup(mock => mock.
              DeployAppAsync(_fakeCfInstance,
                             _fakeOrg,
                             _fakeSpace,
                             _fakeAppName,
                             _realPathToFakeDeploymentDir,
                             _defaultFullFWFlag,
                             expectedStdOutCallback,
                             expectedStdErrCallback,
                             null,
                             true, null, null))
                .ReturnsAsync(FakeSuccessDetailedResult);

            await _sut.StartDeployment();
        }

        [TestMethod]
        [TestCategory("StartDeployment")]
        public async Task StartDeploymentTask_LogsError_WhenDeployResultReportsFailure()
        {
            _sut.AppName = _fakeAppName;
            _sut.SelectedOrg = _fakeOrg;
            _sut.SelectedSpace = _fakeSpace;

            StdOutDelegate expectedStdOutCallback = _sut.OutputViewModel.AppendLine;
            StdErrDelegate expectedStdErrCallback = _sut.OutputViewModel.AppendLine;

            MockCloudFoundryService.Setup(mock => mock.
                DeployAppAsync(_fakeCfInstance,
                               _fakeOrg,
                               _fakeSpace,
                               _fakeAppName,
                               _realPathToFakeDeploymentDir,
                               _defaultFullFWFlag,
                               expectedStdOutCallback,
                               expectedStdErrCallback,
                               null,
                               true, null, null))
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
            _sut.SelectedOrg = _fakeOrg;
            _sut.SelectedSpace = _fakeSpace;

            StdOutDelegate expectedStdOutCallback = _sut.OutputViewModel.AppendLine;
            StdErrDelegate expectedStdErrCallback = _sut.OutputViewModel.AppendLine;

            MockCloudFoundryService.Setup(mock => mock.
                DeployAppAsync(_fakeCfInstance,
                               _fakeOrg,
                               _fakeSpace,
                               _fakeAppName,
                               _realPathToFakeDeploymentDir,
                               _defaultFullFWFlag,
                               expectedStdOutCallback,
                               expectedStdErrCallback,
                               null,
                               true, null, null))
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
            _sut.SelectedOrg = _fakeOrg;
            _sut.SelectedSpace = _fakeSpace;

            StdOutDelegate expectedStdOutCallback = _sut.OutputViewModel.AppendLine;
            StdErrDelegate expectedStdErrCallback = _sut.OutputViewModel.AppendLine;

            var invalidRefreshTokenFailure = new DetailedResult(false, "junk error", FakeFailureCmdResult)
            {
                FailureType = FailureType.InvalidRefreshToken
            };

            MockCloudFoundryService.Setup(mock => mock.
                DeployAppAsync(_fakeCfInstance,
                               _fakeOrg,
                               _fakeSpace,
                               _fakeAppName,
                               _realPathToFakeDeploymentDir,
                               _defaultFullFWFlag,
                               expectedStdOutCallback,
                               expectedStdErrCallback,
                               null,
                               true, null, null))
                    .ReturnsAsync(invalidRefreshTokenFailure);

            MockTasExplorerViewModel.SetupSet(m => m.AuthenticationRequired = true).Verifiable();

            await _sut.StartDeployment();

            MockTasExplorerViewModel.VerifyAll();
        }

        [TestMethod]
        public async Task UpdateCfOrgOptions_RaisesPropertyChangedEvent_WhenOrgsRequestSucceeds()
        {
            var fakeCf = new CloudFoundryInstance("junk name", FakeCfApiAddress);
            var fakeTasConnection = new FakeCfInstanceViewModel(fakeCf, Services);
            MockTasExplorerViewModel.SetupGet(m => m.TasConnection).Returns(fakeTasConnection);

            var fakeOrgsList = new List<CloudFoundryOrganization> { FakeCfOrg };

            var fakeSuccessfulOrgsResponse = new DetailedResult<List<CloudFoundryOrganization>>(
                content: fakeOrgsList,
                succeeded: true,
                explanation: null,
                cmdDetails: FakeSuccessCmdResult);

            MockCloudFoundryService.Setup(mock => mock.
                GetOrgsForCfInstanceAsync(fakeCf, true, It.IsAny<int>()))
                    .ReturnsAsync(fakeSuccessfulOrgsResponse);


            Assert.AreEqual(0, _sut.CfOrgOptions.Count);

            await _sut.UpdateCfOrgOptions();

            Assert.AreEqual(1, _sut.CfOrgOptions.Count);
            Assert.AreEqual(FakeCfOrg, _sut.CfOrgOptions[0]);

            Assert.IsTrue(_receivedEvents.Contains("CfOrgOptions"));
        }

        [TestMethod]
        public async Task UpdateCfOrgOptions_SetsCfOrgOptionsToEmptyList_WhenSelectedCfIsNull()
        {

            await _sut.UpdateCfOrgOptions();

            Assert.AreEqual(0, _sut.CfOrgOptions.Count);

            Assert.IsTrue(_receivedEvents.Contains("CfOrgOptions"));
        }

        [TestMethod]
        public async Task UpdateCfOrgOptions_DisplaysErrorDialog_WhenOrgsResponseReportsFailure()
        {
            var fakeCf = new CloudFoundryInstance("fake junk name", FakeCfApiAddress);
            var fakeTasConnection = new FakeCfInstanceViewModel(fakeCf, Services);
            MockTasExplorerViewModel.SetupGet(m => m.TasConnection).Returns(fakeTasConnection);

            var fakeExplanation = "junk";

            var fakeFailedOrgsResponse = new DetailedResult<List<CloudFoundryOrganization>>(
                content: null,
                succeeded: false,
                explanation: fakeExplanation,
                cmdDetails: FakeFailureCmdResult);

            MockCloudFoundryService.Setup(mock => mock.
                GetOrgsForCfInstanceAsync(fakeCf, true, It.IsAny<int>()))
                    .ReturnsAsync(fakeFailedOrgsResponse);

            MockErrorDialogService.Setup(mock => mock.
                DisplayErrorDialog(DeploymentDialogViewModel.GetOrgsFailureMsg, fakeExplanation));

            var initialOrgOptions = _sut.CfOrgOptions;

            await _sut.UpdateCfOrgOptions();

            MockDialogService.VerifyAll();
            Assert.AreEqual(initialOrgOptions, _sut.CfOrgOptions);
        }

        [TestMethod]
        public async Task UpdateCfOrgOptions_LogsError_WhenOrgsResponseReportsFailure()
        {
            var fakeCf = new CloudFoundryInstance("fake junk name", FakeCfApiAddress);
            var fakeTasConnection = new FakeCfInstanceViewModel(fakeCf, Services);
            MockTasExplorerViewModel.SetupGet(m => m.TasConnection).Returns(fakeTasConnection);

            var fakeExplanation = "junk";

            var fakeFailedOrgsResponse = new DetailedResult<List<CloudFoundryOrganization>>(
                content: null,
                succeeded: false,
                explanation: fakeExplanation,
                cmdDetails: FakeFailureCmdResult);

            MockCloudFoundryService.Setup(mock => mock.
                GetOrgsForCfInstanceAsync(fakeCf, true, It.IsAny<int>()))
                    .ReturnsAsync(fakeFailedOrgsResponse);

            MockErrorDialogService.Setup(mock => mock.
                DisplayErrorDialog(DeploymentDialogViewModel.GetOrgsFailureMsg, fakeExplanation));

            var initialOrgOptions = _sut.CfOrgOptions;

            await _sut.UpdateCfOrgOptions();

            MockLogger.Verify(m => m.
                Error($"{DeploymentDialogViewModel.GetOrgsFailureMsg}. {fakeFailedOrgsResponse}"),
                    Times.Once);
        }

        [TestMethod]
        public async Task UpdateCfSpaceOptions_RaisesPropertyChangedEvent_WhenSpacesRequestSucceeds()
        {
            MockTasExplorerViewModel.SetupGet(m => m.TasConnection).Returns(new FakeCfInstanceViewModel(FakeCfInstance, Services));

            var fakeSpacesList = new List<CloudFoundrySpace> { FakeCfSpace };

            var fakeSuccessfulSpacesResponse = new DetailedResult<List<CloudFoundrySpace>>(
                content: fakeSpacesList,
                succeeded: true,
                explanation: null,
                cmdDetails: FakeSuccessCmdResult);

            MockCloudFoundryService.Setup(mock => mock.
                GetSpacesForOrgAsync(FakeCfOrg, true, It.IsAny<int>()))
                    .ReturnsAsync(fakeSuccessfulSpacesResponse);

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
            MockTasExplorerViewModel.SetupGet(m => m.TasConnection).Returns(new FakeCfInstanceViewModel(FakeCfInstance, Services));

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
            MockTasExplorerViewModel.SetupGet(m => m.TasConnection).Returns(new FakeCfInstanceViewModel(FakeCfInstance, Services));

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

            _sut.SelectedOrg = FakeCfOrg;
            var initialSpaceOptions = _sut.CfSpaceOptions;

            await _sut.UpdateCfSpaceOptions();

            MockDialogService.VerifyAll();
            Assert.AreEqual(initialSpaceOptions, _sut.CfSpaceOptions);
        }

        [TestMethod]
        [TestCategory("CanOpenLoginView")]
        public void CanOpenLoginView_ReturnsTrue()
        {
            Assert.IsTrue(_sut.CanOpenLoginView(null));
        }

        [TestMethod]
        [TestCategory("OpenLoginView")]
        public void OpenLoginView_InvokesOpenLoginViewOnTasExplorerViewModel()
        {
            object fakeArg = new object();

            _sut.OpenLoginView(fakeArg);

            MockTasExplorerViewModel.Verify(m => m.OpenLoginView(fakeArg), Times.Once);
        }

        [TestMethod]
        [TestCategory("OpenLoginView")]
        public void OpenLoginView_SetsCfInstanceOptions_WhenTasConnectionGetsSet()
        {
            object fakeArg = new object();

            MockTasExplorerViewModel.Setup(m => m.
                OpenLoginView(fakeArg))
                    .Callback(() =>
                    {
                        MockTasExplorerViewModel.SetupGet(m => m.TasConnection)
                            .Returns(new CfInstanceViewModel(FakeCfInstance, null, Services));
                    });

            Assert.AreEqual(0, _sut.CfInstanceOptions.Count);

            _sut.OpenLoginView(fakeArg);

            Assert.AreEqual(1, _sut.CfInstanceOptions.Count);
            Assert.AreEqual(FakeCfInstance, _sut.CfInstanceOptions[0]);
        }

        [TestMethod]
        [TestCategory("OpenLoginView")]
        public void OpenLoginView_DoesNotChangeCfInstanceOptions_WhenTasConnectionDoesNotGetSet()
        {
            object fakeArg = new object();

            MockTasExplorerViewModel.Setup(m => m.
                OpenLoginView(fakeArg))
                    .Callback(() =>
                    {
                        MockTasExplorerViewModel.SetupGet(m => m.TasConnection)
                            .Returns((CfInstanceViewModel)null);
                    });

            Assert.AreEqual(0, _sut.CfInstanceOptions.Count);

            _sut.OpenLoginView(fakeArg);

            Assert.AreEqual(0, _sut.CfInstanceOptions.Count);
        }

        [TestMethod]
        [TestCategory("OpenLoginView")]
        public void OpenLoginView_SetsIsLoggedInPropertyToTrue_WhenTasConnectionGetsSet()
        {
            // pre-check
            Assert.IsNull(_sut.TasExplorerViewModel.TasConnection);
            Assert.IsFalse(_sut.IsLoggedIn);

            //arrange
            MockTasExplorerViewModel.Setup(m => m.
                OpenLoginView(null))
                    .Callback(() =>
                    {
                        MockTasExplorerViewModel.SetupGet(m => m.TasConnection)
                            .Returns(new CfInstanceViewModel(FakeCfInstance, null, Services));
                    });
        
            //act
            _sut.OpenLoginView(null);

            //assert
            Assert.IsNotNull(_sut.TasExplorerViewModel.TasConnection);
            Assert.IsTrue(_sut.IsLoggedIn);
        }
        
        [TestMethod]
        [TestCategory("OpenLoginView")]
        public void OpenLoginView_SetsTargetName_WhenTasConnectionIsNotNull()
        {
            var initialTargetName = _sut.TargetName;
            Assert.IsNull(_sut.TasExplorerViewModel.TasConnection);
            
            MockTasExplorerViewModel.Setup(m => m.
                OpenLoginView(null))
                    .Callback(() =>
                    {
                        MockTasExplorerViewModel.SetupGet(m => m.TasConnection)
                            .Returns(new CfInstanceViewModel(FakeCfInstance, null, Services));
                    });

            _sut.OpenLoginView(null);

            Assert.IsNotNull(_sut.TasExplorerViewModel.TasConnection);
            Assert.AreNotEqual(initialTargetName, _sut.TargetName);
            Assert.AreEqual(_sut.TasExplorerViewModel.TasConnection.DisplayText, _sut.TargetName);
        }

        [TestMethod]
        [TestCategory("OpenLoginView")]
        public void OpenLoginView_UpdatesCfOrgOptions_WhenTasConnectionGetsSet()
        {
            MockTasExplorerViewModel.Setup(m => m.
                OpenLoginView(null))
                    .Callback(() =>
                    {
                        MockTasExplorerViewModel.SetupGet(m => m.TasConnection)
                            .Returns(new CfInstanceViewModel(FakeCfInstance, null, Services));
                    });

            Assert.IsNull(_sut.TasExplorerViewModel.TasConnection);

            _sut.OpenLoginView(null);

            Assert.IsNotNull(_sut.TasExplorerViewModel.TasConnection);
            MockThreadingService.Verify(m => m.StartTask(_sut.UpdateCfOrgOptions), Times.Once);
        }

        [TestMethod]
        [TestCategory("IsLoggedIn")]
        public void IsLoggedIn_ReturnsTrue_WhenTasConnectionIsNotNull()
        {
            MockTasExplorerViewModel.SetupGet(m => m.TasConnection).Returns(new FakeCfInstanceViewModel(FakeCfInstance, Services));

            _sut = new DeploymentDialogViewModel(Services, null, _fakeProjPath, FakeTargetFrameworkMoniker);

            Assert.IsNotNull(_sut.TasExplorerViewModel.TasConnection);
            Assert.IsTrue(_sut.IsLoggedIn);
        }

        [TestMethod]
        [TestCategory("IsLoggedIn")]
        public void IsLoggedIn_ReturnsFalse_WhenTasConnectionIsNull()
        {
            Assert.IsNull(_sut.TasExplorerViewModel.TasConnection);
            Assert.IsFalse(_sut.IsLoggedIn);
        }

        [TestMethod]
        [TestCategory("IsLoggedIn")]
        public void IsLoggedIn_SetterRaisesPropChangedEvent()
        {
            Assert.AreEqual(0, _receivedEvents.Count);
            Assert.IsFalse(_sut.IsLoggedIn);

            _sut.IsLoggedIn = true;

            Assert.AreEqual(1, _receivedEvents.Count);
            Assert.IsTrue(_receivedEvents.Contains("IsLoggedIn"));
        }

        [TestMethod]
        [TestCategory("TargetName")]
        public void TargetName_SetterRaisesPropChangedEvent()
        {
            Assert.AreEqual(0, _receivedEvents.Count);

            _sut.TargetName = "new name";

            Assert.AreEqual(1, _receivedEvents.Count);
            Assert.IsTrue(_receivedEvents.Contains("TargetName"));
        }

        [TestMethod]
        [TestCategory("StackOptions")]
        public void StackOptions_Returns_LinuxAndWindows()
        {
            Assert.AreEqual(2, _sut.StackOptions.Count);
            Assert.IsTrue(_sut.StackOptions.Contains("windows"));
            Assert.IsTrue(_sut.StackOptions.Contains("linux"));
        }

        [TestMethod]
        [TestCategory("StackOptions")]
        public void StackOptions_Returns_Windows_WhenTargetFrameworkIsNETFramework()
        {
            _sut = new DeploymentDialogViewModel(Services, null, _fakeProjPath, targetFrameworkMoniker: DeploymentDialogViewModel.FullFrameworkTFM);

            Assert.IsTrue(_sut._fullFrameworkDeployment);

            Assert.AreEqual(1, _sut.StackOptions.Count);
            Assert.IsTrue(_sut.StackOptions.Contains("windows"));
        }

        [TestMethod]
        [TestCategory("ManifestPath")]
        public void ManifestPathSetter_SetsAppName_WhenManifestExistsAndContainsAppName()
        {
            var manifestPath = "TestFakes/fake-manifest.yml";
            var expectedFakeAppNameFromManifest = "my-cool-app";

            Assert.IsTrue(File.ReadAllText(manifestPath).Contains("- name"));
            Assert.IsTrue(File.ReadAllText(manifestPath).Contains(expectedFakeAppNameFromManifest));

            Assert.AreNotEqual(expectedFakeAppNameFromManifest, _sut.AppName);

            _sut.ManifestPath = manifestPath;

            Assert.AreEqual(expectedFakeAppNameFromManifest, _sut.AppName);
        }
        
        [TestMethod]
        [TestCategory("ManifestPath")]
        public void ManifestPathSetter_DisplaysError_AndDoesNotChangeAppName_WhenManifestDoesNotExist()
        {
            var pathToNonexistentManifest = "bogus//path";
            var initialAppName = _sut.AppName;

            MockErrorDialogService.Setup(m => m.
                DisplayErrorDialog(DeploymentDialogViewModel.ManifestNotFoundTitle, It.Is<string>(s => s.Contains(pathToNonexistentManifest) && s.Contains("does not appear to be a valid path to a manifest"))))
                    .Verifiable();

            _sut.ManifestPath = pathToNonexistentManifest;

            Assert.AreEqual(initialAppName, _sut.AppName);
        }

        [TestMethod]
        [TestCategory("ManifestPath")]
        public void ManifestPathSetter_DoesNotChangeAppName_WhenManifestExistsButDoesNotContainAppName()
        {
            var pathToInvalidManifest = "TestFakes/fake-invalid-manifest.yml";

            Assert.IsFalse(File.ReadAllText(pathToInvalidManifest).Contains("- name"));
            var initialAppName = _sut.AppName;

            _sut.ManifestPath = pathToInvalidManifest;

            Assert.AreEqual(initialAppName, _sut.AppName);
        }

        [TestMethod]
        [TestCategory("ManifestPath")]
        [TestCategory("SelectedStack")]
        public void ManifestPathSetter_SetsSelectedStack_WhenManifestExistsAndContainsStack()
        {
            var pathToFakeManifest = "TestFakes/fake-manifest.yml";
            var expectedFakeStackNameFromManifest = "windows";

            Assert.IsTrue(File.ReadAllText(pathToFakeManifest).Contains("stack:"));
            Assert.IsTrue(File.ReadAllText(pathToFakeManifest).Contains(expectedFakeStackNameFromManifest));

            Assert.AreNotEqual(expectedFakeStackNameFromManifest, _sut.SelectedStack);

            _sut.ManifestPath = pathToFakeManifest;

            Assert.AreEqual(expectedFakeStackNameFromManifest, _sut.SelectedStack);
        }

        [TestMethod]
        [TestCategory("ManifestPath")]
        [TestCategory("SelectedStack")]
        public void ManifestPathSetter_DoesNotChangeStack_WhenManifestDoesNotExist()
        {
            var pathToNonexistentManifest = "bogus//path";
            var initialStack = _sut.SelectedStack;

            _sut.ManifestPath = pathToNonexistentManifest;

            Assert.AreEqual(initialStack, _sut.SelectedStack);
        }

        [TestMethod]
        [TestCategory("ManifestPath")]
        [TestCategory("SelectedStack")]
        public void ManifestPathSetter_DoesNotChangeStack_WhenManifestExistsAndContainsInvalidStack()
        {
            var pathToInvalidManifest = "TestFakes/fake-invalid-manifest.yml";
            var expectedInvalidFakeStackNameFromManifest = "my-cool-stack";
            var initialStack = _sut.SelectedStack;

            var fakeManifestContents = File.ReadAllText(pathToInvalidManifest);

            Assert.IsTrue(fakeManifestContents.Contains("stack:"));
            Assert.IsTrue(fakeManifestContents.Contains(expectedInvalidFakeStackNameFromManifest));

            _sut.ManifestPath = pathToInvalidManifest;

            Assert.AreEqual(initialStack, _sut.SelectedStack);
        }

        [TestMethod]
        [TestCategory("SelectedStack")]
        public void SelectedStackSetter_SetsValue_WhenValueExistsInStackOptions()
        {
            var stackVal = "windows";

            Assert.IsTrue(_sut.StackOptions.Contains(stackVal));

            _sut.SelectedStack = stackVal;

            Assert.AreEqual(stackVal, _sut.SelectedStack);
            Assert.AreEqual(1, _receivedEvents.Count);
            Assert.AreEqual("SelectedStack", _receivedEvents[0]);
        }

        [TestMethod]
        [TestCategory("SelectedStack")]
        public void SelectedStackSetter_DoesNotSetValue_WhenValueDoesNotExistInStackOptions()
        {
            var bogusStackVal = "junk";
            var initialSelectedStack = _sut.SelectedStack;

            Assert.IsFalse(_sut.StackOptions.Contains(bogusStackVal));

            _sut.SelectedStack = bogusStackVal;

            Assert.AreEqual(initialSelectedStack, _sut.SelectedStack);
            Assert.AreEqual(0, _receivedEvents.Count);
        }

        [TestMethod]
        [TestCategory("SelectedDeploymentDirectoryPath")]
        public void DirectoryPathSetter_SetsDirectoryPathLabel_WhenDirectoryExistsAtGivenPath()
        {
            _sut.SelectedDeploymentDirectoryPath = "junk//path";
            var initialDirectoryPathLabel = _sut.DirectoryPathLabel;

            Assert.IsNull(_sut.SelectedDeploymentDirectoryPath);
            Assert.AreEqual("<none specified>", _sut.DirectoryPathLabel);

            _sut.SelectedDeploymentDirectoryPath = _realPathToFakeDeploymentDir;

            Assert.IsNotNull(_sut.SelectedDeploymentDirectoryPath);
            Assert.AreNotEqual(initialDirectoryPathLabel, _sut.DirectoryPathLabel);
            Assert.AreEqual(_realPathToFakeDeploymentDir, _sut.DirectoryPathLabel);
            Assert.AreEqual(_realPathToFakeDeploymentDir, _sut.SelectedDeploymentDirectoryPath);
        }

        [TestMethod]
        [TestCategory("SelectedDeploymentDirectoryPath")]
        public void DirectoryPathSetter_DisplaysError_AndSetsDirectoryPathLabelToNoneSpecified_WhenNoDirectoryExistsAtGivenPath()
        {
            var fakePath = "asdf//junk";
            Assert.IsFalse(Directory.Exists(fakePath));

            _sut.DirectoryPathLabel = "fake initial value";

            Assert.AreNotEqual("<none specified>", _sut.DirectoryPathLabel);

            _sut.SelectedDeploymentDirectoryPath = fakePath;

            Assert.AreEqual("<none specified>", _sut.DirectoryPathLabel);

            MockErrorDialogService.Verify(
                m => m.DisplayErrorDialog(DeploymentDialogViewModel.DirectoryNotFoundTitle, It.Is<string>(s => s.Contains(fakePath) && s.Contains("does not appear to be a valid path"))),
                Times.Once);
        }

        [TestMethod]
        [TestCategory("SourceDeployment")]
        [TestCategory("DeploymentButtonLabel")]
        public void SourceDeployment_SetsDeploymentButtonLabelToPushSource_WhenSetToTrue()
        {
            _sut.DeploymentButtonLabel = "fake initial value";

            _sut.SourceDeployment = true;

            Assert.AreEqual("Push app (from source)", _sut.DeploymentButtonLabel);
        }

        [TestMethod]
        [TestCategory("SourceDeployment")]
        [TestCategory("DeploymentButtonLabel")]
        public void SourceDeployment_SetsDeploymentButtonLabelToPushBinaries_WhenSetToFalse()
        {
            _sut.DeploymentButtonLabel = "fake initial value";

            _sut.SourceDeployment = false;

            Assert.AreEqual("Push app (from binaries)", _sut.DeploymentButtonLabel);
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
}