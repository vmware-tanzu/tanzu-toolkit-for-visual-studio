﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Tanzu.Toolkit.Models;
using Tanzu.Toolkit.Services;

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
                mock.GetViewByViewModelName(nameof(OutputViewModel), null))
                    .Returns(new FakeOutputView());

            MockFileService.Setup(m => m.FileExists(_fakeManifestPath)).Returns(true);
            MockFileService.Setup(m => m.DirectoryExists(_fakeProjectPath)).Returns(true);
            MockFileService.Setup(m => m.DirContainsFiles(_fakeProjectPath)).Returns(true);

            _fakeSelectedBuildpacks = new ObservableCollection<string> { _fakeBuildpackName1, _fakeBuildpackName2, _fakeBuildpackName3 };

            _sut = new DeploymentDialogViewModel(Services, _fakeProjName, _fakeProjectPath, FakeTargetFrameworkMoniker)
            {
                ManifestModel = _fakeManifestModel,
                SelectedBuildpacks = _fakeSelectedBuildpacks,
            };

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
            MockSerializationService.VerifyAll();
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
        [TestCategory("BuildpackOptions")]
        public void Constructor_SetsBuildpackOptionsToEmptyList()
        {
            CollectionAssert.AreEqual(new List<BuildpackListItem>(), _sut.BuildpackOptions);
        }

        [TestMethod]
        [TestCategory("ctor")]
        [TestCategory("StackOptions")]
        public void Constructor_SetsStackOptionsToEmptyList()
        {
            CollectionAssert.AreEqual(new List<string>(), _sut.StackOptions);
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
        [TestCategory("BuildpackOptions")]
        public void Constructor_UpdatesBuildpackOptions_WhenTasConnectionIsNotNull()
        {
            MockTasExplorerViewModel.SetupGet(m => m.TasConnection).Returns(new FakeCfInstanceViewModel(FakeCfInstance, Services));

            _sut = new DeploymentDialogViewModel(Services, null, _fakeProjectPath, FakeTargetFrameworkMoniker);

            Assert.IsNotNull(_sut.TasExplorerViewModel.TasConnection);
            MockThreadingService.Verify(m => m.StartTask(_sut.UpdateBuildpackOptions), Times.Once);
        }

        [TestMethod]
        [TestCategory("ctor")]
        [TestCategory("StackOptions")]
        public void Constructor_UpdatesStackOptions_WhenTasConnectionIsNotNull()
        {
            MockTasExplorerViewModel.SetupGet(m => m.TasConnection).Returns(new FakeCfInstanceViewModel(FakeCfInstance, Services));

            _sut = new DeploymentDialogViewModel(Services, null, _fakeProjectPath, FakeTargetFrameworkMoniker);

            Assert.IsNotNull(_sut.TasExplorerViewModel.TasConnection);
            MockThreadingService.Verify(m => m.StartTask(_sut.UpdateStackOptions), Times.Once);
        }

        [TestMethod]
        [TestCategory("ctor")]
        [TestCategory("ManifestModel")]
        public void Constructor_SetsManifestModel_ToNewAppManifest_WhenNoDefaultManifestExistsAtAnExpectedPath()
        {
            // ensure no "default" manifest is picked up when sut is constructed
            MockFileService.Setup(m => m.FileExists(It.Is<string>(s => s.Contains("manifest.yaml")))).Returns(false);
            MockFileService.Setup(m => m.FileExists(It.Is<string>(s => s.Contains("manifest.yml")))).Returns(false);

            _sut = new DeploymentDialogViewModel(Services, _fakeProjName, _fakeProjectPath, FakeTargetFrameworkMoniker);

            Assert.IsNotNull(_sut.ManifestModel);
            Assert.IsNotNull(_sut.ManifestModel.Applications[0]);

            var manifestModelApp = _sut.ManifestModel.Applications[0];
            Assert.IsNotNull(manifestModelApp.Name);
            Assert.IsNotNull(manifestModelApp.Buildpacks);
            Assert.AreEqual(_fakeProjName, manifestModelApp.Name);
            CollectionAssert.AreEquivalent(new List<string>(), manifestModelApp.Buildpacks);
        }

        [TestMethod]
        [TestCategory("ctor")]
        [TestCategory("DeploymentDirectoryPath")]
        [TestCategory("DeploymentDirectoryPathLabel")]
        public void Constructor_SetsDeploymentDirectoryPathToNull_AndSetsDirectoryPathLabelToDefaultAppDirectory_WhenPathNotSpecifiedByManifest()
        {
            _sut = new DeploymentDialogViewModel(Services, _fakeProjName, _fakeProjectPath, FakeTargetFrameworkMoniker);

            Assert.IsNull(_sut.ManifestModel.Applications[0].Path); // ensure path not specified

            Assert.IsNull(_sut.DeploymentDirectoryPath);
            Assert.AreEqual("<Default App Directory>", _sut.DirectoryPathLabel);

            MockErrorDialogService.Verify(m => m.DisplayErrorDialog(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        [TestCategory("ctor")]
        [TestCategory("DeploymentDirectoryPath")]
        [TestCategory("DeploymentDirectoryPathLabel")]
        public void Constructor_SetsDeploymentDirectoryPathAndDirectoryPathLabel_ToPathValue_WhenManifestSpecifiesPath()
        {
            var fakeAppPath = _fakeProjectPath;
            var expectedDefaultManifestPath = Path.Combine(_fakeProjectPath, "manifest.yml");
            var expectedPathValue = _fakeManifestModel.Applications[0].Path;
            var fakeManifestContent = "some yaml";

            MockFileService.Setup(m => m.DirectoryExists(expectedPathValue)).Returns(true);

            // simulate that the manifest path value came from a default "manifest.yml" file
            MockFileService.Setup(m => m.FileExists(expectedDefaultManifestPath)).Returns(true);

            MockFileService.Setup(m => m.ReadFileContents(expectedDefaultManifestPath)).Returns(fakeManifestContent);
            MockSerializationService.Setup(m => m.ParseCfAppManifest(fakeManifestContent)).Returns(_fakeManifestModel);

            _sut = new DeploymentDialogViewModel(Services, _fakeProjName, fakeAppPath, FakeTargetFrameworkMoniker);

            Assert.IsNotNull(_sut.ManifestModel.Applications[0].Path); // ensure path value specified
            Assert.AreEqual(expectedPathValue, _sut.ManifestModel.Applications[0].Path);

            Assert.AreEqual(expectedPathValue, _sut.DeploymentDirectoryPath);
            Assert.AreEqual(expectedPathValue, _sut.DirectoryPathLabel);
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
            string expectedDefaultAppPath = _sut.PathToProjectRootDir;
            CloudFoundryInstance expectedCf = _sut.SelectedSpace.ParentOrg.ParentCf;
            CloudFoundryOrganization expectedOrg = _sut.SelectedSpace.ParentOrg;
            CloudFoundrySpace expectedSpace = _sut.SelectedSpace;
            Action<string> expectedStdOutCallback = _sut.OutputViewModel.AppendLine;
            Action<string> expectedStdErrCallback = _sut.OutputViewModel.AppendLine;

            MockCloudFoundryService.Setup(mock => mock.
              DeployAppAsync(expectedManifest, expectedDefaultAppPath, expectedCf, expectedOrg, expectedSpace, expectedStdOutCallback, expectedStdErrCallback))
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
            string expectedDefaultAppPath = _sut.PathToProjectRootDir;
            CloudFoundryInstance expectedCf = _sut.SelectedSpace.ParentOrg.ParentCf;
            CloudFoundryOrganization expectedOrg = _sut.SelectedSpace.ParentOrg;
            CloudFoundrySpace expectedSpace = _sut.SelectedSpace;
            Action<string> expectedStdOutCallback = _sut.OutputViewModel.AppendLine;
            Action<string> expectedStdErrCallback = _sut.OutputViewModel.AppendLine;

            MockCloudFoundryService.Setup(mock => mock.
              DeployAppAsync(expectedManifest, expectedDefaultAppPath, expectedCf, expectedOrg, expectedSpace, expectedStdOutCallback, expectedStdErrCallback))
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
            string expectedDefaultAppPath = _sut.PathToProjectRootDir;
            CloudFoundryInstance expectedCf = _sut.SelectedSpace.ParentOrg.ParentCf;
            CloudFoundryOrganization expectedOrg = _sut.SelectedSpace.ParentOrg;
            CloudFoundrySpace expectedSpace = _sut.SelectedSpace;
            Action<string> expectedStdOutCallback = _sut.OutputViewModel.AppendLine;
            Action<string> expectedStdErrCallback = _sut.OutputViewModel.AppendLine;

            string expectedErrorTitle = $"{DeploymentDialogViewModel.DeploymentErrorMsg} {_fakeAppName}.";
            string expectedErrorMsg = $"{FakeFailureDetailedResult.Explanation}";

            MockCloudFoundryService.Setup(mock => mock.
              DeployAppAsync(expectedManifest, expectedDefaultAppPath, expectedCf, expectedOrg, expectedSpace, expectedStdOutCallback, expectedStdErrCallback))
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
            string expectedDefaultAppPath = _sut.PathToProjectRootDir;
            CloudFoundryInstance expectedCf = _sut.SelectedSpace.ParentOrg.ParentCf;
            CloudFoundryOrganization expectedOrg = _sut.SelectedSpace.ParentOrg;
            CloudFoundrySpace expectedSpace = _sut.SelectedSpace;
            Action<string> expectedStdOutCallback = _sut.OutputViewModel.AppendLine;
            Action<string> expectedStdErrCallback = _sut.OutputViewModel.AppendLine;

            MockCloudFoundryService.Setup(mock => mock.
              DeployAppAsync(expectedManifest, expectedDefaultAppPath, expectedCf, expectedOrg, expectedSpace, expectedStdOutCallback, expectedStdErrCallback))
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
            string expectedDefaultAppPath = _sut.PathToProjectRootDir;
            CloudFoundryInstance expectedCf = _sut.SelectedSpace.ParentOrg.ParentCf;
            CloudFoundryOrganization expectedOrg = _sut.SelectedSpace.ParentOrg;
            CloudFoundrySpace expectedSpace = _sut.SelectedSpace;
            Action<string> expectedStdOutCallback = _sut.OutputViewModel.AppendLine;
            Action<string> expectedStdErrCallback = _sut.OutputViewModel.AppendLine;

            MockCloudFoundryService.Setup(mock => mock.
              DeployAppAsync(expectedManifest, expectedDefaultAppPath, expectedCf, expectedOrg, expectedSpace, expectedStdOutCallback, expectedStdErrCallback))
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
        [TestCategory("BuildpackOptions")]
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
        [TestCategory("OpenLoginView")]
        [TestCategory("StackOptions")]
        public void OpenLoginView_UpdatesStackOptions_WhenTasConnectionGetsSet()
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
            MockThreadingService.Verify(m => m.StartTask(_sut.UpdateStackOptions), Times.Once);
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
        [TestCategory("ManifestPath")]
        public void ManifestPathSetter_SetsAppName_WhenManifestExistsAndContainsAppName()
        {
            var pathToFakeManifest = "some//fake//path";
            var fakeManifestContent = "some yaml";
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
            MockFileService.Setup(m => m.ReadFileContents(pathToFakeManifest)).Returns(fakeManifestContent);
            MockSerializationService.Setup(m => m.ParseCfAppManifest(fakeManifestContent)).Returns(fakeAppManifest);

            Assert.AreNotEqual(expectedAppName1, _sut.AppName);

            _sut.ManifestPath = pathToFakeManifest;

            Assert.AreEqual(expectedAppName1, _sut.AppName);
        }

        [TestMethod]
        [TestCategory("ManifestPath")]
        public void ManifestPathSetter_SetsStartCommand_WhenManifestExistsAndContainsStartCommand()
        {
            var pathToFakeManifest = "some//fake//path";
            var fakeManifestContent = "some yaml";
            var expectedStartCommand = "my expected command";

            var fakeAppManifest = new AppManifest
            {
                Applications = new List<AppConfig>
                {
                    new AppConfig
                    {
                        Command = expectedStartCommand,
                    }
                }
            };

            MockFileService.Setup(m => m.FileExists(pathToFakeManifest)).Returns(true);
            MockFileService.Setup(m => m.ReadFileContents(pathToFakeManifest)).Returns(fakeManifestContent);
            MockSerializationService.Setup(m => m.ParseCfAppManifest(fakeManifestContent)).Returns(fakeAppManifest);

            Assert.AreNotEqual(expectedStartCommand, _sut.StartCommand);

            _sut.ManifestPath = pathToFakeManifest;

            Assert.AreEqual(expectedStartCommand, _sut.StartCommand);
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
            var fakeManifestContent = "some yaml";

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
            MockFileService.Setup(m => m.ReadFileContents(pathToFakeManifest)).Returns(fakeManifestContent);
            MockSerializationService.Setup(m => m.ParseCfAppManifest(fakeManifestContent)).Returns(fakeAppManifest);

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
            var fakeManifestContent = "some yaml";
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
            MockFileService.Setup(m => m.ReadFileContents(pathToFakeManifest)).Returns(fakeManifestContent);
            MockSerializationService.Setup(m => m.ParseCfAppManifest(fakeManifestContent)).Returns(fakeAppManifest);

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
        [TestCategory("SelectedBuildpacks")]
        public void ManifestPathSetter_SetsSelectedBuildpacks_WhenManifestExistsAndContainsOneBuildpack()
        {
            var pathToFakeManifest = "some//fake//path";
            var fakeManifestContent = "some yaml";
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
            MockFileService.Setup(m => m.ReadFileContents(pathToFakeManifest)).Returns(fakeManifestContent);
            MockSerializationService.Setup(m => m.ParseCfAppManifest(fakeManifestContent)).Returns(fakeAppManifest);

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
            var fakeManifestContent = "some yaml";
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
            MockFileService.Setup(m => m.ReadFileContents(pathToFakeManifest)).Returns(fakeManifestContent);
            MockSerializationService.Setup(m => m.ParseCfAppManifest(fakeManifestContent)).Returns(fakeAppManifest);

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
            var pathToFakeManifest = "bogus//path";
            var fakeManifestContent = "some yaml";
            var fakeExceptionMsg = "parser didn't get out of bed this morning";
            var fakeParsingException = new Exception(fakeExceptionMsg);

            var initialBuildpack = _sut.SelectedBuildpacks;

            MockFileService.Setup(m => m.FileExists(pathToFakeManifest)).Returns(true);
            MockFileService.Setup(m => m.ReadFileContents(pathToFakeManifest)).Returns(fakeManifestContent);
            MockSerializationService.Setup(m => m.ParseCfAppManifest(fakeManifestContent)).Throws(fakeParsingException);

            _sut.ManifestPath = pathToFakeManifest;

            Assert.AreEqual(initialBuildpack, _sut.SelectedBuildpacks);
            MockErrorDialogService.Verify(m => m.DisplayErrorDialog(DeploymentDialogViewModel.ManifestParsingErrorTitle, fakeExceptionMsg), Times.Once);
        }

        [TestMethod]
        [TestCategory("ManifestPath")]
        [TestCategory("DeploymentDirectoryPath")]
        [TestCategory("DeploymentDirectoryPathLabel")]
        public void ManifestPathSetter_SetsDeploymentDirectoryPathToNull_AndSetsDirectoryPathLabelToDefaultAppDirectory_WhenPathNotSpecifiedByManifest()
        {
            var newManifestPath = _fakeManifestPath;
            var fakeManifestContent = "some yaml";

            var manifestThatDoesNotSpecifyPath = new AppManifest
            {
                Applications = new List<AppConfig>
                {
                    new AppConfig
                    {
                        Path = null,
                    }
                }
            };

            MockFileService.Setup(m => m.ReadFileContents(newManifestPath)).Returns(fakeManifestContent);
            MockSerializationService.Setup(m => m.ParseCfAppManifest(fakeManifestContent)).Returns(manifestThatDoesNotSpecifyPath);

            _sut.ManifestPath = newManifestPath;

            Assert.IsNull(_sut.ManifestModel.Applications[0].Path);

            Assert.IsNull(_sut.DeploymentDirectoryPath);
            Assert.AreEqual("<Default App Directory>", _sut.DirectoryPathLabel);
        }

        [TestMethod]
        [TestCategory("ManifestPath")]
        [TestCategory("DeploymentDirectoryPath")]
        [TestCategory("DeploymentDirectoryPathLabel")]
        public void ManifestPathSetter_SetsDeploymentDirectoryPathAndDirectoryPathLabel_ToPathValue_WhenManifestSpecifiesPath()
        {
            var newManifestPath = _fakeManifestPath;
            var pathValInNewManifest = "some\\path\\to\\an\\app\\dir";
            var fakeManifestContent = "some yaml";

            var manifestThatSpecifiesAppDirPath = new AppManifest
            {
                Applications = new List<AppConfig>
                {
                    new AppConfig
                    {
                        Path = pathValInNewManifest,
                    }
                }
            };

            MockFileService.Setup(m => m.ReadFileContents(newManifestPath)).Returns(fakeManifestContent);

            MockSerializationService.Setup(m => m.ParseCfAppManifest(fakeManifestContent)).Returns(manifestThatSpecifiesAppDirPath);

            MockFileService.Setup(m => m.DirectoryExists(pathValInNewManifest)).Returns(true);

            _sut.ManifestPath = newManifestPath;

            Assert.IsNotNull(_sut.ManifestModel.Applications[0].Path);
            Assert.AreEqual(pathValInNewManifest, _sut.ManifestModel.Applications[0].Path);

            Assert.IsNotNull(_sut.DeploymentDirectoryPath);
            Assert.AreEqual(pathValInNewManifest, _sut.DeploymentDirectoryPath);
            
            Assert.IsNotNull(_sut.DirectoryPathLabel);
            Assert.AreEqual(pathValInNewManifest, _sut.DirectoryPathLabel);
        }

        [TestMethod]
        [TestCategory("SelectedStack")]
        [TestCategory("ManifestModel")]
        public void SelectedStackSetter_SetsValueInManifestModel()
        {
            var stackVal = "windows";
            var initialStackInManifestModel = _sut.ManifestModel.Applications[0].Stack;

            Assert.AreNotEqual(stackVal, initialStackInManifestModel);

            _sut.SelectedStack = stackVal;

            Assert.AreEqual(stackVal, _sut.SelectedStack);
            Assert.AreEqual(stackVal, _sut.ManifestModel.Applications[0].Stack);
            Assert.AreEqual(1, _receivedEvents.Count);
            Assert.AreEqual("SelectedStack", _receivedEvents[0]);
        }

        [TestMethod]
        [TestCategory("SelectedStack")]
        [TestCategory("SelectedBuildpacks")]
        public void SelectedStackSetter_EvaluatesStackCompatibilityWithBuildpackOptions_AndDeselectsIncompatibleBuildpacks()
        {
            var newStackVal = "windows";
            var initialStackInManifestModel = _sut.ManifestModel.Applications[0].Stack;

            /** Matrix: all possible buildpack selection / stack compatibility changes when changing selectedStack value
             * 	oldComp	oldSelected	newComp	newSelected
             * 	0	    0		    0	    0	    incompatibleDeselectedBpToStayIncompatibleAndDeselected
             * 	0	    0		    0	    1	    N/A - no spontaneous selection
             * 	0	    0		    1	    0	    incompatibleDeselectedBpToBecomeCompatibleAndStayDeselected
             * 	0	    0		    1	    1	    N/A - no spontaneous selection
             * 	0	    1		    0	    0	    N/A - can't start compatible and selected
             * 	0	    1		    0	    1	    N/A - can't start compatible and selected
             * 	0	    1		    1	    0	    N/A - can't start compatible and selected
             * 	0	    1		    1	    1	    N/A - can't start compatible and selected
             * 	1	    0		    0	    0	    compatibleDeselectedBpToBecomeIncompatibleAndStayDeselected
             * 	1	    0		    0	    1	    N/A - no spontaneous selection
             * 	1	    0		    1	    0	    compatibleDeselectedBpToStayCompatibleAndDeselected
             * 	1	    0		    1	    1	    N/A - no spontaneous selection
             * 	1	    1		    0	    0	    compatibleSelectedBpToBecomeIncompatibleAndDeselected
             * 	1	    1		    0	    1	    N/A - can't be selected if not compatible
             * 	1	    1		    1	    0   	N/A - should say selected if compatible with both new & old stacks
             * 	1	    1		    1	    1	    compatibleSelectedBpToStayCompatibleAndSelected 
             */

            var incompatibleDeselectedBpToStayIncompatibleAndDeselected = new FakeBuildpackListItem(
                "incompatibleDeselectedBpToStayIncompatibleAndDeselected",
                selected: false,
                compatibleWithCurrentStack: false,
                stacks: new List<string> { "unusedStack" });

            var incompatibleDeselectedBpToBecomeCompatibleAndStayDeselected = new FakeBuildpackListItem(
                "incompatibleDeselectedBpToBecomeCompatibleAndStayDeselected",
                selected: false,
                compatibleWithCurrentStack: false,
                stacks: new List<string> { newStackVal });

            var compatibleDeselectedBpToBecomeIncompatibleAndStayDeselected = new FakeBuildpackListItem(
                "compatibleDeselectedBpToBecomeIncompatibleAndStayDeselected",
                selected: false,
                compatibleWithCurrentStack: true,
                stacks: new List<string> { initialStackInManifestModel });

            var compatibleDeselectedBpToStayCompatibleAndDeselected = new FakeBuildpackListItem(
                "compatibleDeselectedBpToStayCompatibleAndDeselected",
                selected: false,
                compatibleWithCurrentStack: true,
                stacks: new List<string> { initialStackInManifestModel, newStackVal });

            var compatibleSelectedBpToBecomeIncompatibleAndDeselected = new FakeBuildpackListItem(
                "compatibleSelectedBpToBecomeIncompatibleAndDeselected",
                selected: true,
                compatibleWithCurrentStack: true,
                stacks: new List<string> { initialStackInManifestModel });

            var compatibleSelectedBpToStayCompatibleAndSelected = new FakeBuildpackListItem(
                "compatibleSelectedBpToStayCompatibleAndSelected",
                selected: true,
                compatibleWithCurrentStack: true,
                stacks: new List<string> { initialStackInManifestModel, newStackVal });

            _sut.BuildpackOptions = new List<BuildpackListItem>
            {
                incompatibleDeselectedBpToStayIncompatibleAndDeselected,
                incompatibleDeselectedBpToBecomeCompatibleAndStayDeselected,
                compatibleDeselectedBpToBecomeIncompatibleAndStayDeselected,
                compatibleDeselectedBpToStayCompatibleAndDeselected,
                compatibleSelectedBpToBecomeIncompatibleAndDeselected,
                compatibleSelectedBpToStayCompatibleAndSelected,
            };

            _sut.SelectedBuildpacks = new ObservableCollection<string>
            {
                compatibleSelectedBpToBecomeIncompatibleAndDeselected.Name,
                compatibleSelectedBpToStayCompatibleAndSelected.Name,
            };

            _receivedEvents.Clear();

            Assert.AreNotEqual(newStackVal, initialStackInManifestModel);

            Assert.IsFalse(incompatibleDeselectedBpToStayIncompatibleAndDeselected.IsSelected);
            Assert.IsFalse(incompatibleDeselectedBpToStayIncompatibleAndDeselected.CompatibleWithStack);
            Assert.IsFalse(incompatibleDeselectedBpToBecomeCompatibleAndStayDeselected.IsSelected);
            Assert.IsFalse(incompatibleDeselectedBpToBecomeCompatibleAndStayDeselected.CompatibleWithStack);
            Assert.IsFalse(compatibleDeselectedBpToBecomeIncompatibleAndStayDeselected.IsSelected);
            Assert.IsTrue(compatibleDeselectedBpToBecomeIncompatibleAndStayDeselected.CompatibleWithStack);
            Assert.IsFalse(compatibleDeselectedBpToStayCompatibleAndDeselected.IsSelected);
            Assert.IsTrue(compatibleDeselectedBpToStayCompatibleAndDeselected.CompatibleWithStack);
            Assert.IsTrue(compatibleSelectedBpToBecomeIncompatibleAndDeselected.IsSelected);
            Assert.IsTrue(compatibleSelectedBpToBecomeIncompatibleAndDeselected.CompatibleWithStack);
            Assert.IsTrue(compatibleSelectedBpToStayCompatibleAndSelected.IsSelected);
            Assert.IsTrue(compatibleSelectedBpToStayCompatibleAndSelected.CompatibleWithStack);

            Assert.AreEqual(0, _receivedEvents.Count);

            _sut.SelectedStack = newStackVal;

            Assert.AreEqual(newStackVal, _sut.SelectedStack);
            Assert.AreEqual(2, _receivedEvents.Count);
            CollectionAssert.Contains(_receivedEvents, "SelectedStack");
            CollectionAssert.Contains(_receivedEvents, "SelectedBuildpacks");

            Assert.IsFalse(incompatibleDeselectedBpToStayIncompatibleAndDeselected.IsSelected);
            Assert.IsFalse(incompatibleDeselectedBpToStayIncompatibleAndDeselected.CompatibleWithStack);
            Assert.IsFalse(incompatibleDeselectedBpToBecomeCompatibleAndStayDeselected.IsSelected);
            Assert.IsTrue(incompatibleDeselectedBpToBecomeCompatibleAndStayDeselected.CompatibleWithStack);
            Assert.IsFalse(compatibleDeselectedBpToBecomeIncompatibleAndStayDeselected.IsSelected);
            Assert.IsFalse(compatibleDeselectedBpToBecomeIncompatibleAndStayDeselected.CompatibleWithStack);
            Assert.IsFalse(compatibleDeselectedBpToStayCompatibleAndDeselected.IsSelected);
            Assert.IsTrue(compatibleDeselectedBpToStayCompatibleAndDeselected.CompatibleWithStack);
            Assert.IsFalse(compatibleSelectedBpToBecomeIncompatibleAndDeselected.IsSelected);
            Assert.IsFalse(compatibleSelectedBpToBecomeIncompatibleAndDeselected.CompatibleWithStack);
            Assert.IsTrue(compatibleSelectedBpToStayCompatibleAndSelected.IsSelected);
            Assert.IsTrue(compatibleSelectedBpToStayCompatibleAndSelected.CompatibleWithStack);

            Assert.AreEqual(1, _sut.SelectedBuildpacks.Count);
            CollectionAssert.Contains(_sut.SelectedBuildpacks, compatibleSelectedBpToStayCompatibleAndSelected.Name);
        }

        [TestMethod]
        [TestCategory("DeploymentDirectoryPath")]
        [TestCategory("DirectoryPathLabel")]
        [TestCategory("ManifestModel")]
        public void DirectoryPathSetter_SetsDirectoryPathLabel_AndPathInManifestModel_WhenDirectoryExistsAtGivenPath()
        {
            _sut.DeploymentDirectoryPath = "junk//path";
            var initialDirectoryPathLabel = _sut.DirectoryPathLabel;
            var initialPathInManifestModel = _sut.ManifestModel.Applications[0].Path;

            Assert.IsNull(_sut.DeploymentDirectoryPath);
            Assert.IsNull(initialPathInManifestModel);
            Assert.AreEqual("<Default App Directory>", _sut.DirectoryPathLabel);
            Assert.AreNotEqual(_fakeProjectPath, initialPathInManifestModel);

            _sut.DeploymentDirectoryPath = _fakeProjectPath;

            Assert.IsNotNull(_sut.DeploymentDirectoryPath);
            Assert.AreNotEqual(initialDirectoryPathLabel, _sut.DirectoryPathLabel);
            Assert.AreEqual(_fakeProjectPath, _sut.ManifestModel.Applications[0].Path);
            Assert.AreEqual(_fakeProjectPath, _sut.DirectoryPathLabel);
            Assert.AreEqual(_fakeProjectPath, _sut.DeploymentDirectoryPath);
        }

        [TestMethod]
        [TestCategory("DeploymentDirectoryPath")]
        [TestCategory("DirectoryPathLabel")]
        [TestCategory("ManifestModel")]
        public void DirectoryPathSetter_DisplaysError_AndSetsDirectoryPathLabelToDefaultAppDirectory_AndSetsManifestModelPathValueToNull_WhenNoDirectoryExistsAtGivenPath()
        {
            var fakePath = "asdf//junk";
            Assert.IsFalse(Directory.Exists(fakePath));

            _sut.DirectoryPathLabel = "fake initial value";

            var initialPathInManifestModel = _sut.ManifestModel.Applications[0].Path;
            Assert.IsNotNull(initialPathInManifestModel);
            Assert.AreNotEqual(initialPathInManifestModel, _sut.DeploymentDirectoryPath);
            Assert.AreNotEqual(fakePath, initialPathInManifestModel);

            Assert.AreNotEqual("<Default App Directory>", _sut.DirectoryPathLabel);

            _sut.DeploymentDirectoryPath = fakePath;

            Assert.AreEqual("<Default App Directory>", _sut.DirectoryPathLabel);
            Assert.AreNotEqual(initialPathInManifestModel, _sut.DeploymentDirectoryPath);
            Assert.IsNull(_sut.ManifestModel.Applications[0].Path);

            MockErrorDialogService.Verify(
                m => m.DisplayErrorDialog(DeploymentDialogViewModel.DirectoryNotFoundTitle, It.Is<string>(s => s.Contains(fakePath) && s.Contains("does not appear to be a valid path"))),
                Times.Once);
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

            CollectionAssert.AreEqual(new List<BuildpackListItem>(), _sut.BuildpackOptions);
            CollectionAssert.Contains(_receivedEvents, "BuildpackOptions");
        }

        [TestMethod]
        [TestCategory("UpdateBuildpackOptions")]
        public async Task UpdateBuildpackOptions_SetsBuildpackOptionsToQueryContent_WhenQuerySucceeds()
        {
            var fakeCf = new FakeCfInstanceViewModel(FakeCfInstance, Services);
            var fakeBuildpacksContent = new List<CfBuildpack>
            {
                new CfBuildpack
                {
                    Name = "bp1",
                    Stack = "stack1",
                },
                new CfBuildpack
                {
                    Name = "bp1",
                    Stack = "stack2",
                },
                new CfBuildpack
                {
                    Name = "bp1",
                    Stack = "stack3",
                },
                new CfBuildpack
                {
                    Name = "bp2",
                    Stack = "stack1",
                },
                new CfBuildpack
                {
                    Name = "bp3",
                    Stack = "stack2",
                },
            };
            var numUniqueBpNamesInFakeResponse = fakeBuildpacksContent.GroupBy(bp => bp.Name).Select(g => g.FirstOrDefault()).ToList().Count;

            var fakeBuildpacksResponse = new DetailedResult<List<CfBuildpack>>
            {
                Succeeded = true,
                Content = fakeBuildpacksContent
            };

            // simulate builpack specification in a pre-loaded manifest; expect corresponding bp list item to be marked as selected
            _sut.ManifestModel.Applications[0].Buildpacks = new List<string> { "bp2" };

            // simulate existing stack selection; expect corresponding bp list items to be appopriately marked as (in)compatible
            _sut.SelectedStack = "stack1";

            MockTasExplorerViewModel.SetupGet(m => m.TasConnection).Returns(fakeCf);

            MockCloudFoundryService.Setup(m => m.GetBuildpacksAsync(fakeCf.CloudFoundryInstance.ApiAddress, 1)).ReturnsAsync(fakeBuildpacksResponse);

            CollectionAssert.AreNotEquivalent(fakeBuildpacksContent, _sut.BuildpackOptions);
            CollectionAssert.DoesNotContain(_receivedEvents, "BuildpackOptions");

            await _sut.UpdateBuildpackOptions();

            Assert.AreEqual(numUniqueBpNamesInFakeResponse, _sut.BuildpackOptions.Count);

            Assert.IsTrue(_sut.BuildpackOptions.Exists(bp => bp.Name == "bp1"
                                                             && bp.ValidStacks.Count == 3
                                                             && bp.ValidStacks.Contains("stack1")
                                                             && bp.ValidStacks.Contains("stack2")
                                                             && bp.ValidStacks.Contains("stack3")
                                                             && bp.IsSelected == false
                                                             && bp.CompatibleWithStack == true));

            Assert.IsTrue(_sut.BuildpackOptions.Exists(bp => bp.Name == "bp2"
                                                             && bp.ValidStacks.Count == 1
                                                             && bp.ValidStacks.Contains("stack1")
                                                             && bp.IsSelected == true
                                                             && bp.CompatibleWithStack == true));

            Assert.IsTrue(_sut.BuildpackOptions.Exists(bp => bp.Name == "bp3"
                                                             && bp.ValidStacks.Count == 1
                                                             && bp.ValidStacks.Contains("stack2")
                                                             && bp.IsSelected == false
                                                             && bp.CompatibleWithStack == false));

            CollectionAssert.Contains(_receivedEvents, "BuildpackOptions");
        }

        [TestMethod]
        [TestCategory("UpdateBuildpackOptions")]
        public async Task UpdateBuildpackOptions_SetsBuildpackOptionsToEmptyList_AndRaisesError_WhenQueryFails()
        {
            var fakeCf = new FakeCfInstanceViewModel(FakeCfInstance, Services);
            const string fakeFailureReason = "junk";
            var fakeBuildpacksResponse = new DetailedResult<List<CfBuildpack>>(succeeded: false, content: null, explanation: fakeFailureReason, cmdDetails: null);

            MockTasExplorerViewModel.SetupGet(m => m.TasConnection).Returns(fakeCf);

            MockCloudFoundryService.Setup(m => m.GetBuildpacksAsync(fakeCf.CloudFoundryInstance.ApiAddress, 1)).ReturnsAsync(fakeBuildpacksResponse);

            CollectionAssert.DoesNotContain(_receivedEvents, "BuildpackOptions");

            await _sut.UpdateBuildpackOptions();

            CollectionAssert.AreEquivalent(new List<BuildpackListItem>(), _sut.BuildpackOptions);
        }

        [TestMethod]
        [TestCategory("AddToSelectedBuildpacks")]
        [TestCategory("ManifestModel")]
        public void AddToSelectedBuildpacks_AddsToSelectedBuildpacks_AndAddsToManifestModelBuildpacks_AndRaisesPropChangedEvent_WhenArgIsString()
        {
            string item = "new entry";
            List<string> initialSelectedBps = _sut.SelectedBuildpacks.ToList();
            var initialBpsInManifestModel = _sut.ManifestModel.Applications[0].Buildpacks;

            CollectionAssert.DoesNotContain(initialSelectedBps, item);
            CollectionAssert.DoesNotContain(initialBpsInManifestModel, item);
            CollectionAssert.DoesNotContain(_receivedEvents, "SelectedBuildpacks");

            _sut.AddToSelectedBuildpacks(item);

            var updatedSelectedBps = _sut.SelectedBuildpacks;
            var updatedManifestModelBps = _sut.ManifestModel.Applications[0].Buildpacks;

            CollectionAssert.AreNotEquivalent(initialSelectedBps, updatedSelectedBps);
            CollectionAssert.AreNotEquivalent(initialSelectedBps, updatedManifestModelBps);
            Assert.AreEqual(initialSelectedBps.Count + 1, updatedSelectedBps.Count);
            Assert.AreEqual(initialSelectedBps.Count + 1, updatedManifestModelBps.Count);
            CollectionAssert.Contains(updatedSelectedBps, item);
            CollectionAssert.Contains(updatedManifestModelBps, item);

            CollectionAssert.Contains(_receivedEvents, "SelectedBuildpacks");
        }

        [TestMethod]
        [TestCategory("AddToSelectedBuildpacks")]
        [TestCategory("ManifestModel")]
        public void AddToSelectedBuildpacks_DoesNothing_WhenArgIsNotString()
        {
            object nonStringItem = new object();
            List<string> initialSelectedBps = _sut.SelectedBuildpacks.ToList();
            var initialBpsInManifestModel = _sut.ManifestModel.Applications[0].Buildpacks;

            CollectionAssert.DoesNotContain(initialSelectedBps, nonStringItem);
            CollectionAssert.DoesNotContain(initialBpsInManifestModel, nonStringItem);
            CollectionAssert.DoesNotContain(_receivedEvents, "SelectedBuildpacks");

            _sut.AddToSelectedBuildpacks(nonStringItem);

            var updatedSelectedBps = _sut.SelectedBuildpacks;
            var updatedBpsInManifestModel = _sut.ManifestModel.Applications[0].Buildpacks;

            CollectionAssert.AreEquivalent(initialSelectedBps, updatedSelectedBps);
            CollectionAssert.AreEquivalent(initialBpsInManifestModel, updatedBpsInManifestModel);
            CollectionAssert.DoesNotContain(_receivedEvents, "SelectedBuildpacks");
        }

        [TestMethod]
        [TestCategory("AddToSelectedBuildpacks")]
        [TestCategory("ManifestModel")]
        public void AddToSelectedBuildpacks_DoesNothing_WhenStringAlreadyExistsInSelectedBuildpacks()
        {
            string item = _sut.ManifestModel.Applications[0].Buildpacks[0];

            _sut.SelectedBuildpacks = new ObservableCollection<string>(_sut.ManifestModel.Applications[0].Buildpacks);

            List<string> initialSelectedBps = _sut.SelectedBuildpacks.ToList();
            var initialBpsInManifestModel = _sut.ManifestModel.Applications[0].Buildpacks;

            CollectionAssert.AreEquivalent(initialSelectedBps, initialBpsInManifestModel);

            _receivedEvents.Clear();

            CollectionAssert.Contains(initialSelectedBps, item);
            CollectionAssert.Contains(initialBpsInManifestModel, item);
            CollectionAssert.DoesNotContain(_receivedEvents, "SelectedBuildpacks");

            _sut.AddToSelectedBuildpacks(item);

            var updatedSelectedBps = _sut.SelectedBuildpacks;
            var updatedBpsInManifestModel = _sut.ManifestModel.Applications[0].Buildpacks;

            CollectionAssert.AreEquivalent(initialSelectedBps, updatedSelectedBps);
            CollectionAssert.AreEquivalent(initialBpsInManifestModel, updatedBpsInManifestModel);
            CollectionAssert.DoesNotContain(_receivedEvents, "SelectedBuildpacks");
        }

        [TestMethod]
        [TestCategory("RemoveFromSelectedBuildpacks")]
        [TestCategory("ManifestModel")]
        public void RemoveFromSelectedBuildpacks_RemovesFromSelectedBuildpacks_AndRemovesFromManifestModelBuildpacks_AndRaisesPropChangedEvent_WhenArgIsString()
        {
            string item = _sut.ManifestModel.Applications[0].Buildpacks[0];

            _sut.SelectedBuildpacks = new ObservableCollection<string>(_sut.ManifestModel.Applications[0].Buildpacks);

            List<string> initialSelectedBps = _sut.SelectedBuildpacks.ToList();
            var initialBpsInManifestModel = _sut.ManifestModel.Applications[0].Buildpacks;

            CollectionAssert.Contains(initialSelectedBps, item);
            CollectionAssert.Contains(initialBpsInManifestModel, item);
            CollectionAssert.AreEquivalent(initialSelectedBps, initialBpsInManifestModel);

            _receivedEvents.Clear();
            CollectionAssert.DoesNotContain(_receivedEvents, "SelectedBuildpacks");

            _sut.RemoveFromSelectedBuildpacks(item);

            var updatedSelectedBps = _sut.SelectedBuildpacks;
            var updatedBpsInManifestModel = _sut.ManifestModel.Applications[0].Buildpacks;

            CollectionAssert.AreNotEquivalent(initialSelectedBps, updatedSelectedBps);
            CollectionAssert.AreNotEquivalent(initialBpsInManifestModel, updatedBpsInManifestModel);
            Assert.AreEqual(initialSelectedBps.Count - 1, updatedSelectedBps.Count);
            Assert.AreEqual(initialBpsInManifestModel.Count - 1, updatedBpsInManifestModel.Count);
            CollectionAssert.DoesNotContain(updatedSelectedBps, item);
            CollectionAssert.DoesNotContain(updatedBpsInManifestModel, item);

            CollectionAssert.Contains(_receivedEvents, "SelectedBuildpacks");
        }

        [TestMethod]
        [TestCategory("RemoveFromSelectedBuildpacks")]
        [TestCategory("ManifestModel")]
        public void RemoveFromSelectedBuildpacks_DoesNothing_WhenArgIsNotString()
        {
            object nonStringItem = new object();

            List<string> initialSelectedBps = _sut.SelectedBuildpacks.ToList();
            var initialBpsInManifestModel = _sut.ManifestModel.Applications[0].Buildpacks;

            _sut.RemoveFromSelectedBuildpacks(nonStringItem);

            var updatedSelectedBps = _sut.SelectedBuildpacks;
            var updatedBpsInManifestModel = _sut.ManifestModel.Applications[0].Buildpacks;

            CollectionAssert.AreEquivalent(initialSelectedBps, updatedSelectedBps);
            CollectionAssert.AreEquivalent(initialBpsInManifestModel, updatedBpsInManifestModel);
            CollectionAssert.DoesNotContain(_receivedEvents, "SelectedBuildpacks");
        }

        [TestMethod]
        [TestCategory("AppName")]
        [TestCategory("ManifestModel")]
        public void AppNameSetter_SetsValueInManifestModel()
        {
            var nameVal = "windows";
            var initialStackInManifestModel = _sut.ManifestModel.Applications[0].Name;

            Assert.AreNotEqual(nameVal, initialStackInManifestModel);

            _sut.AppName = nameVal;

            Assert.AreEqual(nameVal, _sut.AppName);
            Assert.AreEqual(nameVal, _sut.ManifestModel.Applications[0].Name);
            Assert.AreEqual(1, _receivedEvents.Count);
            Assert.AreEqual("AppName", _receivedEvents[0]);
        }
        [TestMethod]
        [TestCategory("StartCommand")]
        [TestCategory("ManifestModel")]
        public void StartCommandSetter_SetsValueInManifestModel()
        {
            var nameVal = "start cmmd";
            var initialStartCmmdInManifestModel = _sut.ManifestModel.Applications[0].Command;

            Assert.AreNotEqual(nameVal, initialStartCmmdInManifestModel);

            _sut.StartCommand = nameVal;

            Assert.AreEqual(nameVal, _sut.StartCommand);
            Assert.AreEqual(nameVal, _sut.ManifestModel.Applications[0].Command);
            Assert.AreEqual(1, _receivedEvents.Count);
            Assert.AreEqual("StartCommand", _receivedEvents[0]);
        }

        [TestMethod]
        [TestCategory("StartCommand")]
        public void StartCommand_SetterRaisesPropChangedEvent()
        {
            Assert.AreEqual(0, _receivedEvents.Count);

            _sut.StartCommand = "new start command";

            Assert.AreEqual(1, _receivedEvents.Count);
            Assert.IsTrue(_receivedEvents.Contains("StartCommand"));
        }

        [TestMethod]
        [TestCategory("UpdateStackOptions")]
        public async Task UpdateStackOptions_SetsStackOptionsToEmptyList_WhenNotLoggedIn()
        {
            Assert.IsNull(_sut.TasExplorerViewModel.TasConnection);

            CollectionAssert.DoesNotContain(_receivedEvents, "StackOptions");

            await _sut.UpdateStackOptions();

            CollectionAssert.AreEqual(new List<string>(), _sut.StackOptions);
            CollectionAssert.Contains(_receivedEvents, "StackOptions");
        }

        [TestMethod]
        [TestCategory("UpdateStackOptions")]
        public async Task UpdateStackOptions_SetsStackOptionsToQueryContent_WhenQuerySucceeds()
        {
            var fakeCf = new FakeCfInstanceViewModel(FakeCfInstance, Services);
            var fakeStacksContent = new List<string>
            {
                "cool_stack",
                "uncool_stack",
                "junk_stack",
            };
            var fakeStacksResponse = new DetailedResult<List<string>>(succeeded: true, content: fakeStacksContent);

            MockTasExplorerViewModel.SetupGet(m => m.TasConnection).Returns(fakeCf);

            MockCloudFoundryService.Setup(m => m.GetStackNamesAsync(fakeCf.CloudFoundryInstance, 1)).ReturnsAsync(fakeStacksResponse);

            Assert.AreNotEqual(fakeStacksContent, _sut.StackOptions);
            CollectionAssert.DoesNotContain(_receivedEvents, "StackOptions");

            await _sut.UpdateStackOptions();

            CollectionAssert.AreEquivalent(fakeStacksContent, _sut.StackOptions);
            CollectionAssert.Contains(_receivedEvents, "StackOptions");
        }

        [TestMethod]
        [TestCategory("UpdateStackOptions")]
        public async Task UpdateStackOptions_SetsStackOptionsToEmptyList_AndRaisesError_WhenQueryFails()
        {
            var fakeCf = new FakeCfInstanceViewModel(FakeCfInstance, Services);
            const string fakeFailureReason = "junk";
            var fakeStacksResponse = new DetailedResult<List<string>>(succeeded: false, content: null, explanation: fakeFailureReason, cmdDetails: null);

            MockTasExplorerViewModel.SetupGet(m => m.TasConnection).Returns(fakeCf);

            MockCloudFoundryService.Setup(m => m.GetStackNamesAsync(fakeCf.CloudFoundryInstance, 1)).ReturnsAsync(fakeStacksResponse);

            CollectionAssert.DoesNotContain(_receivedEvents, "StackOptions");

            await _sut.UpdateStackOptions();

            CollectionAssert.AreEquivalent(new List<string>(), _sut.StackOptions);
        }

        [TestMethod]
        [TestCategory("WriteManifestToFile")]
        public void WriteManifestToFile_WritesSerializedManifestToGivenFilePath()
        {
            var fakeFilePath = "junk";
            var fakeSerializedManifest = "fake yaml content";

            MockSerializationService.Setup(m => m.SerializeCfAppManifest(It.IsAny<AppManifest>())).Returns(fakeSerializedManifest);

            _sut.WriteManifestToFile(fakeFilePath);

            MockFileService.Verify(m => m.WriteTextToFile(fakeFilePath, fakeSerializedManifest), Times.Once);
        }

        [TestMethod]
        [TestCategory("WriteManifestToFile")]
        public void WriteManifestToFile_DisplaysErrorDialog_AndLogsError_WhenFileCreationFails()
        {
            var fakeFilePath = "junk";
            var fakeSerializedManifest = "fake yaml content";
            var fakeExceptionMsg = ":(";

            MockSerializationService.Setup(m => m.SerializeCfAppManifest(It.IsAny<AppManifest>())).Returns(fakeSerializedManifest);
            MockFileService.Setup(m => m.WriteTextToFile(fakeFilePath, fakeSerializedManifest)).Throws(new Exception(fakeExceptionMsg));

            _sut.WriteManifestToFile(fakeFilePath);

            MockErrorDialogService.Verify(m => m.DisplayErrorDialog("Unable to save manifest file", It.Is<string>(s => s.Contains(fakeExceptionMsg) && s.Contains(fakeFilePath))), Times.Once);
        }
    }

    public class FakeBuildpackListItem : BuildpackListItem
    {
        public FakeBuildpackListItem(string name, bool selected, bool compatibleWithCurrentStack, List<string> stacks)
        {
            Name = name;
            IsSelected = selected;
            ValidStacks = stacks;
            if (compatibleWithCurrentStack)
            {
                // set CompatibleWithStack to true
                EvalutateStackCompatibility(null);
            }
            else
            {
                // set CompatibleWithStack to false
                EvalutateStackCompatibility("some junk that should purposefully not appear in list of stacks");
            }
        }
    }
}