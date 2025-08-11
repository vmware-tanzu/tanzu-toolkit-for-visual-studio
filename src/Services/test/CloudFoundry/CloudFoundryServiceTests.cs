using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Tanzu.Toolkit.CloudFoundryApiClient;
using Tanzu.Toolkit.CloudFoundryApiClient.Models;
using Tanzu.Toolkit.Models;
using Tanzu.Toolkit.Services.CfCli;
using Tanzu.Toolkit.Services.CloudFoundry;
using Tanzu.Toolkit.Services.CommandProcess;
using Tanzu.Toolkit.Services.ErrorDialog;
using Tanzu.Toolkit.Services.File;
using Tanzu.Toolkit.Services.Logging;

namespace Tanzu.Toolkit.Services.Tests.CloudFoundry
{
    [TestClass]
    public class CloudFoundryServiceTests : ServicesTestSupport
    {
        private CloudFoundryService _sut;

        private IServiceProvider _services;

        private Mock<ICfApiClient> _mockCfApiClient;
        private Mock<ICfCliService> _mockCfCliService;
        private Mock<IErrorDialog> _mockErrorDialogWindowService;
        private Mock<ICommandProcessService> _mockCommandProcessService;
        private Mock<IFileService> _mockFileService;
        private Mock<ISerializationService> _mockSerializationService;
        private Mock<ILoggingService> _mockLoggingService;
        private Mock<ILogger> _mockLogger;

        private readonly Exception _fakeException = new("junk");

        [TestInitialize]
        public void TestInit()
        {
            var serviceCollection = new ServiceCollection();
            _mockCfApiClient = new Mock<ICfApiClient>();
            _mockCfCliService = new Mock<ICfCliService>();
            _mockErrorDialogWindowService = new Mock<IErrorDialog>();
            _mockCommandProcessService = new Mock<ICommandProcessService>();
            _mockFileService = new Mock<IFileService>();
            _mockSerializationService = new Mock<ISerializationService>();
            _mockLoggingService = new Mock<ILoggingService>();

            _mockLogger = new Mock<ILogger>();
            _mockLoggingService.SetupGet(m => m.Logger).Returns(_mockLogger.Object);

            serviceCollection.AddSingleton(_mockCfApiClient.Object);
            serviceCollection.AddSingleton(_mockCfCliService.Object);
            serviceCollection.AddSingleton(_mockErrorDialogWindowService.Object);
            serviceCollection.AddSingleton(_mockCommandProcessService.Object);
            serviceCollection.AddSingleton(_mockFileService.Object);
            serviceCollection.AddSingleton(_mockSerializationService.Object);
            serviceCollection.AddSingleton(_mockLoggingService.Object);

            _services = serviceCollection.BuildServiceProvider();

            _sut = new CloudFoundryService(_services);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _mockCfApiClient.VerifyAll();
            _mockCfCliService.VerifyAll();
            _mockFileService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("LoginWithCredentials")]
        [DataRow(null)]
        [DataRow("")]
        public async Task LoginWithCredentials_ThrowsArgumentException_WhenUsernameIsInvalid(string username)
        {
            await Assert.ThrowsExactlyAsync<ArgumentException>(() => _sut.LoginWithCredentials(username, _fakeValidPassword));
        }

        [TestMethod]
        [TestCategory("LoginWithCredentials")]
        public async Task LoginWithCredentials_ThrowsArgumentNullException_WhenPasswordIsNull()
        {
            await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => _sut.LoginWithCredentials("junk", null));
        }

        [TestMethod]
        [TestCategory("LoginWithCredentials")]
        public async Task LoginWithCredentials_ReturnsSuccessfulResult_WhenLoginSucceeds()
        {
            _mockCfCliService.Setup(mock => mock.AuthenticateAsync(_fakeValidUsername, _fakeValidPassword))
                .ReturnsAsync(new DetailedResult(true, null, _fakeSuccessCmdResult));

            var result = await _sut.LoginWithCredentials(_fakeValidUsername, _fakeValidPassword);

            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
        }

