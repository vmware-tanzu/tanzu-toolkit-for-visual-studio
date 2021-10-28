using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
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
        private const string _fakeProjName = "fake project name";
        private const string _fakeStack = "windows";
        private const string _fakeBuildpackName1 = "bp1";
        private const string _fakeBuildpackName2 = "bp2";
        private const string _fakeBuildpackName3 = "bp3";
        private ObservableCollection<string> _fakeSelectedBuildpacks;
        private const string FakeTargetFrameworkMoniker = "junk";
        private static readonly CloudFoundryInstance _fakeCfInstance = new CloudFoundryInstance("", "");
        private static readonly CloudFoundryOrganization _fakeOrg = new CloudFoundryOrganization("", "", _fakeCfInstance);
        private readonly CloudFoundrySpace _fakeSpace = new CloudFoundrySpace("", "", _fakeOrg);
        private DeploymentDialogViewModel _sut;
        private List<string> _receivedEvents;

        [TestInitialize]
        public void TestInit()
        {
            RenewMockServices();

            // * return fake view/viewmodel for output window
            MockViewLocatorService.Setup(mock =>
                mock.NavigateTo(nameof(OutputViewModel), null))
                    .Returns(new FakeOutputView());

            MockFileService.Setup(m => m.FileExists(_fakeManifestPath)).Returns(true);
            MockFileService.Setup(m => m.DirectoryExists(_fakeProjectPath)).Returns(true);
            MockFileService.Setup(m => m.DirContainsFiles(_fakeProjectPath)).Returns(true);

            _fakeSelectedBuildpacks = new ObservableCollection<string> { _fakeBuildpackName1, _fakeBuildpackName2, _fakeBuildpackName3 };

            _sut = new DeploymentDialogViewModel(Services, _fakeProjName, _fakeProjectPath, FakeTargetFrameworkMoniker);

            _sut.SelectedBuildpacks = _fakeSelectedBuildpacks;

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
            var vm = new DeploymentDialogViewModel(Services, null, _fakeProjectPath, FakeTargetFrameworkMoniker);

            Assert.IsNotNull(vm.CfOrgOptions);
            Assert.AreEqual(0, vm.CfOrgOptions.Count);
        }

        [TestMethod]
        [TestCategory("ctor")]
        public void DeploymentDialogViewModel_SetsCfSpaceOptionsToEmptyList_WhenConstructed()
        {
            var vm = new DeploymentDialogViewModel(Services, null, _fakeProjectPath, FakeTargetFrameworkMoniker);

            Assert.IsNotNull(vm.CfSpaceOptions);
            Assert.AreEqual(0, vm.CfSpaceOptions.Count);
        }

        [TestMethod]
        [TestCategory("ctor")]
        public void Constructor_SetsBuildpackOptionsToEmptyList()
        {
            CollectionAssert.AreEqual(new List<string>(), _sut.BuildpackOptions);
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

            _sut = new DeploymentDialogViewModel(Services, null, _fakeProjectPath, FakeTargetFrameworkMoniker);

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

            _sut = new DeploymentDialogViewModel(Services, null, _fakeProjectPath, FakeTargetFrameworkMoniker);

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

            _sut = new DeploymentDialogViewModel(Services, null, _fakeProjectPath, FakeTargetFrameworkMoniker);

            Assert.IsNotNull(_sut.TasExplorerViewModel.TasConnection);
            MockThreadingService.Verify(m => m.StartTask(_sut.UpdateCfOrgOptions), Times.Once);
        }

        [TestMethod]
        [TestCategory("ctor")]
        public void Constructor_UpdatesBuildpackOptions_WhenTasConnectionIsNotNull()
        {
            MockTasExplorerViewModel.SetupGet(m => m.TasConnection).Returns(new FakeCfInstanceViewModel(FakeCfInstance, Services));

            _sut = new DeploymentDialogViewModel(Services, null, _fakeProjectPath, FakeTargetFrameworkMoniker);

            Assert.IsNotNull(_sut.TasExplorerViewModel.TasConnection);
            MockThreadingService.Verify(m => m.StartTask(_sut.UpdateBuildpackOptions), Times.Once);
        }

        [TestMethod]
        [TestCategory("ctor")]
        [TestCategory("DeploymentDirectoryPath")]
        public void Constructor_SetsDefaultDirectoryPath_EqualToProjectDirPath()
        {
            _sut = new DeploymentDialogViewModel(Services, null, _fakeProjectPath, FakeTargetFrameworkMoniker);

            Assert.AreEqual(_fakeProjectPath, _sut.PathToProjectRootDir);
            Assert.AreEqual(_sut.PathToProjectRootDir, _sut.DeploymentDirectoryPath);
            Assert.AreEqual(_sut.PathToProjectRootDir, _sut.DirectoryPathLabel);
        }

        [TestMethod]
        [TestCategory("CanDeployApp")]
        public void CanDeployApp_ReturnsTrue_WhenAllRequiredFieldsAreValid()
        {
            _sut.AppName = "FakeAppName";
            _sut.SelectedOrg = FakeCfOrg;
            _sut.SelectedSpace = FakeCfSpace;
            _sut.IsLoggedIn = true;

            Assert.IsTrue(_sut.CanDeployApp(null));
        }

        [TestMethod]
        [TestCategory("CanDeployApp")]
        public void CanDeployApp_ReturnsFalse_WhenAppNameEmpty()
        {
            _sut.AppName = "";
            _sut.SelectedOrg = FakeCfOrg;
            _sut.SelectedSpace = FakeCfSpace;
            _sut.IsLoggedIn = true;

            Assert.IsFalse(_sut.CanDeployApp(null));
        }

        [TestMethod]
        [TestCategory("CanDeployApp")]
        public void CanDeployApp_ReturnsFalse_WhenSelectedOrgEmpty()
        {
            _sut.AppName = "junk";
            _sut.SelectedOrg = null;
            _sut.SelectedSpace = FakeCfSpace;
            _sut.IsLoggedIn = true;

            Assert.IsFalse(_sut.CanDeployApp(null));
        }

        [TestMethod]
        [TestCategory("CanDeployApp")]
        public void CanDeployApp_ReturnsFalse_WhenSelectedSpaceEmpty()
        {
            _sut.AppName = "junk";
            _sut.SelectedOrg = FakeCfOrg;
            _sut.SelectedSpace = null;
            _sut.IsLoggedIn = true;

            Assert.IsFalse(_sut.CanDeployApp(null));
        }

        [TestMethod]
        [TestCategory("CanDeployApp")]
        public void CanDeployApp_ReturnsFalse_WhenNotLoggedIn()
        {
            _sut.AppName = "junk";
            _sut.SelectedOrg = FakeCfOrg;
            _sut.SelectedSpace = FakeCfSpace;
            _sut.IsLoggedIn = false;

            Assert.IsFalse(_sut.CanDeployApp(null));
        }

        [TestMethod]
        [TestCategory("DeployApp")]
        public void DeployApp_SetsDeploymentInProgress_AndInvokesStartDeployment_AndClosesDeploymentDialog_WhenCanDeployAppIsTrue()
        {
            var dw = new object();

            _sut.AppName = _fakeAppName;
            _sut.SelectedOrg = _fakeOrg;
            _sut.SelectedSpace = _fakeSpace;
            _sut.IsLoggedIn = true;

            Assert.IsTrue(_sut.CanDeployApp(null));
            Assert.IsFalse(_sut.DeploymentInProgress);

            _sut.DeployApp(dw);

            Assert.IsTrue(_sut.DeploymentInProgress);

            MockDialogService.Verify(mock => mock.CloseDialog(dw, true), Times.Once);
            MockThreadingService.Verify(mock => mock.StartTask(_sut.StartDeployment), Times.Once);
        }

        [TestMethod]
        public void DeployApp_DoesNothing_WhenCanDeployAppIsFalse()
        {
            var dw = new object();

            _sut.AppName = "";
            _sut.SelectedOrg = null;
            _sut.SelectedSpace = null;
            _sut.IsLoggedIn = false;

            Assert.IsFalse(_sut.CanDeployApp(null));
            Assert.IsFalse(_sut.DeploymentInProgress);

            _sut.DeployApp(dw);

            Assert.IsFalse(_sut.DeploymentInProgress);

            MockDialogService.Verify(mock => mock.CloseDialog(dw, true), Times.Never);
            MockThreadingService.Verify(mock => mock.StartTask(_sut.StartDeployment), Times.Never);
        }

        [TestMethod]
        [TestCategory("StartDeployment")]
        public async Task StartDeploymentTask_UpdatesDeploymentInProgress_WhenComplete()
        {
            _sut.AppName = _fakeAppName;
            _sut.SelectedSpace = _fakeSpace; // space must be set to faciliate lookup of parent org & grandparent cf

            AppManifest expectedManifest = _sut.ManifestModel;
            CloudFoundryInstance expectedCf = _sut.SelectedSpace.ParentOrg.ParentCf;
            CloudFoundryOrganization expectedOrg = _sut.SelectedSpace.ParentOrg;
            CloudFoundrySpace expectedSpace = _sut.SelectedSpace;
            StdOutDelegate expectedStdOutCallback = _sut.OutputViewModel.AppendLine;
            StdErrDelegate expectedStdErrCallback = _sut.OutputViewModel.AppendLine;

            MockCloudFoundryService.Setup(mock => mock.
              DeployAppAsync(expectedManifest, expectedCf, expectedOrg, expectedSpace, expectedStdOutCallback, expectedStdErrCallback))
                .ReturnsAsync(FakeSuccessDetailedResult);

            _sut.AppName = _fakeAppName;
            _sut.SelectedSpace = _fakeSpace;

            _sut.DeploymentInProgress = true;

            await _sut.StartDeployment();

            Assert.IsFalse(_sut.DeploymentInProgress);
        }

        [TestMethod]
        [TestCategory("StartDeployment")]
        public async Task StartDeploymentTask_LogsError_WhenDeployResultReportsFailure()
        {
            _sut.AppName = _fakeAppName;
            _sut.SelectedSpace = _fakeSpace; // space must be set to faciliate lookup of parent org & grandparent cf

            AppManifest expectedManifest = _sut.ManifestModel;
            CloudFoundryInstance expectedCf = _sut.SelectedSpace.ParentOrg.ParentCf;
            CloudFoundryOrganization expectedOrg = _sut.SelectedSpace.ParentOrg;
            CloudFoundrySpace expectedSpace = _sut.SelectedSpace;
            StdOutDelegate expectedStdOutCallback = _sut.OutputViewModel.AppendLine;
            StdErrDelegate expectedStdErrCallback = _sut.OutputViewModel.AppendLine;

            MockCloudFoundryService.Setup(mock => mock.
              DeployAppAsync(expectedManifest, expectedCf, expectedOrg, expectedSpace, expectedStdOutCallback, expectedStdErrCallback))
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
            _sut.SelectedSpace = _fakeSpace; // space must be set to faciliate lookup of parent org & grandparent cf

            AppManifest expectedManifest = _sut.ManifestModel;
            CloudFoundryInstance expectedCf = _sut.SelectedSpace.ParentOrg.ParentCf;
            CloudFoundryOrganization expectedOrg = _sut.SelectedSpace.ParentOrg;
            CloudFoundrySpace expectedSpace = _sut.SelectedSpace;
            StdOutDelegate expectedStdOutCallback = _sut.OutputViewModel.AppendLine;
            StdErrDelegate expectedStdErrCallback = _sut.OutputViewModel.AppendLine;

            string expectedErrorTitle = $"{DeploymentDialogViewModel.DeploymentErrorMsg} {_fakeAppName}.";
            string expectedErrorMsg = $"{FakeFailureDetailedResult.Explanation}";
            
            MockCloudFoundryService.Setup(mock => mock.
              DeployAppAsync(expectedManifest, expectedCf, expectedOrg, expectedSpace, expectedStdOutCallback, expectedStdErrCallback))
                .ReturnsAsync(FakeFailureDetailedResult);

            await _sut.StartDeployment();

            MockErrorDialogService.Verify(mock => mock.DisplayErrorDialog(expectedErrorTitle, expectedErrorMsg), Times.Once);
        }

        [TestMethod]
        [TestCategory("StartDeployment")]
        public async Task StartDeployment_SetsAuthRequiredToTrueOnTasExplorer_WhenFailureTypeIsInvalidRefreshToken()
        {
            var invalidRefreshTokenFailure = new DetailedResult(false, "junk error", FakeFailureCmdResult)
            {
                FailureType = FailureType.InvalidRefreshToken
            };
            
            _sut.SelectedSpace = _fakeSpace; // space must be set to faciliate lookup of parent org & grandparent cf

            AppManifest expectedManifest = _sut.ManifestModel;
            CloudFoundryInstance expectedCf = _sut.SelectedSpace.ParentOrg.ParentCf;
            CloudFoundryOrganization expectedOrg = _sut.SelectedSpace.ParentOrg;
            CloudFoundrySpace expectedSpace = _sut.SelectedSpace;
            StdOutDelegate expectedStdOutCallback = _sut.OutputViewModel.AppendLine;
            StdErrDelegate expectedStdErrCallback = _sut.OutputViewModel.AppendLine;

            MockCloudFoundryService.Setup(mock => mock.
              DeployAppAsync(expectedManifest, expectedCf, expectedOrg, expectedSpace, expectedStdOutCallback, expectedStdErrCallback))
                .ReturnsAsync(invalidRefreshTokenFailure);

            MockTasExplorerViewModel.SetupSet(m => m.AuthenticationRequired = true).Verifiable();

            await _sut.StartDeployment();

            MockTasExplorerViewModel.VerifyAll();
        }

        [TestMethod]
        [TestCategory("StartDeployment")]
        public async Task StartDeployment_TrimsRedundantInfoFromErrorMessage()
        {
            var specialRedundantErrorContent = "Instances starting...\n";
            var significantErrorInfo = "some junk error";
            var errorMsgWithRedundantInfo = string.Concat(Enumerable.Repeat(specialRedundantErrorContent, 8)) + significantErrorInfo;
            var redundantErrorInfoResult = new DetailedResult(false, errorMsgWithRedundantInfo, FakeFailureCmdResult);

            _sut.AppName = _fakeAppName;
            _sut.SelectedSpace = _fakeSpace; // space must be set to faciliate lookup of parent org & grandparent cf

            AppManifest expectedManifest = _sut.ManifestModel;
            CloudFoundryInstance expectedCf = _sut.SelectedSpace.ParentOrg.ParentCf;
            CloudFoundryOrganization expectedOrg = _sut.SelectedSpace.ParentOrg;
            CloudFoundrySpace expectedSpace = _sut.SelectedSpace;
            StdOutDelegate expectedStdOutCallback = _sut.OutputViewModel.AppendLine;
            StdErrDelegate expectedStdErrCallback = _sut.OutputViewModel.AppendLine;

            MockCloudFoundryService.Setup(mock => mock.
              DeployAppAsync(expectedManifest, expectedCf, expectedOrg, expectedSpace, expectedStdOutCallback, expectedStdErrCallback))
                .ReturnsAsync(redundantErrorInfoResult);

            await _sut.StartDeployment();

            MockErrorDialogService.Verify(m => m.
              DisplayErrorDialog(
                It.Is<string>(s => s.Contains(DeploymentDialogViewModel.DeploymentErrorMsg)),
                It.Is<string>(s => s.Contains(significantErrorInfo) && !s.Contains(specialRedundantErrorContent))),
                Times.Once);
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
        [TestCategory("OpenLoginView")]
        public void OpenLoginView_UpdatesBuildpackOptions_WhenTasConnectionGetsSet()
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
            MockThreadingService.Verify(m => m.StartTask(_sut.UpdateBuildpackOptions), Times.Once);
        }

        [TestMethod]
        [TestCategory("IsLoggedIn")]
        public void IsLoggedIn_ReturnsTrue_WhenTasConnectionIsNotNull()
        {
            MockTasExplorerViewModel.SetupGet(m => m.TasConnection).Returns(new FakeCfInstanceViewModel(FakeCfInstance, Services));

            _sut = new DeploymentDialogViewModel(Services, null, _fakeProjectPath, FakeTargetFrameworkMoniker);

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
            Assert.IsTrue(_sut.StackOptions.Contains("cflinuxfs3"));
        }

        [TestMethod]
        [TestCategory("StackOptions")]
        public void StackOptions_Returns_Windows_WhenTargetFrameworkIsNETFramework()
        {
            _sut = new DeploymentDialogViewModel(Services, null, _fakeProjectPath, targetFrameworkMoniker: DeploymentDialogViewModel.FullFrameworkTFM);

            Assert.IsTrue(_sut._fullFrameworkDeployment);

            Assert.AreEqual(1, _sut.StackOptions.Count);
            Assert.IsTrue(_sut.StackOptions.Contains("windows"));
        }

        [TestMethod]
        [TestCategory("ManifestPath")]
        public void ManifestPathSetter_SetsAppName_WhenManifestExistsAndContainsAppName()
        {
            var pathToFakeManifest = "some//fake//path";
            var expectedAppName1 = "app1";

            var fakeAppManifest = new AppManifest
            {
                Applications = new List<AppConfig>
                {
                    new AppConfig
                    {
                        Name = expectedAppName1,
                    }
                }
            };

            var fakeManifestParsingResponse = new DetailedResult<AppManifest>
            {
                Succeeded = true,
                Content = fakeAppManifest,
            };

            MockFileService.Setup(m => m.FileExists(pathToFakeManifest)).Returns(true);
            MockCloudFoundryService.Setup(m => m.ParseManifestFile(pathToFakeManifest)).Returns(fakeManifestParsingResponse);

            Assert.AreNotEqual(expectedAppName1, _sut.AppName);

            _sut.ManifestPath = pathToFakeManifest;

            Assert.AreEqual(expectedAppName1, _sut.AppName);
        }

        [TestMethod]
        [TestCategory("ManifestPath")]
        public void ManifestPathSetter_DisplaysError_AndDoesNotChangeAppName_WhenManifestDoesNotExist()
        {
            var pathToNonexistentManifest = "some//fake//path";
            MockFileService.Setup(m => m.FileExists(pathToNonexistentManifest)).Returns(false);

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
            var pathToFakeManifest = "some//fake//path";

            var fakeAppManifest = new AppManifest
            {
                Applications = new List<AppConfig>
                {
                    new AppConfig
                    {
                    }
                }
            };

            var fakeManifestParsingResponse = new DetailedResult<AppManifest>
            {
                Succeeded = true,
                Content = fakeAppManifest,
            };

            MockFileService.Setup(m => m.FileExists(pathToFakeManifest)).Returns(true);
            MockCloudFoundryService.Setup(m => m.ParseManifestFile(pathToFakeManifest)).Returns(fakeManifestParsingResponse);

            var initialAppName = _sut.AppName;

            _sut.ManifestPath = pathToFakeManifest;

            Assert.AreEqual(initialAppName, _sut.AppName);
        }

        [TestMethod]
        [TestCategory("ManifestPath")]
        [TestCategory("SelectedStack")]
        public void ManifestPathSetter_SetsSelectedStack_WhenManifestExistsAndContainsStack()
        {
            var pathToFakeManifest = "some//fake//path";
            var expectedAppName1 = "app1";
            var expectedFakeStackNameFromManifest = "windows";

            var fakeAppManifest = new AppManifest
            {
                Applications = new List<AppConfig>
                {
                    new AppConfig
                    {
                        Name = expectedAppName1,
                        Stack = expectedFakeStackNameFromManifest,
                    }
                }
            };

            var fakeManifestParsingResponse = new DetailedResult<AppManifest>
            {
                Succeeded = true,
                Content = fakeAppManifest,
            };

            MockFileService.Setup(m => m.FileExists(pathToFakeManifest)).Returns(true);
            MockCloudFoundryService.Setup(m => m.ParseManifestFile(pathToFakeManifest)).Returns(fakeManifestParsingResponse);

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
            var pathToFakeManifest = "some//fake//path";
            var invalidStackName = "asdf";

            var fakeAppManifest = new AppManifest
            {
                Applications = new List<AppConfig>
                {
                    new AppConfig
                    {
                        Name = "junk",
                        Stack = invalidStackName,
                    }
                }
            };

            var fakeManifestParsingResponse = new DetailedResult<AppManifest>
            {
                Succeeded = true,
                Content = fakeAppManifest,
            };

            MockFileService.Setup(m => m.FileExists(pathToFakeManifest)).Returns(true);
            MockCloudFoundryService.Setup(m => m.ParseManifestFile(pathToFakeManifest)).Returns(fakeManifestParsingResponse);

            var initialStack = _sut.SelectedStack;

            _sut.ManifestPath = pathToFakeManifest;

            Assert.AreEqual(initialStack, _sut.SelectedStack);
        }

        [TestMethod]
        [TestCategory("ManifestPath")]
        [TestCategory("SelectedBuildpacks")]
        public void ManifestPathSetter_SetsSelectedBuildpacks_WhenManifestExistsAndContainsOneBuildpack()
        {
            var pathToFakeManifest = "some//fake//path";
            var expectedAppName1 = "app1";
            var expectedBuildpackName1 = "my_cool_bp";
            var expectedSelectedBuildpacks = new ObservableCollection<string> { expectedBuildpackName1 };

            var fakeAppManifest = new AppManifest
            {
                Applications = new List<AppConfig>
                {
                    new AppConfig
                    {
                        Name = expectedAppName1,
                        Buildpacks = new List<string>
                        {
                            expectedBuildpackName1,
                        }
                    }
                }
            };

            var fakeManifestParsingResponse = new DetailedResult<AppManifest>
            {
                Succeeded = true,
                Content = fakeAppManifest,
            };

            MockFileService.Setup(m => m.FileExists(pathToFakeManifest)).Returns(true);
            MockCloudFoundryService.Setup(m => m.ParseManifestFile(pathToFakeManifest)).Returns(fakeManifestParsingResponse);

            CollectionAssert.AreNotEquivalent(expectedSelectedBuildpacks, _sut.SelectedBuildpacks);

            _sut.ManifestPath = pathToFakeManifest;

            CollectionAssert.AreEquivalent(expectedSelectedBuildpacks, _sut.SelectedBuildpacks);
        }

        [TestMethod]
        [TestCategory("ManifestPath")]
        [TestCategory("SelectedBuildpacks")]
        public void ManifestPathSetter_SetsSelectedBuildpacks_WhenManifestExistsAndContainsMoreThanOneBuildpack()
        {
            var pathToFakeManifest = "some//fake//path";
            var expectedAppName1 = "app1";
            var expectedBuildpackName1 = "my_cool_bp";
            var expectedBuildpackName2 = "another";
            var expectedBuildpackName3 = "junk";
            var expectedSelectedBuildpacks = new ObservableCollection<string> { expectedBuildpackName1, expectedBuildpackName2, expectedBuildpackName3 };

            var fakeAppManifest = new AppManifest
            {
                Applications = new List<AppConfig>
                {
                    new AppConfig
                    {
                        Name = expectedAppName1,
                        Buildpacks = new List<string>
                        {
                            expectedBuildpackName1,
                            expectedBuildpackName2,
                            expectedBuildpackName3,
                        }
                    }
                }
            };

            var fakeManifestParsingResponse = new DetailedResult<AppManifest>
            {
                Succeeded = true,
                Content = fakeAppManifest,
            };

            MockFileService.Setup(m => m.FileExists(pathToFakeManifest)).Returns(true);
            MockCloudFoundryService.Setup(m => m.ParseManifestFile(pathToFakeManifest)).Returns(fakeManifestParsingResponse);

            CollectionAssert.AreNotEquivalent(expectedSelectedBuildpacks, _sut.SelectedBuildpacks);

            _sut.ManifestPath = pathToFakeManifest;

            CollectionAssert.AreEquivalent(expectedSelectedBuildpacks, _sut.SelectedBuildpacks);
        }

        [TestMethod]
        [TestCategory("ManifestPath")]
        [TestCategory("SelectedBuildpacks")]
        public void ManifestPathSetter_DoesNotChangeSelectedBuildpacks_AndDisplaysError_WhenManifestDoesNotExist()
        {
            var pathToNonexistentManifest = "bogus//path";
            var initialBuildpack = _sut.SelectedBuildpacks;

            MockFileService.Setup(m => m.FileExists(pathToNonexistentManifest)).Returns(false);

            _sut.ManifestPath = pathToNonexistentManifest;

            Assert.AreEqual(initialBuildpack, _sut.SelectedBuildpacks);
            MockErrorDialogService.Verify(m => m.DisplayErrorDialog(DeploymentDialogViewModel.ManifestNotFoundTitle, It.Is<string>(s => s.Contains(pathToNonexistentManifest) && s.Contains("does not appear to be a valid path to a manifest"))), Times.Once);
        }

        [TestMethod]
        [TestCategory("ManifestPath")]
        [TestCategory("SelectedBuildpacks")]
        public void ManifestPathSetter_DoesNotChangeSelectedBuildpacks_AndDisplaysError_WhenManifestParsingFails()
        {
            var pathToManifest = "bogus//path";
            var initialBuildpack = _sut.SelectedBuildpacks;
            var fakeParsingFailureMsg = "junk";
            var fakeParsingFailureResponse = new DetailedResult<AppManifest>
            {
                Succeeded = false,
                Explanation = fakeParsingFailureMsg,
            };

            MockFileService.Setup(m => m.FileExists(pathToManifest)).Returns(true);
            MockCloudFoundryService.Setup(m => m.ParseManifestFile(pathToManifest)).Returns(fakeParsingFailureResponse);

            _sut.ManifestPath = pathToManifest;

            Assert.AreEqual(initialBuildpack, _sut.SelectedBuildpacks);
            MockErrorDialogService.Verify(m => m.DisplayErrorDialog(DeploymentDialogViewModel.ManifestParsingErrorTitle, fakeParsingFailureMsg), Times.Once);
        }

        [TestMethod]
        [TestCategory("SelectedStack")]
        public void SelectedStackSetter_SetsValue_WhenValueExistsInStackOptions()
        {
            Assert.Fail("TODO: add tests that ensure the ManifestModel gets updated when 'selectedXYZ' properties change");

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
        [TestCategory("DeploymentDirectoryPath")]
        public void DirectoryPathSetter_SetsDirectoryPathLabel_WhenDirectoryExistsAtGivenPath()
        {
            _sut.DeploymentDirectoryPath = "junk//path";
            var initialDirectoryPathLabel = _sut.DirectoryPathLabel;

            Assert.IsNull(_sut.DeploymentDirectoryPath);
            Assert.AreEqual("<none specified>", _sut.DirectoryPathLabel);

            _sut.DeploymentDirectoryPath = _fakeProjectPath;

            Assert.IsNotNull(_sut.DeploymentDirectoryPath);
            Assert.AreNotEqual(initialDirectoryPathLabel, _sut.DirectoryPathLabel);
            Assert.AreEqual(_fakeProjectPath, _sut.DirectoryPathLabel);
            Assert.AreEqual(_fakeProjectPath, _sut.DeploymentDirectoryPath);
        }

        [TestMethod]
        [TestCategory("DeploymentDirectoryPath")]
        public void DirectoryPathSetter_DisplaysError_AndSetsDirectoryPathLabelToNoneSpecified_WhenNoDirectoryExistsAtGivenPath()
        {
            var fakePath = "asdf//junk";
            Assert.IsFalse(Directory.Exists(fakePath));

            _sut.DirectoryPathLabel = "fake initial value";

            Assert.AreNotEqual("<none specified>", _sut.DirectoryPathLabel);

            _sut.DeploymentDirectoryPath = fakePath;

            Assert.AreEqual("<none specified>", _sut.DirectoryPathLabel);

            MockErrorDialogService.Verify(
                m => m.DisplayErrorDialog(DeploymentDialogViewModel.DirectoryNotFoundTitle, It.Is<string>(s => s.Contains(fakePath) && s.Contains("does not appear to be a valid path"))),
                Times.Once);
        }

        [TestMethod]
        [TestCategory("BinaryDeployment")]
        [TestCategory("DeploymentButtonLabel")]
        public void BinaryDeployment_SetsDeploymentButtonLabelToPushSource_WhenSetToFalse()
        {
            _sut.DeploymentButtonLabel = "fake initial value";

            _sut.BinaryDeployment = false;

            Assert.AreEqual("Push app (from source)", _sut.DeploymentButtonLabel);
        }

        [TestMethod]
        [TestCategory("BinaryDeployment")]
        [TestCategory("DeploymentButtonLabel")]
        public void BinaryDeployment_SetsDeploymentButtonLabelToPushBinaries_WhenSetToTrue()
        {
            _sut.DeploymentButtonLabel = "fake initial value";

            _sut.BinaryDeployment = true;

            Assert.AreEqual("Push app (from binaries)", _sut.DeploymentButtonLabel);
        }

        [TestMethod]
        [TestCategory("ToggleAdvancedOptions")]
        [DataRow(true, false)]
        [DataRow(false, true)]
        public void ToggleAdvancedOptions_InvertsValueOfExpandedProp(bool initialExpandedVal, bool expectedExpandedVal)
        {
            _sut.Expanded = initialExpandedVal;

            _sut.ToggleAdvancedOptions();

            Assert.AreEqual(expectedExpandedVal, _sut.Expanded);
        }

        [TestMethod]
        [TestCategory("Expanded")]
        [TestCategory("ExpansionButtonText")]
        [DataRow(true, "Hide Options")]
        [DataRow(false, "More Options")]
        public void SettingExpandedProp_ChangesExpansionButtonText(bool expandedVal, string expectedButtonText)
        {
            _sut.Expanded = expandedVal;

            Assert.AreEqual(_sut.ExpansionButtonText, expectedButtonText);
            Assert.AreEqual(2, _receivedEvents.Count);
            Assert.IsTrue(_receivedEvents.Contains("Expanded"));
            Assert.IsTrue(_receivedEvents.Contains("ExpansionButtonText"));
        }

        [TestMethod]
        [TestCategory("UpdateBuildpackOptions")]
        public async Task UpdateBuildpackOptions_SetsBuildpackOptionsToEmptyList_WhenNotLoggedIn()
        {
            Assert.IsNull(_sut.TasExplorerViewModel.TasConnection);

            CollectionAssert.DoesNotContain(_receivedEvents, "BuildpackOptions");

            await _sut.UpdateBuildpackOptions();

            CollectionAssert.AreEqual(new List<string>(), _sut.BuildpackOptions);
            CollectionAssert.Contains(_receivedEvents, "BuildpackOptions");
        }

        [TestMethod]
        [TestCategory("UpdateBuildpackOptions")]
        public async Task UpdateBuildpackOptions_SetsBuildpackOptionsToQueryContent_WhenQuerySucceeds()
        {
            var fakeCf = new FakeCfInstanceViewModel(FakeCfInstance, Services);
            var fakeBuildpacksContent = new List<string>
            {
                "my_cool_bp",
                "ruby_buildpack",
                "topaz_buildpack",
                "emerald_buildpack",
            };
            var fakeBuildpacksResponse = new DetailedResult<List<string>>(succeeded: true, content: fakeBuildpacksContent);

            MockTasExplorerViewModel.SetupGet(m => m.TasConnection).Returns(fakeCf);

            MockCloudFoundryService.Setup(m => m.GetUniqueBuildpackNamesAsync(fakeCf.CloudFoundryInstance.ApiAddress, 1)).ReturnsAsync(fakeBuildpacksResponse);

            Assert.AreNotEqual(fakeBuildpacksContent, _sut.BuildpackOptions);
            CollectionAssert.DoesNotContain(_receivedEvents, "BuildpackOptions");

            await _sut.UpdateBuildpackOptions();

            Assert.AreEqual(fakeBuildpacksContent, _sut.BuildpackOptions);
            CollectionAssert.Contains(_receivedEvents, "BuildpackOptions");
        }

        [TestMethod]
        [TestCategory("UpdateBuildpackOptions")]
        public async Task UpdateBuildpackOptions_SetsBuildpackOptionsToEmptyList_AndRaisesError_WhenQueryFails()
        {
            var fakeCf = new FakeCfInstanceViewModel(FakeCfInstance, Services);
            const string fakeFailureReason = "junk";
            var fakeBuildpacksResponse = new DetailedResult<List<string>>(succeeded: false, content: null, explanation: fakeFailureReason, cmdDetails: null);

            MockTasExplorerViewModel.SetupGet(m => m.TasConnection).Returns(fakeCf);

            MockCloudFoundryService.Setup(m => m.GetUniqueBuildpackNamesAsync(fakeCf.CloudFoundryInstance.ApiAddress, 1)).ReturnsAsync(fakeBuildpacksResponse);

            CollectionAssert.DoesNotContain(_receivedEvents, "BuildpackOptions");

            await _sut.UpdateBuildpackOptions();

            CollectionAssert.AreEquivalent(new List<string>(), _sut.BuildpackOptions);
        }

        [TestMethod]
        [TestCategory("AddToSelectedBuildpacks")]
        public void AddToSelectedBuildpacks_AddsToSelectedBuildpacks_AndRaisesPropChangedEvent_WhenArgIsString()
        {
            string item = "new entry";
            List<string> initialSelectedBps = _sut.SelectedBuildpacks.ToList();

            CollectionAssert.DoesNotContain(initialSelectedBps, item);
            CollectionAssert.DoesNotContain(_receivedEvents, "SelectedBuildpacks");

            _sut.AddToSelectedBuildpacks(item);

            var updatedSelectedBps = _sut.SelectedBuildpacks;

            CollectionAssert.AreNotEquivalent(initialSelectedBps, updatedSelectedBps);
            Assert.AreEqual(initialSelectedBps.Count + 1, updatedSelectedBps.Count);
            CollectionAssert.Contains(updatedSelectedBps, item);

            CollectionAssert.Contains(_receivedEvents, "SelectedBuildpacks");
        }

        [TestMethod]
        [TestCategory("AddToSelectedBuildpacks")]
        public void AddToSelectedBuildpacks_DoesNothing_WhenArgIsNotString()
        {
            object nonStringItem = new object();
            List<string> initialSelectedBps = _sut.SelectedBuildpacks.ToList();

            CollectionAssert.DoesNotContain(initialSelectedBps, nonStringItem);
            CollectionAssert.DoesNotContain(_receivedEvents, "SelectedBuildpacks");

            _sut.AddToSelectedBuildpacks(nonStringItem);

            var updatedSelectedBps = _sut.SelectedBuildpacks;

            CollectionAssert.AreEquivalent(initialSelectedBps, updatedSelectedBps);
            CollectionAssert.DoesNotContain(_receivedEvents, "SelectedBuildpacks");
        }

        [TestMethod]
        [TestCategory("AddToSelectedBuildpacks")]
        public void AddToSelectedBuildpacks_DoesNothing_WhenStringAlreadyExistsInSelectedBuildpacks()
        {
            var item = "junk";
            _sut.SelectedBuildpacks = new ObservableCollection<string> { item, "extra", "stuff" };
            List<string> initialSelectedBps = _sut.SelectedBuildpacks.ToList();

            _receivedEvents.Clear();

            CollectionAssert.Contains(initialSelectedBps, item);
            CollectionAssert.DoesNotContain(_receivedEvents, "SelectedBuildpacks");

            _sut.AddToSelectedBuildpacks(item);

            var updatedSelectedBps = _sut.SelectedBuildpacks;

            CollectionAssert.AreEquivalent(initialSelectedBps, updatedSelectedBps);
            CollectionAssert.DoesNotContain(_receivedEvents, "SelectedBuildpacks");
        }

        [TestMethod]
        [TestCategory("RemoveFromSelectedBuildpacks")]
        public void RemoveFromSelectedBuildpacks_RemovesFromSelectedBuildpacks_AndRaisesPropChangedEvent_WhenArgIsString()
        {
            string item = "existing entry";

            _sut.SelectedBuildpacks = new System.Collections.ObjectModel.ObservableCollection<string>
            {
                item
            };

            List<string> initialSelectedBps = _sut.SelectedBuildpacks.ToList();
            CollectionAssert.Contains(initialSelectedBps, item);

            _receivedEvents.Clear();
            CollectionAssert.DoesNotContain(_receivedEvents, "SelectedBuildpacks");

            _sut.RemoveFromSelectedBuildpacks(item);

            var updatedSelectedBps = _sut.SelectedBuildpacks;

            CollectionAssert.AreNotEquivalent(initialSelectedBps, updatedSelectedBps);
            Assert.AreEqual(initialSelectedBps.Count - 1, updatedSelectedBps.Count);
            CollectionAssert.DoesNotContain(updatedSelectedBps, item);

            CollectionAssert.Contains(_receivedEvents, "SelectedBuildpacks");
        }

        [TestMethod]
        [TestCategory("RemoveFromSelectedBuildpacks")]
        public void RemoveFromSelectedBuildpacks_DoesNothing_WhenArgIsNotString()
        {
            object nonStringItem = new object();

            List<string> initialSelectedBps = _sut.SelectedBuildpacks.ToList();

            _sut.RemoveFromSelectedBuildpacks(nonStringItem);

            var updatedSelectedBps = _sut.SelectedBuildpacks;

            CollectionAssert.AreEquivalent(initialSelectedBps, updatedSelectedBps);
            CollectionAssert.DoesNotContain(_receivedEvents, "SelectedBuildpacks");
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