        [TestMethod]
        [TestCategory("LoginWithCredentials")]
        public async Task LoginWithCredentials_ReturnsFailedResult_WhenAuthenticationFails()
        {
            _mockCfCliService.Setup(mock => mock.AuthenticateAsync(_fakeValidUsername, _fakeValidPassword))
                .ReturnsAsync(new DetailedResult(false, "fake failure message", _fakeFailureCmdResult));

            var result = await _sut.LoginWithCredentials(_fakeValidUsername, _fakeValidPassword);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Explanation.Contains(CloudFoundryService._loginFailureMessage));
        }

        [TestMethod]
        [TestCategory("LoginWithCredentials")]
        public async Task LoginWithCredentials_IncludesNestedExceptionMessages_WhenExceptionIsThrown()
        {
            var baseMessage = "base exception message";
            var innerMessage = "inner exception message";
            var outerMessage = "outer exception message";
            var multilayeredException = new Exception(outerMessage, new Exception(innerMessage, new Exception(baseMessage)));

            _mockCfCliService.Setup(mock => mock.AuthenticateAsync(_fakeValidUsername, _fakeValidPassword))
                .Throws(multilayeredException);

            var result = await _sut.LoginWithCredentials(_fakeValidUsername, _fakeValidPassword);

            Assert.IsTrue(result.Explanation.Contains(baseMessage));
            Assert.IsTrue(result.Explanation.Contains(innerMessage));
            Assert.IsTrue(result.Explanation.Contains(outerMessage));
        }

        [TestMethod]
        [TestCategory("LoginWithCredentials")]
        public async Task LoginWithCredentials_InvokesCfCliAuthenticateAsync()
        {
            var fakeCfAuthResponse = new DetailedResult(true, null, new CommandResult(null, null, 0));

            _mockCfCliService.Setup(mock => mock.AuthenticateAsync(_fakeValidUsername, _fakeValidPassword))
                .ReturnsAsync(fakeCfAuthResponse);

            var result = await _sut.LoginWithCredentials(_fakeValidUsername, _fakeValidPassword);

            _mockCfCliService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("MatchCliVersionToApiVersion")]
        public async Task MatchCliVersionToApiVersion_SetsCliVersionTo8_AndRaisesErrorDialog_WhenApiVersionCouldNotBeDetected()
        {
            _mockCfCliService.Setup(mock => mock.AuthenticateAsync(_fakeValidUsername, _fakeValidPassword))
                .ReturnsAsync(new DetailedResult(true, null, _fakeSuccessCmdResult));

            var expectedCliVersion = 8;

            _mockCfCliService.Setup(m => m.GetApiVersion()).ReturnsAsync((Version)null);

            _mockFileService.SetupSet(m => m.CliVersion = expectedCliVersion).Verifiable();

            var result = await _sut.LoginWithCredentials(_fakeValidUsername, _fakeValidPassword);

            _mockFileService.VerifyAll();
            _mockCfCliService.VerifyAll();
            _mockErrorDialogWindowService.Verify(
                m => m.DisplayErrorDialog(CloudFoundryService._ccApiVersionUndetectableErrTitle, CloudFoundryService._ccApiVersionUndetectableErrMsg),
                Times.Once);
        }

        [TestMethod]
        [TestCategory("GetOrgs")]
        public async Task GetOrgsForCfInstanceAsync_ReturnsFailedResult_WhenTokenCannotBeFound()
        {
            _mockCfCliService.Setup(m => m.GetOAuthToken())
                .Returns((string)null);

            var result = await _sut.GetOrgsForCfInstanceAsync(_fakeCfInstance);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsNull(result.CmdResult);
            Assert.IsNull(result.Content);

            Assert.IsEmpty(_mockCfApiClient.Invocations);
            _mockLogger.Verify(m => m.Error(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("GetOrgs")]
        public async Task GetOrgsForCfInstanceAsync_RetriesWithFreshToken_WhenListOrgsThrowsException()
        {
            var fakeExceptionMsg = "junk";
            var fakeOrgsResponse = new List<CloudFoundryApiClient.Models.OrgsResponse.Org>
            {
                new() { Name = _org1Name, Guid = _org1Guid, }, new() { Name = _org2Name, Guid = _org2Guid, },
            };

            var expectedResultContent = new List<CloudFoundryOrganization> { new(_org1Name, _org1Guid, _fakeCfInstance), new(_org2Name, _org2Guid, _fakeCfInstance), };

            _mockCfCliService.SetupSequence(m => m.GetOAuthToken())
                .Returns(_expiredAccessToken) // simulate stale cached token on first attempt
                .Returns(_fakeValidAccessToken); // simulate fresh cached token on second attempt

            _mockCfApiClient.Setup(m => m.ListOrgs(_fakeCfInstance.ApiAddress, _expiredAccessToken))
                .Throws(new Exception(fakeExceptionMsg));

            _mockCfApiClient.Setup(m => m.ListOrgs(_fakeCfInstance.ApiAddress, _fakeValidAccessToken))
                .ReturnsAsync(fakeOrgsResponse);

            var result = await _sut.GetOrgsForCfInstanceAsync(_fakeCfInstance);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.IsNull(result.CmdResult);
            Assert.AreEqual(expectedResultContent.Count, result.Content.Count);

            _mockCfCliService.Verify(m => m.ClearCachedAccessToken(), Times.Once);
            _mockCfApiClient.Verify(m => m.ListOrgs(_fakeCfInstance.ApiAddress, It.IsAny<string>()), Times.Exactly(2));
            _mockLogger.Verify(m => m.Information(It.Is<string>(s => s.Contains("retry")), fakeExceptionMsg, It.IsAny<int>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("GetOrgs")]
        public async Task GetOrgsForCfInstanceAsync_ReturnsFailedResult_WhenListOrgsThrowsException_AndThereAreZeroRetriesLeft()
        {
            _mockCfCliService.Setup(m => m.GetOAuthToken())
                .Returns(_fakeValidAccessToken);

            _mockCfApiClient.Setup(m => m.ListOrgs(_fakeCfInstance.ApiAddress, _fakeValidAccessToken))
                .Throws(_fakeException);

            var result = await _sut.GetOrgsForCfInstanceAsync(_fakeCfInstance, retryAmount: 0);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsTrue(result.Explanation.Contains(_fakeException.Message));
            Assert.IsNull(result.CmdResult);
            Assert.IsNull(result.Content);

            _mockLogger.Verify(m => m.Error(It.IsAny<string>(), _fakeException), Times.Once);
        }

        [TestMethod]
        [TestCategory("GetOrgs")]
        public async Task GetOrgsForCfInstanceAsync_ReturnsSuccessfulResult_WhenListOrgsSucceeds()
        {
            var fakeOrgsResponse = new List<CloudFoundryApiClient.Models.OrgsResponse.Org>
            {
                new() { Name = _org1Name, Guid = _org1Guid, },
                new() { Name = _org2Name, Guid = _org2Guid, },
                new() { Name = _org3Name, Guid = _org3Guid, },
                new() { Name = _org4Name, Guid = _org4Guid, },
            };

            var expectedResultContent = new List<CloudFoundryOrganization>
            {
                new(_org1Name, _org1Guid, _fakeCfInstance),
                new(_org2Name, _org2Guid, _fakeCfInstance),
                new(_org3Name, _org3Guid, _fakeCfInstance),
                new(_org4Name, _org4Guid, _fakeCfInstance),
            };

            _mockCfCliService.Setup(m => m.GetOAuthToken())
                .Returns(_fakeValidAccessToken);

            _mockCfApiClient.Setup(m => m.ListOrgs(_fakeCfInstance.ApiAddress, _fakeValidAccessToken))
                .ReturnsAsync(fakeOrgsResponse);

            var result = await _sut.GetOrgsForCfInstanceAsync(_fakeCfInstance);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.IsNull(result.CmdResult);
            Assert.AreEqual(expectedResultContent.Count, result.Content.Count);

            for (var i = 0; i < expectedResultContent.Count; i++)
            {
                Assert.AreEqual(expectedResultContent[i].OrgId, result.Content[i].OrgId);
                Assert.AreEqual(expectedResultContent[i].OrgName, result.Content[i].OrgName);
                Assert.AreEqual(expectedResultContent[i].ParentCf, result.Content[i].ParentCf);
            }
        }

        [TestMethod]
        [TestCategory("GetOrgs")]
        public async Task GetOrgsForCfInstanceAsync_ReturnsFailedResult_WhenTokenRetrievalThrowsInvalidRefreshTokenException()
        {
            _mockCfCliService.Setup(m => m.GetOAuthToken())
                .Throws(new InvalidRefreshTokenException());

            var result = await _sut.GetOrgsForCfInstanceAsync(_fakeCfInstance);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsNull(result.CmdResult);
            Assert.IsNull(result.Content);
            Assert.AreEqual(FailureType.InvalidRefreshToken, result.FailureType);
        }

        [TestMethod]
        [TestCategory("GetSpaces")]
        public async Task GetSpacesForOrgAsync_ReturnsFailedResult_WhenTokenCannotBeFound()
        {
            _mockCfCliService.Setup(m => m.GetOAuthToken())
                .Returns((string)null);

            var result = await _sut.GetSpacesForOrgAsync(_fakeOrg);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsNull(result.CmdResult);
            Assert.IsNull(result.Content);

            Assert.IsEmpty(_mockCfApiClient.Invocations);
            _mockLogger.Verify(m => m.Error(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("GetSpaces")]
        public async Task GetSpacesForOrgAsync_RetriesWithFreshToken_WhenListSpacesThrowsException()
        {
            var fakeExceptionMsg = "junk";
            var fakeSpacesResponse = new List<CloudFoundryApiClient.Models.SpacesResponse.Space>
            {
                new() { Name = _space1Name, Guid = _space1Guid, }, new() { Name = _space2Name, Guid = _space2Guid, },
            };

            var expectedResultContent = new List<CloudFoundrySpace> { new(_space1Name, _space1Guid, _fakeOrg), new(_space2Name, _space2Guid, _fakeOrg), };

            _mockCfCliService.SetupSequence(m => m.GetOAuthToken())
                .Returns(_expiredAccessToken) // simulate stale cached token on first attempt
                .Returns(_fakeValidAccessToken); // simulate fresh cached token on second attempt

            _mockCfApiClient.Setup(m => m.ListSpacesForOrg(_fakeOrg.ParentCf.ApiAddress, _expiredAccessToken, _fakeOrg.OrgId))
                .Throws(new Exception(fakeExceptionMsg));

            _mockCfApiClient.Setup(m => m.ListSpacesForOrg(_fakeOrg.ParentCf.ApiAddress, _fakeValidAccessToken, _fakeOrg.OrgId))
                .ReturnsAsync(fakeSpacesResponse);

            var result = await _sut.GetSpacesForOrgAsync(_fakeOrg);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.IsNull(result.CmdResult);
            Assert.AreEqual(expectedResultContent.Count, result.Content.Count);

            _mockCfCliService.Verify(m => m.ClearCachedAccessToken(), Times.Once);
            _mockCfApiClient.Verify(m => m.ListSpacesForOrg(_fakeOrg.ParentCf.ApiAddress, It.IsAny<string>(), _fakeOrg.OrgId), Times.Exactly(2));
            _mockLogger.Verify(m => m.Information(It.Is<string>(s => s.Contains(fakeExceptionMsg) && s.Contains("retry"))), Times.Once);
        }

        [TestMethod]
        [TestCategory("GetSpaces")]
        public async Task GetSpacesForOrgAsync_ReturnsFailedResult_WhenListSpacesForOrgThrowsException_AndThereAreZeroRetriesLeft()
        {
            _mockCfCliService.Setup(m => m.GetOAuthToken())
                .Returns(_fakeValidAccessToken);

            _mockCfApiClient.Setup(m => m.ListSpacesForOrg(_fakeOrg.ParentCf.ApiAddress, _fakeValidAccessToken, _fakeOrg.OrgId))
                .Throws(_fakeException);

            var result = await _sut.GetSpacesForOrgAsync(_fakeOrg, retryAmount: 0);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsTrue(result.Explanation.Contains(_fakeException.Message));
            Assert.IsNull(result.CmdResult);
            Assert.IsNull(result.Content);

            _mockLogger.Verify(m => m.Error(It.IsAny<string>(), _fakeException), Times.Once);
        }

        [TestMethod]
        [TestCategory("GetSpaces")]
        public async Task GetSpacesForOrgAsync_ReturnsFailedResult_WhenListSpacesForOrgThrowsException()
        {
            _mockCfCliService.Setup(m => m.GetOAuthToken())
                .Returns(_fakeValidAccessToken);

            _mockCfApiClient.Setup(m => m.ListSpacesForOrg(_fakeOrg.ParentCf.ApiAddress, _fakeValidAccessToken, _fakeOrg.OrgId))
                .Throws(_fakeException);

            var result = await _sut.GetSpacesForOrgAsync(_fakeOrg);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsTrue(result.Explanation.Contains(_fakeException.Message));
            Assert.IsNull(result.CmdResult);
            Assert.IsNull(result.Content);

            _mockLogger.Verify(m => m.Error(It.IsAny<string>(), _fakeException), Times.Once);
        }

        [TestMethod]
        [TestCategory("GetSpaces")]
        public async Task GetSpacesForOrgAsync_ReturnsSuccessfulResult_WhenListSpacesSucceeds()
        {
            var fakeSpacesResponse = new List<CloudFoundryApiClient.Models.SpacesResponse.Space>
            {
                new() { Name = _space1Name, Guid = _space1Guid, },
                new() { Name = _space2Name, Guid = _space2Guid, },
                new() { Name = _space3Name, Guid = _space3Guid, },
                new() { Name = _space4Name, Guid = _space4Guid, },
            };

            var expectedResultContent = new List<CloudFoundrySpace>
            {
                new(_space1Name, _space1Guid, _fakeOrg), new(_space2Name, _space2Guid, _fakeOrg), new(_space3Name, _space3Guid, _fakeOrg), new(_space4Name, _space4Guid, _fakeOrg),
            };

            _mockCfCliService.Setup(m => m.GetOAuthToken())
                .Returns(_fakeValidAccessToken);

            _mockCfApiClient.Setup(m => m.ListSpacesForOrg(_fakeOrg.ParentCf.ApiAddress, _fakeValidAccessToken, _fakeOrg.OrgId))
                .ReturnsAsync(fakeSpacesResponse);

            var result = await _sut.GetSpacesForOrgAsync(_fakeOrg);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.IsNull(result.CmdResult);
            Assert.AreEqual(expectedResultContent.Count, result.Content.Count);

            for (var i = 0; i < expectedResultContent.Count; i++)
            {
                Assert.AreEqual(expectedResultContent[i].SpaceId, result.Content[i].SpaceId);
                Assert.AreEqual(expectedResultContent[i].SpaceName, result.Content[i].SpaceName);
                Assert.AreEqual(expectedResultContent[i].ParentOrg, result.Content[i].ParentOrg);
            }
        }

        [TestMethod]
        [TestCategory("GetSpaces")]
        public async Task GetSpacesForOrgAsync_ReturnsFailedResult_WhenTokenRetrievalThrowsInvalidRefreshTokenException()
        {
            _mockCfCliService.Setup(m => m.GetOAuthToken())
                .Throws(new InvalidRefreshTokenException());

            var result = await _sut.GetSpacesForOrgAsync(_fakeOrg);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsNull(result.CmdResult);
            Assert.IsNull(result.Content);
            Assert.AreEqual(FailureType.InvalidRefreshToken, result.FailureType);
        }

        [TestMethod]
        [TestCategory("GetApps")]
        public async Task GetAppsForSpaceAsync_ReturnsFailedResult_WhenTokenCannotBeFound()
        {
            _mockCfCliService.Setup(m => m.GetOAuthToken())
                .Returns((string)null);

            var result = await _sut.GetAppsForSpaceAsync(_fakeSpace);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsNull(result.CmdResult);
            Assert.IsNull(result.Content);

            Assert.IsEmpty(_mockCfApiClient.Invocations);
            _mockLogger.Verify(m => m.Error(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("GetApps")]
        public async Task GetAppsForSpaceAsync_RetriesWithFreshToken_WhenListAppsThrowsException()
        {
            var fakeExceptionMsg = "junk";
            var fakeAppsResponse = new List<CloudFoundryApiClient.Models.AppsResponse.App>
            {
                new()
                {
                    Name = _app1Name,
                    Guid = _app1Guid,
                    State = _app1State,
                    Lifecycle = new CloudFoundryApiClient.Models.AppsResponse.Lifecycle
                    {
                        Type = _onlySupportedAppLifecycleType,
                        Data = new CloudFoundryApiClient.Models.AppsResponse.Data { Buildpacks = [_buildpack1Name], Stack = _stack1Name, }
                    }
                },
                new()
                {
                    Name = _app2Name,
                    Guid = _app2Guid,
                    State = _app2State,
                    Lifecycle = new CloudFoundryApiClient.Models.AppsResponse.Lifecycle
                    {
                        Type = _onlySupportedAppLifecycleType,
                        Data = new CloudFoundryApiClient.Models.AppsResponse.Data { Buildpacks = [_buildpack1Name, _buildpack2Name], Stack = _stack2Name, }
                    }
                },
            };

            var expectedResultContent = new List<CloudFoundryApp> { new(_app1Name, _app1Guid, _fakeSpace, "fake state"), new(_app2Name, _app2Guid, _fakeSpace, "fake state"), };

            _mockCfCliService.SetupSequence(m => m.GetOAuthToken())
                .Returns(_expiredAccessToken) // simulate stale cached token on first attempt
                .Returns(_fakeValidAccessToken); // simulate fresh cached token on second attempt

            _mockCfApiClient.Setup(m => m.ListAppsForSpace(_fakeSpace.ParentOrg.ParentCf.ApiAddress, _expiredAccessToken, _fakeSpace.SpaceId))
                .Throws(new Exception(fakeExceptionMsg));

            _mockCfApiClient.Setup(m => m.ListAppsForSpace(_fakeSpace.ParentOrg.ParentCf.ApiAddress, _fakeValidAccessToken, _fakeSpace.SpaceId))
                .ReturnsAsync(fakeAppsResponse);

            var result = await _sut.GetAppsForSpaceAsync(_fakeSpace);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.IsNull(result.CmdResult);
            Assert.AreEqual(expectedResultContent.Count, result.Content.Count);

            _mockCfCliService.Verify(m => m.ClearCachedAccessToken(), Times.Once);
            _mockCfApiClient.Verify(m => m.ListAppsForSpace(_fakeSpace.ParentOrg.ParentCf.ApiAddress, It.IsAny<string>(), _fakeSpace.SpaceId), Times.Exactly(2));
            _mockLogger.Verify(m => m.Information(It.Is<string>(s => s.Contains("retry")), fakeExceptionMsg, It.IsAny<int>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("GetApps")]
        public async Task GetAppsForSpaceAsync_ReturnsFailedResult_WhenListAppsForSpaceThrowsException_AndThereAreZeroRetriesLeft()
        {
            _mockCfCliService.Setup(m => m.GetOAuthToken())
                .Returns(_fakeValidAccessToken);

            _mockCfApiClient.Setup(m => m.ListAppsForSpace(_fakeSpace.ParentOrg.ParentCf.ApiAddress, _fakeValidAccessToken, _fakeSpace.SpaceId))
                .Throws(_fakeException);

            var result = await _sut.GetAppsForSpaceAsync(_fakeSpace, retryAmount: 0);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsTrue(result.Explanation.Contains(_fakeException.Message));
            Assert.IsNull(result.CmdResult);
            Assert.IsNull(result.Content);

            _mockLogger.Verify(m => m.Error(It.IsAny<string>(), _fakeException), Times.Once);
        }

        [TestMethod]
        [TestCategory("GetApps")]
        public async Task GetAppsForSpaceAsync_ReturnsFailedResult_WhenListAppsForSpaceThrowsException()
        {
            _mockCfCliService.Setup(m => m.GetOAuthToken())
                .Returns(_fakeValidAccessToken);

            _mockCfApiClient.Setup(m => m.ListAppsForSpace(_fakeSpace.ParentOrg.ParentCf.ApiAddress, _fakeValidAccessToken, _fakeSpace.SpaceId))
                .Throws(_fakeException);

            var result = await _sut.GetAppsForSpaceAsync(_fakeSpace);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsTrue(result.Explanation.Contains(_fakeException.Message));
            Assert.IsNull(result.CmdResult);
            Assert.IsNull(result.Content);

            _mockLogger.Verify(m => m.Error(It.IsAny<string>(), _fakeException), Times.Once);
        }

        [TestMethod]
        [TestCategory("GetApps")]
        public async Task GetAppsForSpaceAsync_ReturnsSuccessfulResult_WhenListAppsSucceeds()
        {
            var fakeAppsResponse = new List<CloudFoundryApiClient.Models.AppsResponse.App>
            {
                new()
                {
                    Name = _app1Name,
                    Guid = _app1Guid,
                    State = _app1State,
                    Lifecycle =
                        new CloudFoundryApiClient.Models.AppsResponse.Lifecycle
                        {
                            Type = _onlySupportedAppLifecycleType,
                            Data = new CloudFoundryApiClient.Models.AppsResponse.Data { Buildpacks = [_buildpack1Name], Stack = _stack1Name, }
                        }
                },
                new()
                {
                    Name = _app2Name,
                    Guid = _app2Guid,
                    State = _app2State,
                    Lifecycle =
                        new CloudFoundryApiClient.Models.AppsResponse.Lifecycle
                        {
                            Type = _onlySupportedAppLifecycleType,
                            Data = new CloudFoundryApiClient.Models.AppsResponse.Data { Buildpacks = [_buildpack1Name, _buildpack2Name], Stack = _stack2Name, }
                        }
                },
                new()
                {
                    Name = _app3Name,
                    Guid = _app3Guid,
                    State = _app3State,
                    Lifecycle = new CloudFoundryApiClient.Models.AppsResponse.Lifecycle
                    {
                        Type = _onlySupportedAppLifecycleType,
                        Data = new CloudFoundryApiClient.Models.AppsResponse.Data { Buildpacks = [_buildpack3Name], Stack = _stack3Name, }
                    }
                },
                new()
                {
                    Name = _app4Name,
                    Guid = _app4Guid,
                    State = _app4State,
                    Lifecycle = new CloudFoundryApiClient.Models.AppsResponse.Lifecycle
                    {
                        Type = _onlySupportedAppLifecycleType,
                        Data = new CloudFoundryApiClient.Models.AppsResponse.Data { Buildpacks = [_buildpack1Name, _buildpack2Name, _buildpack4Name], Stack = _stack4Name, }
                    }
                },
            };

            var expectedResultContent = new List<CloudFoundryApp>
            {
                new(_app1Name, _app1Guid, _fakeSpace, _app1State)
                {
                    Stack = fakeAppsResponse[0].Lifecycle.Data.Stack, Buildpacks = [..fakeAppsResponse[0].Lifecycle.Data.Buildpacks],
                },
                new(_app2Name, _app2Guid, _fakeSpace, _app2State)
                {
                    Stack = fakeAppsResponse[1].Lifecycle.Data.Stack, Buildpacks = [..fakeAppsResponse[1].Lifecycle.Data.Buildpacks],
                },
                new(_app3Name, _app3Guid, _fakeSpace, _app3State)
                {
                    Stack = fakeAppsResponse[2].Lifecycle.Data.Stack, Buildpacks = [..fakeAppsResponse[2].Lifecycle.Data.Buildpacks],
                },
                new(_app4Name, _app4Guid, _fakeSpace, _app4State)
                {
                    Stack = fakeAppsResponse[3].Lifecycle.Data.Stack, Buildpacks = [..fakeAppsResponse[3].Lifecycle.Data.Buildpacks],
                },
            };

            _mockCfCliService.Setup(m => m.GetOAuthToken())
                .Returns(_fakeValidAccessToken);

            _mockCfApiClient.Setup(m => m.ListAppsForSpace(_fakeSpace.ParentOrg.ParentCf.ApiAddress, _fakeValidAccessToken, _fakeSpace.SpaceId))
                .ReturnsAsync(fakeAppsResponse);

            var result = await _sut.GetAppsForSpaceAsync(_fakeSpace);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.IsNull(result.CmdResult);
            Assert.AreEqual(expectedResultContent.Count, result.Content.Count);

            for (var i = 0; i < expectedResultContent.Count; i++)
            {
                Assert.AreEqual(expectedResultContent[i].AppId, result.Content[i].AppId);
                Assert.AreEqual(expectedResultContent[i].AppName, result.Content[i].AppName);
                Assert.AreEqual(expectedResultContent[i].ParentSpace, result.Content[i].ParentSpace);
            }
        }

        [TestMethod]
        [TestCategory("GetApps")]
        public async Task GetAppsForSpaceAsync_ReturnsFailedResult_WhenTokenRetrievalThrowsInvalidRefreshTokenException()
        {
            _mockCfCliService.Setup(m => m.GetOAuthToken())
                .Throws(new InvalidRefreshTokenException());

            var result = await _sut.GetAppsForSpaceAsync(_fakeSpace);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsNull(result.CmdResult);
            Assert.IsNull(result.Content);
            Assert.AreEqual(FailureType.InvalidRefreshToken, result.FailureType);
        }

        [TestMethod]
        [TestCategory("GetBuildpacks")]
        public async Task GetBuildpacksAsync_ReturnsSuccessfulResult_WhenListBuildpacksSucceeds()
        {
            var fakeBp1 = new Buildpack { Name = "Bp1", Stack = "StackA", };
            var fakeBp2 = new Buildpack { Name = "Bp2", Stack = "StackA", };
            var fakeBp3 = new Buildpack { Name = "Bp3", Stack = "StackZ", };

            var fakeBuildpacksResponse = new List<Buildpack> { fakeBp1, fakeBp2, fakeBp3 };

            var expectedResultContent = new List<CfBuildpack>
            {
                new() { Name = fakeBp1.Name, Stack = fakeBp1.Stack, },
                new() { Name = fakeBp2.Name, Stack = fakeBp2.Stack, },
                new() { Name = fakeBp3.Name, Stack = fakeBp3.Stack, },
            };

            _mockCfCliService.Setup(m => m.GetOAuthToken()).Returns(_fakeAccessToken);

            _mockCfApiClient.Setup(m => m.ListBuildpacks(_fakeValidTarget, _fakeAccessToken)).ReturnsAsync(fakeBuildpacksResponse);

            var result = await _sut.GetBuildpacksAsync(_fakeValidTarget);

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(expectedResultContent.Count, result.Content.Count);
            foreach (var bp in result.Content)
            {
                Assert.IsTrue(expectedResultContent.Any(originalBuildpack => originalBuildpack.Name == bp.Name && originalBuildpack.Stack == bp.Stack));
            }
        }

        [TestMethod]
        [TestCategory("GetBuildpacks")]
        public async Task GetBuildpacksAsync_ReturnsFailedResult_WhenTokenRetrievalThrowsInvalidRefreshTokenException()
        {
            _mockCfCliService.Setup(m => m.GetOAuthToken())
                .Throws(new InvalidRefreshTokenException());

            var result = await _sut.GetBuildpacksAsync(_fakeValidTarget);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsNull(result.CmdResult);
            Assert.IsNull(result.Content);
            Assert.AreEqual(FailureType.InvalidRefreshToken, result.FailureType);
        }

        [TestMethod]
        [TestCategory("GetBuildpacks")]
        public async Task GetBuildpacksAsync_ReturnsFailedResult_WhenListBuildpacksThrowsException()
        {
            _mockCfCliService.Setup(m => m.GetOAuthToken())
                .Returns(_fakeValidAccessToken);

            _mockCfApiClient.Setup(m => m.ListBuildpacks(_fakeValidTarget, _fakeValidAccessToken))
                .Throws(_fakeException);

            var result = await _sut.GetBuildpacksAsync(_fakeValidTarget);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsTrue(result.Explanation.Contains(_fakeException.Message));
            Assert.IsNull(result.CmdResult);
            Assert.IsNull(result.Content);

            _mockLogger.Verify(m => m.Error(It.IsAny<string>(), _fakeException), Times.Once);
        }

        [TestMethod]
        [TestCategory("GetBuildpacks")]
        public async Task GetBuildpacksAsync_RetriesWithFreshToken_WhenListBuildpacksThrowsException()
        {
            var fakeExceptionMsg = "junk";
            var fakeBp1 = new Buildpack { Name = "Bp1", Stack = "StackA", };
            var fakeBp2 = new Buildpack { Name = "Bp2", Stack = "StackA", };
            var fakeBp3 = new Buildpack { Name = "Bp3", Stack = "StackZ", };

            var fakeBuildpacksResponse = new List<Buildpack> { fakeBp1, fakeBp2, fakeBp3 };

            var expectedResultContent = new List<CfBuildpack>
            {
                new() { Name = fakeBp1.Name, Stack = fakeBp1.Stack, },
                new() { Name = fakeBp2.Name, Stack = fakeBp2.Stack, },
                new() { Name = fakeBp3.Name, Stack = fakeBp3.Stack, },
            };

            _mockCfCliService.SetupSequence(m => m.GetOAuthToken())
                .Returns(_expiredAccessToken) // simulate stale cached token on first attempt
                .Returns(_fakeValidAccessToken); // simulate fresh cached token on second attempt

            _mockCfApiClient.Setup(m => m.ListBuildpacks(_fakeValidTarget, _expiredAccessToken))
                .Throws(new Exception(fakeExceptionMsg));

            _mockCfApiClient.Setup(m => m.ListBuildpacks(_fakeValidTarget, _fakeValidAccessToken))
                .ReturnsAsync(fakeBuildpacksResponse);

            var result = await _sut.GetBuildpacksAsync(_fakeValidTarget);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.IsNull(result.CmdResult);
            Assert.AreEqual(expectedResultContent.Count, result.Content.Count);

            _mockCfCliService.Verify(m => m.ClearCachedAccessToken(), Times.Once);
            _mockCfApiClient.Verify(m => m.ListBuildpacks(_fakeValidTarget, _expiredAccessToken), Times.Once);
            _mockCfApiClient.Verify(m => m.ListBuildpacks(_fakeValidTarget, _fakeValidAccessToken), Times.Once);
            _mockLogger.Verify(m => m.Information(It.Is<string>(s => s.Contains("retry")), fakeExceptionMsg, It.IsAny<int>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("GetBuildpacks")]
        public async Task GetBuildpacksAsync_ReturnsFailedResult_WhenListBuildpacksThrowsException_AndThereAreZeroRetriesLeft()
        {
            _mockCfCliService.Setup(m => m.GetOAuthToken())
                .Returns(_fakeValidAccessToken);

            _mockCfApiClient.Setup(m => m.ListBuildpacks(_fakeValidTarget, _fakeValidAccessToken))
                .Throws(_fakeException);

            var result = await _sut.GetBuildpacksAsync(_fakeValidTarget, retryAmount: 0);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsTrue(result.Explanation.Contains(_fakeException.Message));
            Assert.IsNull(result.CmdResult);
            Assert.IsNull(result.Content);

            _mockLogger.Verify(m => m.Error(It.IsAny<string>(), _fakeException), Times.Once);
        }

        [TestMethod]
        [TestCategory("GetBuildpacks")]
        public async Task GetBuildpacksAsync_ReturnsFailedResult_WhenTokenCannotBeFound()
        {
            _mockCfCliService.Setup(m => m.GetOAuthToken())
                .Returns((string)null);

            var result = await _sut.GetBuildpacksAsync(_fakeValidTarget);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsNull(result.CmdResult);
            Assert.IsNull(result.Content);

            Assert.IsEmpty(_mockCfApiClient.Invocations);
            _mockLogger.Verify(m => m.Error(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("GetServices")]
        public async Task GetServicesAsync_ReturnsSuccessfulResult_WhenListServicesSucceeds()
        {
            var fakeServ1 = new Service { Name = "Serv1", };
            var fakeServ2 = new Service { Name = "Serv2", };
            var fakeServ3 = new Service { Name = "Serv3", };

            var fakeServicesResponse = new List<Service> { fakeServ1, fakeServ2, fakeServ3 };

            var expectedResultContent = new List<CfService> { new() { Name = fakeServ1.Name, }, new() { Name = fakeServ2.Name, }, new() { Name = fakeServ3.Name, }, };

            _mockCfCliService.Setup(m => m.GetOAuthToken()).Returns(_fakeAccessToken);

            _mockCfApiClient.Setup(m => m.ListServices(_fakeValidTarget, _fakeAccessToken)).ReturnsAsync(fakeServicesResponse);

            var result = await _sut.GetServicesAsync(_fakeValidTarget);

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(expectedResultContent.Count, result.Content.Count);
            foreach (var bp in result.Content)
            {
                Assert.IsTrue(expectedResultContent.Any(originalService => originalService.Name == bp.Name));
            }
        }

        [TestMethod]
        [TestCategory("GetServices")]
        public async Task GetServicesAsync_ReturnsFailedResult_WhenTokenRetrievalThrowsInvalidRefreshTokenException()
        {
            _mockCfCliService.Setup(m => m.GetOAuthToken())
                .Throws(new InvalidRefreshTokenException());

            var result = await _sut.GetServicesAsync(_fakeValidTarget);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsNull(result.CmdResult);
            Assert.IsNull(result.Content);
            Assert.AreEqual(FailureType.InvalidRefreshToken, result.FailureType);
        }

        [TestMethod]
        [TestCategory("GetServices")]
        public async Task GetServicesAsync_ReturnsFailedResult_WhenListServicesThrowsException()
        {
            _mockCfCliService.Setup(m => m.GetOAuthToken())
                .Returns(_fakeValidAccessToken);

            _mockCfApiClient.Setup(m => m.ListServices(_fakeValidTarget, _fakeValidAccessToken))
                .Throws(_fakeException);

            var result = await _sut.GetServicesAsync(_fakeValidTarget);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsTrue(result.Explanation.Contains(_fakeException.Message));
            Assert.IsNull(result.CmdResult);
            Assert.IsNull(result.Content);

            _mockLogger.Verify(m => m.Error(It.IsAny<string>(), _fakeException), Times.Once);
        }

        [TestMethod]
        [TestCategory("GetServices")]
        public async Task GetServicesAsync_RetriesWithFreshToken_WhenListServicesThrowsException()
        {
            var fakeExceptionMsg = "junk";
            var fakeServ1 = new Service { Name = "Serv1", };
            var fakeServ2 = new Service { Name = "Serv2", };
            var fakeServ3 = new Service { Name = "Serv3", };

            var fakeServicesResponse = new List<Service> { fakeServ1, fakeServ2, fakeServ3 };

            var expectedResultContent = new List<CfService> { new() { Name = fakeServ1.Name, }, new() { Name = fakeServ2.Name, }, new() { Name = fakeServ3.Name, }, };

            _mockCfCliService.SetupSequence(m => m.GetOAuthToken())
                .Returns(_expiredAccessToken) // simulate stale cached token on first attempt
                .Returns(_fakeValidAccessToken); // simulate fresh cached token on second attempt

            _mockCfApiClient.Setup(m => m.ListServices(_fakeValidTarget, _expiredAccessToken))
                .Throws(new Exception(fakeExceptionMsg));

            _mockCfApiClient.Setup(m => m.ListServices(_fakeValidTarget, _fakeValidAccessToken))
                .ReturnsAsync(fakeServicesResponse);

            var result = await _sut.GetServicesAsync(_fakeValidTarget);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.IsNull(result.CmdResult);
            Assert.AreEqual(expectedResultContent.Count, result.Content.Count);

            _mockCfCliService.Verify(m => m.ClearCachedAccessToken(), Times.Once);
            _mockCfApiClient.Verify(m => m.ListServices(_fakeValidTarget, _expiredAccessToken), Times.Once);
            _mockCfApiClient.Verify(m => m.ListServices(_fakeValidTarget, _fakeValidAccessToken), Times.Once);
            _mockLogger.Verify(m => m.Information(It.Is<string>(s => s.Contains("retry")), fakeExceptionMsg, It.IsAny<int>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("GetServices")]
        public async Task GetServicesAsync_ReturnsFailedResult_WhenListServicesThrowsException_AndThereAreZeroRetriesLeft()
        {
            _mockCfCliService.Setup(m => m.GetOAuthToken())
                .Returns(_fakeValidAccessToken);

            _mockCfApiClient.Setup(m => m.ListServices(_fakeValidTarget, _fakeValidAccessToken))
                .Throws(_fakeException);

            var result = await _sut.GetServicesAsync(_fakeValidTarget, retryAmount: 0);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsTrue(result.Explanation.Contains(_fakeException.Message));
            Assert.IsNull(result.CmdResult);
            Assert.IsNull(result.Content);

            _mockLogger.Verify(m => m.Error(It.IsAny<string>(), _fakeException), Times.Once);
        }

        [TestMethod]
        [TestCategory("GetServices")]
        public async Task GetServicesAsync_ReturnsFailedResult_WhenTokenCannotBeFound()
        {
            _mockCfCliService.Setup(m => m.GetOAuthToken())
                .Returns((string)null);

            var result = await _sut.GetServicesAsync(_fakeValidTarget);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsNull(result.CmdResult);
            Assert.IsNull(result.Content);

            Assert.IsEmpty(_mockCfApiClient.Invocations);
            _mockLogger.Verify(m => m.Error(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("StopApp")]
        public async Task StopAppAsync_ReturnsSuccessfulResult_AndUpdatesAppState()
        {
            _fakeApp.State = "STARTED";

            _mockCfCliService.Setup(m => m.GetOAuthToken())
                .Returns(_fakeValidAccessToken);

            _mockCfApiClient.Setup(m => m.StopAppWithGuid(_fakeApp.ParentSpace.ParentOrg.ParentCf.ApiAddress, _fakeValidAccessToken, _fakeApp.AppId))
                .ReturnsAsync(true);

            var result = await _sut.StopAppAsync(_fakeApp);

            Assert.AreEqual("STOPPED", _fakeApp.State);
            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.IsNull(result.CmdResult);
        }

        [TestMethod]
        [TestCategory("StopApp")]
        public async Task StopAppAsync_ReturnsFailedResult_WhenStopAppWithGuidReturnsFalse()
        {
            _mockCfCliService.Setup(m => m.GetOAuthToken())
                .Returns(_fakeValidAccessToken);

            _mockCfApiClient.Setup(m => m.StopAppWithGuid(_fakeApp.ParentSpace.ParentOrg.ParentCf.ApiAddress, _fakeValidAccessToken, _fakeApp.AppId))
                .ReturnsAsync(false);

            var result = await _sut.StopAppAsync(_fakeApp);

            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsNull(result.CmdResult);

            _mockLogger.Verify(m => m.Error(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("StopApp")]
        public async Task StopAppAsync_ReturnsFailedResult_WhenStopAppWithGuidThrowsException()
        {
            var appName = _fakeApp.AppName;

            _mockCfCliService.Setup(m => m.GetOAuthToken())
                .Returns(_fakeValidAccessToken);

            _mockCfApiClient.Setup(m => m.StopAppWithGuid(_fakeApp.ParentSpace.ParentOrg.ParentCf.ApiAddress, _fakeValidAccessToken, _fakeApp.AppId))
                .Throws(_fakeException);

            var result = await _sut.StopAppAsync(_fakeApp);

            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsTrue(result.Explanation.Contains(_fakeException.Message));
            Assert.IsNull(result.CmdResult);

            _mockLogger.Verify(m => m.Error(It.IsAny<string>(), _fakeException), Times.Once);
        }

        [TestMethod]
        [TestCategory("StopApp")]
        public async Task StopAppAsync_RetriesWithFreshToken_WhenStopAppWithGuidThrowsException()
        {
            var fakeExceptionMsg = "junk";

            _mockCfCliService.SetupSequence(m => m.GetOAuthToken())
                .Returns(_expiredAccessToken) // simulate stale cached token on first attempt
                .Returns(_fakeValidAccessToken); // simulate fresh cached token on second attempt

            _mockCfApiClient.Setup(m => m.StopAppWithGuid(_fakeApp.ParentSpace.ParentOrg.ParentCf.ApiAddress, _expiredAccessToken, _fakeApp.AppId))
                .Throws(new Exception(fakeExceptionMsg));

            _mockCfApiClient.Setup(m => m.StopAppWithGuid(_fakeApp.ParentSpace.ParentOrg.ParentCf.ApiAddress, _fakeValidAccessToken, _fakeApp.AppId))
                .ReturnsAsync(true);

            var result = await _sut.StopAppAsync(_fakeApp);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.IsNull(result.CmdResult);

            _mockCfCliService.Verify(m => m.ClearCachedAccessToken(), Times.Once);
            _mockCfApiClient.Verify(m => m.StopAppWithGuid(_fakeSpace.ParentOrg.ParentCf.ApiAddress, It.IsAny<string>(), _fakeApp.AppId), Times.Exactly(2));
            _mockLogger.Verify(m => m.Information(It.Is<string>(s => s.Contains("retry")), It.IsAny<string>(), fakeExceptionMsg, It.IsAny<int>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("StopApp")]
        public async Task StopAppAsync_ReturnsFailedResult_WhenStopAppWithGuidThrowsException_AndThereAreZeroRetriesLeft()
        {
            _mockCfCliService.Setup(m => m.GetOAuthToken())
                .Returns(_fakeValidAccessToken);

            _mockCfApiClient.Setup(m => m.StopAppWithGuid(_fakeApp.ParentSpace.ParentOrg.ParentCf.ApiAddress, _fakeValidAccessToken, _fakeApp.AppId))
                .Throws(_fakeException);

            var result = await _sut.StopAppAsync(_fakeApp, retryAmount: 0);

            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsTrue(result.Explanation.Contains(_fakeException.Message));
            Assert.IsNull(result.CmdResult);

            _mockLogger.Verify(m => m.Error(It.IsAny<string>(), _fakeException), Times.Once);
        }

        [TestMethod]
        [TestCategory("StopApp")]
        public async Task StopAppAsync_ReturnsFailedResult_WhenTokenRetrievalThrowsInvalidRefreshTokenException()
        {
            _mockCfCliService.Setup(m => m.GetOAuthToken())
                .Throws(new InvalidRefreshTokenException());

            var result = await _sut.StopAppAsync(_fakeApp, retryAmount: 0);

            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsNull(result.CmdResult);
            Assert.AreEqual(FailureType.InvalidRefreshToken, result.FailureType);
        }

        [TestMethod]
        [TestCategory("StartApp")]
        public async Task StartAppAsync_ReturnsSuccessfulResult_AndUpdatesAppState()
        {
            _fakeApp.State = "STOPPED";

            _mockCfCliService.Setup(m => m.GetOAuthToken())
                .Returns(_fakeValidAccessToken);

            _mockCfApiClient.Setup(m => m.StartAppWithGuid(_fakeApp.ParentSpace.ParentOrg.ParentCf.ApiAddress, _fakeValidAccessToken, _fakeApp.AppId))
                .ReturnsAsync(true);

            var result = await _sut.StartAppAsync(_fakeApp);

            Assert.AreEqual("STARTED", _fakeApp.State);
            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.IsNull(result.CmdResult);
        }

        [TestMethod]
        [TestCategory("StartApp")]
        public async Task StartAppAsync_ReturnsFailedResult_WhenStartAppWithGuidReturnsFalse()
        {
            _mockCfCliService.Setup(m => m.GetOAuthToken())
                .Returns(_fakeValidAccessToken);

            _mockCfApiClient.Setup(m => m.StartAppWithGuid(_fakeApp.ParentSpace.ParentOrg.ParentCf.ApiAddress, _fakeValidAccessToken, _fakeApp.AppId))
                .ReturnsAsync(false);

            var result = await _sut.StartAppAsync(_fakeApp);

            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsNull(result.CmdResult);

            _mockLogger.Verify(m => m.Error(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("StartApp")]
        public async Task StartAppAsync_ReturnsFailedResult_WhenStartAppWithGuidThrowsException()
        {
            _mockCfCliService.Setup(m => m.GetOAuthToken())
                .Returns(_fakeValidAccessToken);

            _mockCfApiClient.Setup(m => m.StartAppWithGuid(_fakeApp.ParentSpace.ParentOrg.ParentCf.ApiAddress, _fakeValidAccessToken, _fakeApp.AppId))
                .Throws(_fakeException);

            var result = await _sut.StartAppAsync(_fakeApp);

            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsTrue(result.Explanation.Contains(_fakeException.Message));
            Assert.IsNull(result.CmdResult);

            _mockLogger.Verify(m => m.Error(It.IsAny<string>(), _fakeException), Times.Once);
        }

        [TestMethod]
        [TestCategory("StartApp")]
        public async Task StartAppAsync_RetriesWithFreshToken_WhenStartAppWithGuidThrowsException()
        {
            var fakeExceptionMsg = "junk";

            _mockCfCliService.SetupSequence(m => m.GetOAuthToken())
                .Returns(_expiredAccessToken) // simulate stale cached token on first attempt
                .Returns(_fakeValidAccessToken); // simulate fresh cached token on second attempt

            _mockCfApiClient.Setup(m => m.StartAppWithGuid(_fakeApp.ParentSpace.ParentOrg.ParentCf.ApiAddress, _expiredAccessToken, _fakeApp.AppId))
                .Throws(new Exception(fakeExceptionMsg));

            _mockCfApiClient.Setup(m => m.StartAppWithGuid(_fakeApp.ParentSpace.ParentOrg.ParentCf.ApiAddress, _fakeValidAccessToken, _fakeApp.AppId))
                .ReturnsAsync(true);

            var result = await _sut.StartAppAsync(_fakeApp);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.IsNull(result.CmdResult);

            _mockCfCliService.Verify(m => m.ClearCachedAccessToken(), Times.Once);
            _mockCfApiClient.Verify(m => m.StartAppWithGuid(_fakeSpace.ParentOrg.ParentCf.ApiAddress, It.IsAny<string>(), _fakeApp.AppId), Times.Exactly(2));
            _mockLogger.Verify(m => m.Information(It.Is<string>(s => s.Contains("retry")), It.IsAny<string>(), fakeExceptionMsg, It.IsAny<int>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("StartApp")]
        public async Task StartAppAsync_ReturnsFailedResult_WhenStartAppWithGuidThrowsException_AndThereAreZeroRetriesLeft()
        {
            _mockCfCliService.Setup(m => m.GetOAuthToken())
                .Returns(_fakeValidAccessToken);

            _mockCfApiClient.Setup(m => m.StartAppWithGuid(_fakeApp.ParentSpace.ParentOrg.ParentCf.ApiAddress, _fakeValidAccessToken, _fakeApp.AppId))
                .Throws(_fakeException);

            var result = await _sut.StartAppAsync(_fakeApp, retryAmount: 0);

            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsTrue(result.Explanation.Contains(_fakeException.Message));
            Assert.IsNull(result.CmdResult);

            _mockLogger.Verify(m => m.Error(It.IsAny<string>(), _fakeException), Times.Once);
        }

        [TestMethod]
        [TestCategory("StartApp")]
        public async Task StartAppAsync_ReturnsFailedResult_WhenTokenRetrievalThrowsInvalidRefreshTokenException()
        {
            _mockCfCliService.Setup(m => m.GetOAuthToken())
                .Throws(new InvalidRefreshTokenException());

            var result = await _sut.StartAppAsync(_fakeApp, retryAmount: 0);

            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsNull(result.CmdResult);
            Assert.AreEqual(FailureType.InvalidRefreshToken, result.FailureType);
        }

        [TestMethod]
        [TestCategory("DeleteApp")]
        public async Task DeleteAppAsync_ReturnsSuccessfulResult_AndUpdatesAppState()
        {
            _fakeApp.State = "STOPPED";

            _mockCfCliService.Setup(m => m.GetOAuthToken())
                .Returns(_fakeValidAccessToken);

            _mockCfApiClient.Setup(m => m.DeleteAppWithGuid(_fakeApp.ParentSpace.ParentOrg.ParentCf.ApiAddress, _fakeValidAccessToken, _fakeApp.AppId))
                .ReturnsAsync(true);

            var result = await _sut.DeleteAppAsync(_fakeApp);

            Assert.AreEqual("DELETED", _fakeApp.State);
            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.IsNull(result.CmdResult);
        }

        [TestMethod]
        [TestCategory("DeleteApp")]
        public async Task DeleteAppAsync_ReturnsFailedResult_WhenDeleteAppWithGuidReturnsFalse()
        {
            _mockCfCliService.Setup(m => m.GetOAuthToken())
                .Returns(_fakeValidAccessToken);

            _mockCfApiClient.Setup(m => m.DeleteAppWithGuid(_fakeApp.ParentSpace.ParentOrg.ParentCf.ApiAddress, _fakeValidAccessToken, _fakeApp.AppId))
                .ReturnsAsync(false);

            var result = await _sut.DeleteAppAsync(_fakeApp);

            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsNull(result.CmdResult);

            _mockLogger.Verify(m => m.Error(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("DeleteApp")]
        public async Task DeleteAppAsync_RetriesWithFreshToken_WhenDeleteAppWithGuidThrowsException()
        {
            var fakeExceptionMsg = "junk";

            _mockCfCliService.SetupSequence(m => m.GetOAuthToken())
                .Returns(_expiredAccessToken) // simulate stale cached token on first attempt
                .Returns(_fakeValidAccessToken); // simulate fresh cached token on second attempt

            _mockCfApiClient.Setup(m => m.DeleteAppWithGuid(_fakeApp.ParentSpace.ParentOrg.ParentCf.ApiAddress, _expiredAccessToken, _fakeApp.AppId))
                .Throws(new Exception(fakeExceptionMsg));

            _mockCfApiClient.Setup(m => m.DeleteAppWithGuid(_fakeApp.ParentSpace.ParentOrg.ParentCf.ApiAddress, _fakeValidAccessToken, _fakeApp.AppId))
                .ReturnsAsync(true);

            var result = await _sut.DeleteAppAsync(_fakeApp);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.IsNull(result.CmdResult);

            _mockCfCliService.Verify(m => m.ClearCachedAccessToken(), Times.Once);
            _mockCfApiClient.Verify(m => m.DeleteAppWithGuid(_fakeSpace.ParentOrg.ParentCf.ApiAddress, It.IsAny<string>(), _fakeApp.AppId), Times.Exactly(2));
            _mockLogger.Verify(m => m.Information(It.Is<string>(s => s.Contains("retry")), It.IsAny<string>(), fakeExceptionMsg, It.IsAny<int>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("DeleteApp")]
        public async Task DeleteAppAsync_ReturnsFailedResult_WhenDeleteAppWithGuidThrowsException_AndThereAreZeroRetriesLeft()
        {
            _mockCfCliService.Setup(m => m.GetOAuthToken())
                .Returns(_fakeValidAccessToken);

            _mockCfApiClient.Setup(m => m.DeleteAppWithGuid(_fakeApp.ParentSpace.ParentOrg.ParentCf.ApiAddress, _fakeValidAccessToken, _fakeApp.AppId))
                .Throws(_fakeException);

            var result = await _sut.DeleteAppAsync(_fakeApp, retryAmount: 0);

            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsTrue(result.Explanation.Contains(_fakeException.Message));
            Assert.IsNull(result.CmdResult);

            _mockLogger.Verify(m => m.Error(It.IsAny<string>(), _fakeException), Times.Once);
        }

        [TestMethod]
        [TestCategory("DeleteApp")]
        public async Task DeleteAppAsync_ReturnsFailedResult_WhenTokenRetrievalThrowsInvalidRefreshTokenException()
        {
            _mockCfCliService.Setup(m => m.GetOAuthToken())
                .Throws(new InvalidRefreshTokenException());

            var result = await _sut.DeleteAppAsync(_fakeApp, retryAmount: 0);

            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsNull(result.CmdResult);
            Assert.AreEqual(FailureType.InvalidRefreshToken, result.FailureType);
        }

        [TestMethod]
        [TestCategory("DeleteApp")]
        public async Task DeleteAppAsync_DeletesRoutes_WhenRemoveRoutesIsTrue()
        {
            var expectedApiAddress = _fakeApp.ParentSpace.ParentOrg.ParentCf.ApiAddress;

            var fakeRoute1 = new Route { Guid = "fake-route-guid-1", };
            var fakeRoute2 = new Route { Guid = "fake-route-guid-2", };

            var fakeRoutesResponse = new List<Route> { fakeRoute1, fakeRoute2, };

            _mockCfCliService.Setup(m => m.GetOAuthToken())
                .Returns(_fakeValidAccessToken);

            _mockCfApiClient.Setup(m => m.ListRoutesForApp(expectedApiAddress, _fakeValidAccessToken, _fakeApp.AppId))
                .ReturnsAsync(fakeRoutesResponse);

            _mockCfApiClient.Setup(m => m.DeleteRouteWithGuid(expectedApiAddress, _fakeValidAccessToken, It.IsAny<string>()))
                .ReturnsAsync(true);

            _mockCfApiClient.Setup(m => m.DeleteAppWithGuid(_fakeApp.ParentSpace.ParentOrg.ParentCf.ApiAddress, _fakeValidAccessToken, _fakeApp.AppId))
                .ReturnsAsync(true);

            var result = await _sut.DeleteAppAsync(_fakeApp, removeRoutes: true);

            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);

            _mockCfApiClient.Verify(m => m.DeleteRouteWithGuid(expectedApiAddress, _fakeValidAccessToken, fakeRoute1.Guid), Times.Once);
            _mockCfApiClient.Verify(m => m.DeleteRouteWithGuid(expectedApiAddress, _fakeValidAccessToken, fakeRoute2.Guid), Times.Once);
            _mockCfApiClient.Verify(m => m.DeleteAppWithGuid(expectedApiAddress, _fakeValidAccessToken, _fakeApp.AppId), Times.Once);
        }

        [TestMethod]
        [TestCategory("DeleteApp")]
        public async Task DeleteAppAsync_ReturnsFailedResult_WhenRemoveRoutesIsTrue_AndRoutesFailToDelete()
        {
            var fakeRoute1 = new Route { Guid = "fake-route-guid-1", };
            var fakeRoute2 = new Route { Guid = "fake-route-guid-2", };

            var fakeRoutesResponse = new List<Route> { fakeRoute1, fakeRoute2, };

            _mockCfCliService.Setup(m => m.GetOAuthToken())
                .Returns(_fakeValidAccessToken);

            _mockCfApiClient.Setup(m => m.ListRoutesForApp(_fakeCfInstance.ApiAddress, _fakeValidAccessToken, _fakeApp.AppId))
                .ReturnsAsync(fakeRoutesResponse);

            _mockCfApiClient.Setup(m => m.DeleteRouteWithGuid(_fakeApp.ParentSpace.ParentOrg.ParentCf.ApiAddress, _fakeValidAccessToken, It.IsAny<string>()))
                .ReturnsAsync(false);

            var result = await _sut.DeleteAppAsync(_fakeApp, removeRoutes: true);

            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsTrue(result.Explanation.Contains(CloudFoundryService._routeDeletionErrorMsg));
            Assert.IsTrue(result.Explanation.Contains($"Please try deleting '{_fakeApp.AppName}' again"));

            // ensure app does not get deleted if routes could not be deleted
            _mockCfApiClient.Verify(m => m.DeleteAppWithGuid(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        [TestCategory("DeployApp")]
        public async Task DeployAppAsync_ReturnsFalseResult_WhenCfPushCommandFails()
        {
            var expectedAppName = _exampleManifest.Applications[0].Name;
            var expectedProjPath = _exampleManifest.Applications[0].Path;

            const string fakeFailureExplanation = "junk";
            var fakeCfPushResponse = new DetailedResult(false, fakeFailureExplanation);

            _mockFileService.Setup(mock => mock.DirContainsFiles(It.IsAny<string>())).Returns(true);

            _mockFileService.Setup(mock => mock.GetUniquePathForTempFile($"temp_manifest_{expectedAppName}")).Returns(_fakeManifestPath);

            _mockCfCliService.Setup(mock =>
                    mock.PushAppAsync(_fakeManifestPath, expectedProjPath, _fakeApp.ParentSpace.ParentOrg.OrgName, _fakeApp.ParentSpace.SpaceName, _fakeOutCallback,
                        _fakeErrCallback))
                .ReturnsAsync(fakeCfPushResponse);

            var result = await _sut.DeployAppAsync(_exampleManifest, null, _fakeCfInstance, _fakeOrg, _fakeSpace, _fakeOutCallback, _fakeErrCallback);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Explanation.Contains(fakeFailureExplanation));

            _mockFileService.Verify(m => m.DeleteFile(_fakeManifestPath), Times.Once); // ensure temp manifest was deleted
        }

        [TestMethod]
        [TestCategory("DeployApp")]
        public async Task DeployAppAsync_ReturnsFalseResult_WhenManifestCreationFails()
        {
            var fakeManifestCreationException = new Exception("bummer dude");

            var expectedAppName = _exampleManifest.Applications[0].Name;
            var expectedProjPath = _exampleManifest.Applications[0].Path;

            _mockFileService.Setup(mock => mock.DirContainsFiles(It.IsAny<string>())).Returns(true);

            _mockFileService.Setup(mock => mock.GetUniquePathForTempFile($"temp_manifest_{expectedAppName}")).Returns(_fakeManifestPath);

            _mockFileService.Setup(mock => mock.WriteTextToFile(_fakeManifestPath, It.IsAny<string>())).Throws(fakeManifestCreationException);

            var result = await _sut.DeployAppAsync(_exampleManifest, null, _fakeCfInstance, _fakeOrg, _fakeSpace, _fakeOutCallback, _fakeErrCallback);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Explanation.Contains(fakeManifestCreationException.Message));
        }

        [TestMethod]
        [TestCategory("DeployApp")]
        public async Task DeployAppAsync_ReturnsTrueResult_WhenCfTargetAndPushCommandsSucceed()
        {
            var expectedAppName = _exampleManifest.Applications[0].Name;
            var expectedProjPath = _exampleManifest.Applications[0].Path;

            _mockFileService.Setup(mock => mock.DirContainsFiles(It.IsAny<string>())).Returns(true);

            _mockFileService.Setup(mock => mock.GetUniquePathForTempFile($"temp_manifest_{expectedAppName}")).Returns(_fakeManifestPath);

            _mockCfCliService.Setup(mock =>
                    mock.PushAppAsync(_fakeManifestPath, expectedProjPath, _fakeApp.ParentSpace.ParentOrg.OrgName, _fakeApp.ParentSpace.SpaceName, _fakeOutCallback,
                        _fakeErrCallback))
                .ReturnsAsync(_fakeSuccessDetailedResult);

            var result = await _sut.DeployAppAsync(_exampleManifest, null, _fakeCfInstance, _fakeOrg, _fakeSpace, _fakeOutCallback, _fakeErrCallback);

            Assert.IsTrue(result.Succeeded);

            _mockFileService.Verify(m => m.DeleteFile(_fakeManifestPath), Times.Once); // ensure temp manifest was deleted
        }

        [TestMethod]
        [TestCategory("DeployApp")]
        public async Task DeployAppAsync_ReturnsFalseResult_WhenProjDirContainsNoFiles()
        {
            _mockFileService.Setup(mock => mock.DirContainsFiles(It.IsAny<string>())).Returns(false);

            var result = await _sut.DeployAppAsync(_exampleManifest, null, _fakeCfInstance, _fakeOrg, _fakeSpace, _fakeOutCallback, _fakeErrCallback);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Explanation.Contains(CloudFoundryService._emptyOutputDirMessage));

            // ensure temp manifest was never created
            _mockFileService.Verify(mock => mock.GetUniquePathForTempFile(It.IsAny<string>()), Times.Never);
            _mockFileService.Verify(mock => mock.WriteTextToFile(_fakeManifestPath, It.IsAny<string>()), Times.Never);

            _mockFileService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("DeployApp")]
        public async Task DeployAppAsync_ReturnsFailedResult_WhenCfCliDeploymentThrowsInvalidRefreshTokenException()
        {
            var expectedAppName = _exampleManifest.Applications[0].Name;
            var expectedProjPath = _exampleManifest.Applications[0].Path;

            _mockFileService.Setup(mock => mock.DirContainsFiles(It.IsAny<string>())).Returns(true);

            _mockFileService.Setup(mock => mock.GetUniquePathForTempFile($"temp_manifest_{expectedAppName}")).Returns(_fakeManifestPath);

            _mockCfCliService.Setup(mock => mock.PushAppAsync(_fakeManifestPath, expectedProjPath, _fakeOrg.OrgName, _fakeSpace.SpaceName, _fakeOutCallback, _fakeErrCallback))
                .Throws(new InvalidRefreshTokenException());

            var result = await _sut.DeployAppAsync(_exampleManifest, null, _fakeCfInstance, _fakeOrg, _fakeSpace, _fakeOutCallback, _fakeErrCallback);

            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.AreEqual(FailureType.InvalidRefreshToken, result.FailureType);

            _mockFileService.Verify(m => m.DeleteFile(_fakeManifestPath), Times.Once); // ensure temp manifest was deleted
        }

        [TestMethod]
        [TestCategory("DeployApp")]
        public async Task DeployAppAsync_UsesDefaultAppPath_WhenManifestAppPathIsNull()
        {
            var appManifest = new AppManifest()
            {
                Applications =
                [
                    new AppConfig(),
                ]
            };

            var defaultAppPath = @"fake\app\path";

            Assert.IsNull(appManifest.Applications[0].Path); // ensure manifest app path null

            _mockFileService.Setup(mock => mock.DirContainsFiles(It.IsAny<string>())).Returns(true);

            _mockFileService.Setup(mock => mock.GetUniquePathForTempFile(It.IsAny<string>())).Returns(_fakeManifestPath);

            _mockCfCliService.Setup(mock =>
                    mock.PushAppAsync(_fakeManifestPath, defaultAppPath, _fakeApp.ParentSpace.ParentOrg.OrgName, _fakeApp.ParentSpace.SpaceName, _fakeOutCallback,
                        _fakeErrCallback))
                .ReturnsAsync(_fakeSuccessDetailedResult);

            var result = await _sut.DeployAppAsync(_exampleManifest, defaultAppPath, _fakeCfInstance, _fakeOrg, _fakeSpace, _fakeOutCallback, _fakeErrCallback);

            _mockCfCliService.Verify(mock =>
                mock.PushAppAsync(_fakeManifestPath, defaultAppPath, _fakeApp.ParentSpace.ParentOrg.OrgName, _fakeApp.ParentSpace.SpaceName, _fakeOutCallback, _fakeErrCallback));
        }

        [TestMethod]
        [TestCategory("DeployApp")]
        public async Task DeployAppAsync_UsesManifestAppPath_WhenManifestAppPathIsNotNull()
        {
            var appManifest = new AppManifest()
            {
                Applications =
                [
                    new AppConfig { Path = @"manifest\app\path", },
                ]
            };

            var defaultAppPath = @"fake\app\path";
            var manifestAppPath = appManifest.Applications[0].Path;

            Assert.IsNotNull(appManifest.Applications[0].Path); // ensure manifest app path is not null

            _mockFileService.Setup(mock => mock.DirContainsFiles(It.IsAny<string>())).Returns(true);

            _mockFileService.Setup(mock => mock.GetUniquePathForTempFile(It.IsAny<string>())).Returns(_fakeManifestPath);

            _mockCfCliService.Setup(mock =>
                    mock.PushAppAsync(_fakeManifestPath, manifestAppPath, _fakeApp.ParentSpace.ParentOrg.OrgName, _fakeApp.ParentSpace.SpaceName, _fakeOutCallback,
                        _fakeErrCallback))
                .ReturnsAsync(_fakeSuccessDetailedResult);

            var result = await _sut.DeployAppAsync(appManifest, defaultAppPath, _fakeCfInstance, _fakeOrg, _fakeSpace, _fakeOutCallback, _fakeErrCallback);

            _mockCfCliService.Verify(mock =>
                mock.PushAppAsync(_fakeManifestPath, manifestAppPath, _fakeApp.ParentSpace.ParentOrg.OrgName, _fakeApp.ParentSpace.SpaceName, _fakeOutCallback, _fakeErrCallback));
        }

        [TestMethod]
        [TestCategory("GetRecentLogs")]
        public async Task GetRecentLogs_ReturnsSuccessResult_WhenWrappedMethodSucceeds()
        {
            var logsStub = "These are fake app logs!\n[12:16:04] App took a nap.";
            var fakeLogsResult = new DetailedResult<string>(logsStub, true, null, _fakeSuccessCmdResult);

            _mockCfCliService.Setup(m => m
                    .GetRecentAppLogs(_fakeApp.AppName, _fakeOrg.OrgName, _fakeSpace.SpaceName))
                .ReturnsAsync(fakeLogsResult);

            var result = await _sut.GetRecentLogsAsync(_fakeApp);

            Assert.AreEqual(result.Content, logsStub);
            Assert.AreEqual(result.Succeeded, fakeLogsResult.Succeeded);
            Assert.AreEqual(result.Explanation, fakeLogsResult.Explanation);
            Assert.AreEqual(result.CmdResult, fakeLogsResult.CmdResult);
        }

        [TestMethod]
        [TestCategory("GetRecentLogs")]
        public async Task GetRecentLogs_ReturnsFailedResult_WhenWrappedMethodFails()
        {
            string fakeLogs = null;
            var fakeErrorMsg = "something went wrong";
            var fakeLogsResult = new DetailedResult<string>(fakeLogs, false, fakeErrorMsg, _fakeFailureCmdResult);

            _mockCfCliService.Setup(m => m
                    .GetRecentAppLogs(_fakeApp.AppName, _fakeOrg.OrgName, _fakeSpace.SpaceName))
                .ReturnsAsync(fakeLogsResult);

            var result = await _sut.GetRecentLogsAsync(_fakeApp);

            Assert.IsNull(result.Content);
            Assert.AreEqual(result.Succeeded, fakeLogsResult.Succeeded);
            Assert.AreEqual(result.Explanation, fakeLogsResult.Explanation);
            Assert.AreEqual(result.CmdResult, fakeLogsResult.CmdResult);
        }

        [TestMethod]
        [TestCategory("GetRecentLogs")]
        public async Task GetRecentLogs_ReturnsFailedResult_WhenCfCliCommandThrowsInvalidRefreshTokenException()
        {
            _mockCfCliService.Setup(m => m
                    .GetRecentAppLogs(_fakeApp.AppName, _fakeOrg.OrgName, _fakeSpace.SpaceName))
                .Throws(new InvalidRefreshTokenException());

            var result = await _sut.GetRecentLogsAsync(_fakeApp);

            Assert.IsNull(result.Content);
            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Explanation.Contains("Unable to retrieve app logs"));
            Assert.IsTrue(result.Explanation.Contains(_fakeApp.AppName));
            Assert.IsTrue(result.Explanation.Contains("Please log back in to re-authenticate"));
            Assert.AreEqual(FailureType.InvalidRefreshToken, result.FailureType);
        }

        [TestMethod]
        [TestCategory("CreateManifestFile")]
        public void CreateManifestFile_ReturnsSuccessfulResult_WhenFileCreatedAtGivenPath()
        {
            var pathToFileCreation = "some//junk//path";
            var fakeManifestContent = "some yaml";
            var expectedWriteStr = $"---\n{fakeManifestContent}";

            _mockSerializationService.Setup(m => m.SerializeCfAppManifest(_exampleManifest))
                .Returns(fakeManifestContent);

            _mockFileService.Setup(m => m.WriteTextToFile(pathToFileCreation, expectedWriteStr))
                .Verifiable();

            var result = _sut.CreateManifestFile(pathToFileCreation, _exampleManifest);

            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.IsNull(result.CmdResult);
            Assert.AreEqual(FailureType.None, result.FailureType);

            _mockFileService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("CreateManifestFile")]
        public void CreateManifestFile_ReturnsFailedResult_WhenFileCreationThrowsException()
        {
            var pathToFileCreation = "some//junk//path";
            var fakeExceptionMsg = "Couldn't create file because... we want to simulate an exception :)";
            var fileCreationException = new Exception(fakeExceptionMsg);

            _mockFileService.Setup(m => m.WriteTextToFile(pathToFileCreation, It.IsAny<string>())).Throws(fileCreationException);

            var result = _sut.CreateManifestFile(pathToFileCreation, _exampleManifest);

            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.AreEqual(fakeExceptionMsg, result.Explanation);
            Assert.IsNull(result.CmdResult);
            Assert.AreEqual(FailureType.None, result.FailureType);
        }

        [TestMethod]
        [TestCategory("GetStacks")]
        public async Task GetStackNamesAsync_ReturnsFailedResult_WhenTokenCannotBeFound()
        {
            _mockCfCliService.Setup(m => m.GetOAuthToken())
                .Returns((string)null);

            var result = await _sut.GetStackNamesAsync(_fakeCfInstance);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsNull(result.CmdResult);
            Assert.IsNull(result.Content);

            Assert.IsEmpty(_mockCfApiClient.Invocations);
            _mockLogger.Verify(m => m.Error(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("GetStacks")]
        public async Task GetStackNamesAsync_RetriesWithFreshToken_WhenListStacksThrowsException()
        {
            var fakeExceptionMsg = "junk";
            var fakeStacksResponse = new List<CloudFoundryApiClient.Models.StacksResponse.Stack>
            {
                new() { Name = _stack1Name, Guid = _stack1Guid, }, new() { Name = _stack2Name, Guid = _stack2Guid, },
            };

            var expectedResultContent = new List<string> { _stack1Name, _stack2Name, };

            _mockCfCliService.SetupSequence(m => m.GetOAuthToken())
                .Returns(_expiredAccessToken) // simulate stale cached token on first attempt
                .Returns(_fakeValidAccessToken); // simulate fresh cached token on second attempt

            _mockCfApiClient.Setup(m => m.ListStacks(_fakeCfInstance.ApiAddress, _expiredAccessToken))
                .Throws(new Exception(fakeExceptionMsg));

            _mockCfApiClient.Setup(m => m.ListStacks(_fakeCfInstance.ApiAddress, _fakeValidAccessToken))
                .ReturnsAsync(fakeStacksResponse);

            var result = await _sut.GetStackNamesAsync(_fakeCfInstance);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.IsNull(result.CmdResult);
            Assert.AreEqual(expectedResultContent.Count, result.Content.Count);

            _mockCfCliService.Verify(m => m.ClearCachedAccessToken(), Times.Once);
            _mockCfApiClient.Verify(m => m.ListStacks(_fakeCfInstance.ApiAddress, It.IsAny<string>()), Times.Exactly(2));
            _mockLogger.Verify(m => m.Information(It.Is<string>(s => s.Contains("retry")), fakeExceptionMsg, It.IsAny<int>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("GetStacks")]
        public async Task GetStackNamesAsync_ReturnsFailedResult_WhenListStacksThrowsException_AndThereAreZeroRetriesLeft()
        {
            _mockCfCliService.Setup(m => m.GetOAuthToken())
                .Returns(_fakeValidAccessToken);

            _mockCfApiClient.Setup(m => m.ListStacks(_fakeCfInstance.ApiAddress, _fakeValidAccessToken))
                .Throws(_fakeException);

            var result = await _sut.GetStackNamesAsync(_fakeCfInstance, retryAmount: 0);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsTrue(result.Explanation.Contains(_fakeException.Message));
            Assert.IsNull(result.CmdResult);
            Assert.IsNull(result.Content);

            _mockLogger.Verify(m => m.Error(It.IsAny<string>(), _fakeException), Times.Once);
        }

        [TestMethod]
        [TestCategory("GetStacks")]
        public async Task GetStackNamesAsync_ReturnsSuccessfulResult_WhenListStacksSucceeds()
        {
            var fakeStacksResponse = new List<CloudFoundryApiClient.Models.StacksResponse.Stack>
            {
                new() { Name = _stack1Name, Guid = _stack1Guid, },
                new() { Name = _stack2Name, Guid = _stack2Guid, },
                new() { Name = _stack3Name, Guid = _stack3Guid, },
                new() { Name = _stack4Name, Guid = _stack4Guid, },
            };

            var expectedResultContent = new List<string>
            {
                _stack1Name, _stack2Name, _stack3Name, _stack4Name,
            };

            _mockCfCliService.Setup(m => m.GetOAuthToken())
                .Returns(_fakeValidAccessToken);

            _mockCfApiClient.Setup(m => m.ListStacks(_fakeCfInstance.ApiAddress, _fakeValidAccessToken))
                .ReturnsAsync(fakeStacksResponse);

            var result = await _sut.GetStackNamesAsync(_fakeCfInstance);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.IsNull(result.CmdResult);
            Assert.AreEqual(expectedResultContent.Count, result.Content.Count);

            for (var i = 0; i < expectedResultContent.Count; i++)
            {
                Assert.AreEqual(expectedResultContent[i], result.Content[i]);
            }
        }

        [TestMethod]
        [TestCategory("GetStacks")]
        public async Task GetStackNamesAsync_ReturnsFailedResult_WhenTokenRetrievalThrowsInvalidRefreshTokenException()
        {
            _mockCfCliService.Setup(m => m.GetOAuthToken())
                .Throws(new InvalidRefreshTokenException());

            var result = await _sut.GetStackNamesAsync(_fakeCfInstance);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsNull(result.CmdResult);
            Assert.IsNull(result.Content);
            Assert.AreEqual(FailureType.InvalidRefreshToken, result.FailureType);
        }

        [TestMethod]
        [TestCategory("GetSsoPrompt")]
        public async Task GetSsoPrompt_ReturnsSuccessfulResult_WhenLoginServerInfoRequestSucceeds()
        {
            var fakePasscode = "fake sso passcode";
            var fakeLoginInfoResponse = new LoginInfoResponse
            {
                Prompts = new Dictionary<string, string[]> { { CloudFoundryService._cfApiSsoPromptKey, ["fake content type", fakePasscode] } }
            };

            _mockCfApiClient.Setup(m => m.GetLoginServerInformation(_fakeValidTarget, false))
                .ReturnsAsync(fakeLoginInfoResponse);

            var result = await _sut.GetSsoPrompt(_fakeValidTarget);

            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.AreEqual(fakePasscode, result.Content);
        }

        [TestMethod]
        [TestCategory("GetSsoPrompt")]
        public async Task GetSsoPrompt_ReturnsFailedResult_WhenLoginServerInfoRequestThrowsException()
        {
            var fakeLoginInfoRequestFailure = new Exception("Pretending something went wrong while looking up login server info");

            _mockCfApiClient.Setup(m => m.GetLoginServerInformation(_fakeValidTarget, false))
                .Throws(fakeLoginInfoRequestFailure);

            var result = await _sut.GetSsoPrompt(_fakeValidTarget);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(fakeLoginInfoRequestFailure.Message, result.Explanation);
            Assert.IsNull(result.Content);
        }

        [TestMethod]
        [TestCategory("GetSsoPrompt")]
        public async Task GetSsoPrompt_ReturnsFailedResult_WhenLoginServerInfoDoesNotContainSsoPromptKey()
        {
            var fakeLoginInfoResponse = new LoginInfoResponse { Prompts = new Dictionary<string, string[]> { { "some irrelevant key", ["fake content type", "some content"] } } };

            _mockCfApiClient.Setup(m => m.GetLoginServerInformation(_fakeValidTarget, false))
                .ReturnsAsync(fakeLoginInfoResponse);

            var result = await _sut.GetSsoPrompt(_fakeValidTarget);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual("Unable to determine SSO URL.", result.Explanation);
            Assert.IsNull(result.Content);
        }

        [TestMethod]
        [TestCategory("LoginWithSsoPasscode")]
        public async Task LoginWithSsoPasscode_ReturnsSuccessfulResult_WhenLoginSucceeds()
        {
            var fakePasscode = "fake sso passcode!";

            _mockCfCliService.Setup(m => m.LoginWithSsoPasscode(_fakeValidTarget, fakePasscode))
                .ReturnsAsync(_fakeSuccessDetailedResult);

            var result = await _sut.LoginWithSsoPasscode(_fakeValidTarget, fakePasscode);

            Assert.AreEqual(_fakeSuccessDetailedResult, result);
        }

        [TestMethod]
        [TestCategory("LoginWithSsoPasscode")]
        public async Task LoginWithSsoPasscode_ReturnsFailedResult_WhenLoginFails()
        {
            var fakePasscode = "fake sso passcode!";

            _mockCfCliService.Setup(m => m.LoginWithSsoPasscode(_fakeValidTarget, fakePasscode))
                .ReturnsAsync(_fakeFailureDetailedResult);

            var result = await _sut.LoginWithSsoPasscode(_fakeValidTarget, fakePasscode);

            Assert.AreEqual(_fakeFailureDetailedResult, result);
        }

        [TestMethod]
        [TestCategory("LogoutCfUser")]
        public void LogoutCfUser_InvokesCfLogout_AndClearsCachedAccessToken()
        {
            _sut.LogoutCfUser();

            _mockCfCliService.Verify(m => m.Logout(), Times.Once);
            _mockCfCliService.Verify(m => m.ClearCachedAccessToken(), Times.Once);
        }

        [TestMethod]
        [TestCategory("GetRoutes")]
        public async Task GetRoutesForAppAsync_ReturnsFailedResult_WhenTokenCannotBeFound()
        {
            _mockCfCliService.Setup(m => m.GetOAuthToken())
                .Returns((string)null);

            var result = await _sut.GetRoutesForAppAsync(_fakeApp);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsNull(result.CmdResult);
            Assert.IsNull(result.Content);

            Assert.IsEmpty(_mockCfApiClient.Invocations);
            _mockLogger.Verify(m => m.Error(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("GetRoutes")]
        public async Task GetRoutesForAppAsync_RetriesWithFreshToken_WhenListRoutesThrowsException()
        {
            var fakeExceptionMsg = "junk";

            var fakeRoute1 = new Route { Guid = "fake-route-guid-1", };
            var fakeRoute2 = new Route { Guid = "fake-route-guid-2", };

            var fakeRoutesResponse = new List<Route> { fakeRoute1, fakeRoute2, };

            var expectedResultContent = new List<CloudFoundryRoute> { new(fakeRoute1.Guid), new(fakeRoute2.Guid), };

            _mockCfCliService.SetupSequence(m => m.GetOAuthToken())
                .Returns(_expiredAccessToken) // simulate stale cached token on first attempt
                .Returns(_fakeValidAccessToken); // simulate fresh cached token on second attempt

            _mockCfApiClient.Setup(m => m.ListRoutesForApp(_fakeCfInstance.ApiAddress, _expiredAccessToken, _fakeApp.AppId))
                .Throws(new Exception(fakeExceptionMsg));

            _mockCfApiClient.Setup(m => m.ListRoutesForApp(_fakeCfInstance.ApiAddress, _fakeValidAccessToken, _fakeApp.AppId))
                .ReturnsAsync(fakeRoutesResponse);

            var result = await _sut.GetRoutesForAppAsync(_fakeApp);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.IsNull(result.CmdResult);
            Assert.AreEqual(expectedResultContent.Count, result.Content.Count);

            for (var i = 0; i < expectedResultContent.Count; i++)
            {
                Assert.AreEqual(expectedResultContent[i].RouteGuid, result.Content[i].RouteGuid);
            }

            _mockCfCliService.Verify(m => m.ClearCachedAccessToken(), Times.Once);
            _mockCfApiClient.Verify(m => m.ListRoutesForApp(_fakeCfInstance.ApiAddress, It.IsAny<string>(), _fakeApp.AppId), Times.Exactly(2));
            _mockLogger.Verify(m => m.Information(It.Is<string>(s => s.Contains("retry")), fakeExceptionMsg, It.IsAny<int>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("GetRoutes")]
        public async Task GetRoutesForAppAsync_ReturnsFailedResult_WhenListRoutesThrowsException_AndThereAreZeroRetriesLeft()
        {
            _mockCfCliService.Setup(m => m.GetOAuthToken())
                .Returns(_fakeValidAccessToken);

            _mockCfApiClient.Setup(m => m.ListRoutesForApp(_fakeCfInstance.ApiAddress, _fakeValidAccessToken, _fakeApp.AppId))
                .Throws(_fakeException);

            var result = await _sut.GetRoutesForAppAsync(_fakeApp, retryAmount: 0);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsTrue(result.Explanation.Contains(_fakeException.Message));
            Assert.IsNull(result.CmdResult);
            Assert.IsNull(result.Content);

            _mockLogger.Verify(m => m.Error(It.IsAny<string>(), _fakeException), Times.Once);
        }

        [TestMethod]
        [TestCategory("GetRoutes")]
        public async Task GetRoutesForAppAsync_ReturnsSuccessfulResult_WhenListRoutesSucceeds()
        {
            var fakeRoute1 = new Route { Guid = "fake-route-guid-1", };
            var fakeRoute2 = new Route { Guid = "fake-route-guid-2", };
            var fakeRoute3 = new Route
            {
                Guid = "", // expect this route to be omitted from final result
            };

            var fakeRoutesResponse = new List<Route> { fakeRoute1, fakeRoute2, fakeRoute3, };

            var expectedResultContent = new List<CloudFoundryRoute> { new(fakeRoute1.Guid), new(fakeRoute2.Guid), };

            _mockCfCliService.Setup(m => m.GetOAuthToken())
                .Returns(_fakeValidAccessToken);

            _mockCfApiClient.Setup(m => m.ListRoutesForApp(_fakeCfInstance.ApiAddress, _fakeValidAccessToken, _fakeApp.AppId))
                .ReturnsAsync(fakeRoutesResponse);

            var result = await _sut.GetRoutesForAppAsync(_fakeApp);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.IsNull(result.CmdResult);
            Assert.AreEqual(expectedResultContent.Count, result.Content.Count);

            for (var i = 0; i < expectedResultContent.Count; i++)
            {
                Assert.AreEqual(expectedResultContent[i].RouteGuid, result.Content[i].RouteGuid);
            }
        }

        [TestMethod]
        [TestCategory("GetRoutes")]
        public async Task GetRoutesForAppAsync_ReturnsFailedResult_WhenTokenRetrievalThrowsInvalidRefreshTokenException()
        {
            _mockCfCliService.Setup(m => m.GetOAuthToken())
                .Throws(new InvalidRefreshTokenException());

            var result = await _sut.GetRoutesForAppAsync(_fakeApp);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsTrue(result.Explanation.Contains(_fakeApp.AppName));
            Assert.IsNull(result.CmdResult);
            Assert.IsNull(result.Content);
            Assert.AreEqual(FailureType.InvalidRefreshToken, result.FailureType);
        }

        [TestMethod]
        [TestCategory("DeleteAllRoutesForAppAsync")]
        public async Task DeleteAllRoutesForAppAsync_DeletesEachRoute_AndReturnsSuccessfulResult_WhenRoutesCanBeFoundForApp()
        {
            var expectedAddress = _fakeApp.ParentSpace.ParentOrg.ParentCf.ApiAddress;
            var expectedAppGuid = _fakeApp.AppId;

            var fakeRoutesResponse = new List<Route> { new() { Guid = "this-is-a-fake-guid-1", }, new() { Guid = "this-is-a-fake-guid-2", }, };

            _mockCfCliService.Setup(m => m.GetOAuthToken()).Returns(_fakeValidAccessToken);
            _mockCfApiClient.Setup(m => m.ListRoutesForApp(expectedAddress, _fakeValidAccessToken, expectedAppGuid)).ReturnsAsync(fakeRoutesResponse);
            _mockCfApiClient.Setup(m => m.DeleteRouteWithGuid(expectedAddress, _fakeValidAccessToken, It.IsAny<string>()))
                .ReturnsAsync(true); // pretend deletion succeeded for any route guid

            var result = await _sut.DeleteAllRoutesForAppAsync(_fakeApp);

            Assert.IsTrue(result.Succeeded);

            foreach (var route in fakeRoutesResponse)
            {
                _mockCfApiClient.Verify(m => m.DeleteRouteWithGuid(expectedAddress, _fakeValidAccessToken, route.Guid), Times.Once);
            }
        }

        [TestMethod]
        [TestCategory("DeleteAllRoutesForAppAsync")]
        public async Task DeleteAllRoutesForAppAsync_ReturnsFailedResult_WhenTokenRetrievalThrowsInvalidRefreshTokenException()
        {
            _mockCfCliService.Setup(m => m
                    .GetOAuthToken())
                .Throws(new InvalidRefreshTokenException());

            var result = await _sut.DeleteAllRoutesForAppAsync(_fakeApp);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Explanation.Contains("Unable to retrieve routes"));
            Assert.IsTrue(result.Explanation.Contains(_fakeApp.AppName));
            Assert.IsTrue(result.Explanation.Contains("Please log back in to re-authenticate"));
            Assert.AreEqual(FailureType.InvalidRefreshToken, result.FailureType);
        }

        [TestMethod]
        [TestCategory("DeleteAllRoutesForAppAsync")]
        public async Task DeleteAllRoutesForAppAsync_ReturnsFailedResult_WhenTokenCannotBeFound()
        {
            _mockCfCliService.Setup(m => m.GetOAuthToken())
                .Returns((string)null);

            var result = await _sut.DeleteAllRoutesForAppAsync(_fakeApp);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsNull(result.CmdResult);

            Assert.IsEmpty(_mockCfApiClient.Invocations);
            _mockLogger.Verify(
                m => m.Error(It.Is<string>(s => s.Contains("CloudFoundryService attempted to get routes for '{appName}' but was unable to look up an access token.")),
                    _fakeApp.AppName), Times.Once);
        }

        [TestMethod]
        [TestCategory("DeleteAllRoutesForAppAsync")]
        public async Task DeleteAllRoutesForAppAsync_ReturnsFailedResult_WhenListRoutesThrowsException_AndThereAreZeroRetriesLeft()
        {
            _mockCfCliService.Setup(m => m.GetOAuthToken())
                .Returns(_fakeValidAccessToken);

            _mockCfApiClient.Setup(m => m.ListRoutesForApp(_fakeCfInstance.ApiAddress, _fakeValidAccessToken, _fakeApp.AppId))
                .Throws(_fakeException);

            var result = await _sut.DeleteAllRoutesForAppAsync(_fakeApp);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsTrue(result.Explanation.Contains(_fakeException.Message));
            Assert.IsNull(result.CmdResult);

            _mockLogger.Verify(m => m.Error(It.IsAny<string>(), _fakeException), Times.Once);
        }

        [TestMethod]
        [TestCategory("DeleteAllRoutesForAppAsync")]
        public async Task DeleteAllRoutesForAppAsync_ReturnsFailedResult_WhenAllRouteDeletionsFail()
        {
            var expectedAddress = _fakeApp.ParentSpace.ParentOrg.ParentCf.ApiAddress;
            var expectedAppGuid = _fakeApp.AppId;

            var fakeRoutesResponse = new List<Route> { new() { Guid = "this-is-a-fake-guid-1", }, new() { Guid = "this-is-a-fake-guid-2", }, };

            _mockCfCliService.Setup(m => m.GetOAuthToken()).Returns(_fakeValidAccessToken);
            _mockCfApiClient.Setup(m => m.ListRoutesForApp(expectedAddress, _fakeValidAccessToken, expectedAppGuid)).ReturnsAsync(fakeRoutesResponse);
            _mockCfApiClient.Setup(m => m.DeleteRouteWithGuid(expectedAddress, _fakeValidAccessToken, It.IsAny<string>()))
                .ReturnsAsync(false); // pretend deletion fails for every route guid

            var result = await _sut.DeleteAllRoutesForAppAsync(_fakeApp);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsTrue(result.Explanation.Contains(CloudFoundryService._routeDeletionErrorMsg));
            Assert.IsNull(result.CmdResult);
        }

        [TestMethod]
        [TestCategory("DeleteAllRoutesForAppAsync")]
        public async Task DeleteAllRoutesForAppAsync_ReturnsFailedResult_WhenOneRouteDeletionFails()
        {
            var expectedAddress = _fakeApp.ParentSpace.ParentOrg.ParentCf.ApiAddress;
            var expectedAppGuid = _fakeApp.AppId;

            var fakeRoutesResponse = new List<Route> { new() { Guid = "this-is-a-fake-guid-1", }, new() { Guid = "this-is-a-fake-guid-2", }, };

            _mockCfCliService.Setup(m => m.GetOAuthToken()).Returns(_fakeValidAccessToken);
            _mockCfApiClient.Setup(m => m.ListRoutesForApp(expectedAddress, _fakeValidAccessToken, expectedAppGuid)).ReturnsAsync(fakeRoutesResponse);
            _mockCfApiClient.SetupSequence(m => m.DeleteRouteWithGuid(expectedAddress, _fakeValidAccessToken, It.IsAny<string>()))
                .ReturnsAsync(true) // pretend first deletion succeeds
                .ReturnsAsync(false); // pretend second deletion fails

            var result = await _sut.DeleteAllRoutesForAppAsync(_fakeApp);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsTrue(result.Explanation.Contains(CloudFoundryService._routeDeletionErrorMsg));
            Assert.IsNull(result.CmdResult);
        }

        [TestMethod]
        [TestCategory("DeleteAllRoutesForAppAsync")]
        public async Task DeleteAllRoutesForAppAsync_ReturnsFailedResult_WhenAllRouteDeletionsThrowExceptions()
        {
            var expectedAddress = _fakeApp.ParentSpace.ParentOrg.ParentCf.ApiAddress;
            var expectedAppGuid = _fakeApp.AppId;

            var fakeRoutesResponse = new List<Route> { new() { Guid = "this-is-a-fake-guid-1", }, new() { Guid = "this-is-a-fake-guid-2", }, };

            _mockCfCliService.Setup(m => m.GetOAuthToken()).Returns(_fakeValidAccessToken);
            _mockCfApiClient.Setup(m => m.ListRoutesForApp(expectedAddress, _fakeValidAccessToken, expectedAppGuid)).ReturnsAsync(fakeRoutesResponse);
            _mockCfApiClient.Setup(m => m.DeleteRouteWithGuid(expectedAddress, _fakeValidAccessToken, It.IsAny<string>())).Throws(new Exception());

            var result = await _sut.DeleteAllRoutesForAppAsync(_fakeApp);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsTrue(result.Explanation.Contains(CloudFoundryService._routeDeletionErrorMsg));
            Assert.IsNull(result.CmdResult);
        }

        [TestMethod]
        [TestCategory("DeleteAllRoutesForAppAsync")]
        public async Task DeleteAllRoutesForAppAsync_ReturnsFailedResult_WhenOneRouteDeletionThrowsException()
        {
            var expectedAddress = _fakeApp.ParentSpace.ParentOrg.ParentCf.ApiAddress;
            var expectedAppGuid = _fakeApp.AppId;

            var fakeRoutesResponse = new List<Route> { new() { Guid = "this-is-a-fake-guid-1", }, new() { Guid = "this-is-a-fake-guid-2", }, };

            _mockCfCliService.Setup(m => m.GetOAuthToken()).Returns(_fakeValidAccessToken);
            _mockCfApiClient.Setup(m => m.ListRoutesForApp(expectedAddress, _fakeValidAccessToken, expectedAppGuid)).ReturnsAsync(fakeRoutesResponse);
            _mockCfApiClient.SetupSequence(m => m.DeleteRouteWithGuid(expectedAddress, _fakeValidAccessToken, It.IsAny<string>()))
                .ReturnsAsync(true) // pretend first deletion succeeds
                .Throws(new Exception()); // this will trigger a retry with a fresh access token

            var result = await _sut.DeleteAllRoutesForAppAsync(_fakeApp);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsTrue(result.Explanation.Contains(CloudFoundryService._routeDeletionErrorMsg));
            Assert.IsNull(result.CmdResult);
        }

        [TestMethod]
        [TestCategory("StreamAppLogs")]
        [DynamicData(nameof(GetAppLogStreamArgs), DynamicDataSourceType.Method)]
        public void StreamAppLogs_ReturnsSuccessfulResult_WhenCfCliServiceSucceeds(CloudFoundryApp app, Action<string> stdOutDel, Action<string> stdErrDel)
        {
            var expectedAppName = app.AppName;
            var expectedSpaceName = app.ParentSpace.SpaceName;
            var expectedOrgName = app.ParentSpace.ParentOrg.OrgName;
            var fakeLogStreamProcess = new Process();
            var fakeSuccessResponse = new DetailedResult<Process> { Succeeded = true, Content = fakeLogStreamProcess, };

            _mockCfCliService.Setup(m => m.StreamAppLogs(expectedAppName, expectedOrgName, expectedSpaceName, stdOutDel, stdErrDel)).Returns(fakeSuccessResponse);

            var result = _sut.StreamAppLogs(app, stdOutDel, stdErrDel);

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(fakeLogStreamProcess, result.Content);
            fakeLogStreamProcess.Dispose();
        }

        [TestMethod]
        [TestCategory("StreamAppLogs")]
        public void StreamAppLogs_ReturnsFailedResult_WhenCfCliServiceFails()
        {
            var fakeFailedResponse = new DetailedResult<Process> { Succeeded = false, Explanation = ":(", };
            _mockCfCliService.Setup(m => m.StreamAppLogs(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Action<string>>(), It.IsAny<Action<string>>()))
                .Returns(fakeFailedResponse);

            var result = _sut.StreamAppLogs(_fakeApp, _fakeOutCallback, _fakeErrCallback);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(result.Explanation, fakeFailedResponse.Explanation);
        }

        [TestMethod]
        [TestCategory("StreamAppLogs")]
        public void StreamAppLogs_ReturnsFailedResult_WhenCfCliServiceThrowsException()
        {
            var fakeException = new Exception(":)");
            _mockCfCliService.Setup(m => m.StreamAppLogs(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Action<string>>(), It.IsAny<Action<string>>()))
                .Throws(fakeException);

            var result = _sut.StreamAppLogs(_fakeApp, _fakeOutCallback, _fakeErrCallback);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(result.Explanation, fakeException.Message);
        }

        private static IEnumerable<object[]> GetAppLogStreamArgs()
        {
            var FakeOrg2 = new CloudFoundryOrganization("fake org 2", "fake org guid 2", null);
            var FakeSpace2 = new CloudFoundrySpace("fake space 2", "fake space guid 2", FakeOrg2);
            var FakeApp2 = new CloudFoundryApp("fake app 2", "fake app guid 2", FakeSpace2, null);
            Action<string> fakeOutCallback = s => { };
            Action<string> fakeOutCallback2 = s => { };
            Action<string> fakeErrCallback = s => { };
            Action<string> fakeErrCallback2 = s => { };

            yield return [_fakeApp, fakeOutCallback, fakeErrCallback];
            yield return [FakeApp2, fakeOutCallback2, fakeErrCallback2];
        }
    }
}