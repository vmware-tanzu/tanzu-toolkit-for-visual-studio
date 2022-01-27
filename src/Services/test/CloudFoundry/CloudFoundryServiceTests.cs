using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Serilog;
using System;
using System.Collections;
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
        private ICloudFoundryService _sut;

        private IServiceProvider _services;

        private Mock<ICfApiClient> _mockCfApiClient;
        private Mock<ICfCliService> _mockCfCliService;
        private Mock<IErrorDialog> _mockErrorDialogWindowService;
        private Mock<ICommandProcessService> _mockCommandProcessService;
        private Mock<IFileService> _mockFileService;
        private Mock<ISerializationService> _mockSerializationService;
        private Mock<ILoggingService> _mockLoggingService;
        private Mock<ILogger> _mockLogger;

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
        [TestCategory("ConnectToCfAsync")]
        public async Task ConnectToCFAsync_ThrowsExceptions_WhenParametersAreInvalid()
        {
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => _sut.LoginWithCredentials(null, null, null, false));
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => _sut.LoginWithCredentials(string.Empty, null, null, false));
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => _sut.LoginWithCredentials("Junk", null, null, false));
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => _sut.LoginWithCredentials("Junk", string.Empty, null, false));
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => _sut.LoginWithCredentials("Junk", "Junk", null, false));
        }

        [TestMethod]
        [TestCategory("ConnectToCfAsync")]
        public async Task ConnectToCFAsync_ReturnsSuccessfulResult_WhenLoginSucceeds()
        {
            _mockCfCliService.Setup(mock => mock.
                TargetApi(_fakeValidTarget, true))
                    .Returns(new DetailedResult(true, null, _fakeSuccessCmdResult));

            _mockCfCliService.Setup(mock => mock.
                AuthenticateAsync(_fakeValidUsername, _fakeValidPassword))
                    .ReturnsAsync(new DetailedResult(true, null, _fakeSuccessCmdResult));

            var result = await _sut.LoginWithCredentials(_fakeValidTarget, _fakeValidUsername, _fakeValidPassword, _skipSsl);

            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            _mockCfCliService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("ConnectToCfAsync")]
        public async Task ConnectToCFAsync_ReturnsFailedResult_WhenLoginFails_BecauseTargetApiFails()
        {
            _mockCfCliService.Setup(mock => mock.TargetApi(_fakeValidTarget, true))
                .Returns(new DetailedResult(false, "fake failure message", _fakeFailureCmdResult));

            var result = await _sut.LoginWithCredentials(_fakeValidTarget, _fakeValidUsername, _fakeValidPassword, _skipSsl);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Explanation.Contains(CloudFoundryService.LoginFailureMessage));
            _mockCfCliService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("ConnectToCfAsync")]
        public async Task ConnectToCFAsync_ReturnsFailedResult_WhenLoginFails_BecauseAuthenticateFails()
        {
            _mockCfCliService.Setup(mock => mock.TargetApi(_fakeValidTarget, true))
                .Returns(new DetailedResult(true, null, _fakeSuccessCmdResult));
            _mockCfCliService.Setup(mock => mock.AuthenticateAsync(_fakeValidUsername, _fakeValidPassword))
                .ReturnsAsync(new DetailedResult(false, "fake failure message", _fakeFailureCmdResult));

            var result = await _sut.LoginWithCredentials(_fakeValidTarget, _fakeValidUsername, _fakeValidPassword, _skipSsl);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Explanation.Contains(CloudFoundryService.LoginFailureMessage));
            _mockCfCliService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("ConnectToCfAsync")]
        public async Task ConnectToCfAsync_IncludesNestedExceptionMessages_WhenExceptionIsThrown()
        {
            string baseMessage = "base exception message";
            string innerMessage = "inner exception message";
            string outerMessage = "outer exception message";
            Exception multilayeredException = new Exception(outerMessage, new Exception(innerMessage, new Exception(baseMessage)));

            _mockCfCliService.Setup(mock => mock.
                TargetApi(_fakeValidTarget, true))
                    .Returns(new DetailedResult(true, null, _fakeSuccessCmdResult));

            _mockCfCliService.Setup(mock => mock.
                AuthenticateAsync(_fakeValidUsername, _fakeValidPassword))
                    .Throws(multilayeredException);

            var result = await _sut.LoginWithCredentials(_fakeValidTarget, _fakeValidUsername, _fakeValidPassword, _skipSsl);

            Assert.IsTrue(result.Explanation.Contains(baseMessage));
            Assert.IsTrue(result.Explanation.Contains(innerMessage));
            Assert.IsTrue(result.Explanation.Contains(outerMessage));
            _mockCfCliService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("ConnectToCfAsync")]
        public async Task ConnectToCfAsync_InvokesCfApiAndCfAuthCommands()
        {
            var cfApiArgs = $"api {_fakeValidTarget}{(_skipSsl ? " --skip-ssl-validation" : string.Empty)}";
            var fakeCfApiResponse = new DetailedResult(true, null, new CommandResult(null, null, 0));
            var fakePasswordStr = new System.Net.NetworkCredential(string.Empty, _fakeValidPassword).Password;
            var cfAuthArgs = $"auth {_fakeValidUsername} {fakePasswordStr}";
            var fakeCfAuthResponse = new DetailedResult(true, null, new CommandResult(null, null, 0));

            _mockCfCliService.Setup(mock => mock.
              TargetApi(_fakeValidTarget, It.IsAny<bool>()))
                .Returns(fakeCfApiResponse);

            _mockCfCliService.Setup(mock => mock.
              AuthenticateAsync(_fakeValidUsername, _fakeValidPassword))
                .ReturnsAsync(fakeCfAuthResponse);

            var result = await _sut.LoginWithCredentials(_fakeValidTarget, _fakeValidUsername, _fakeValidPassword, _skipSsl);

            _mockCfCliService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("MatchCliVersionToApiVersion")]
        public async Task MatchCliVersionToApiVersion_SetsCliVersionTo7_AndRaisesErrorDialog_WhenApiVersionCouldNotBeDetected()
        {
            _mockCfCliService.Setup(mock => mock.
                TargetApi(_fakeValidTarget, true))
                    .Returns(new DetailedResult(true, null, _fakeSuccessCmdResult));

            _mockCfCliService.Setup(mock => mock.
                AuthenticateAsync(_fakeValidUsername, _fakeValidPassword))
                    .ReturnsAsync(new DetailedResult(true, null, _fakeSuccessCmdResult));

            int expectedCliVersion = 7;

            _mockCfCliService.Setup(m => m.
                GetApiVersion()).ReturnsAsync((Version)null);

            _mockFileService.SetupSet(m => m.
                CliVersion = expectedCliVersion).Verifiable();

            var result = await _sut.LoginWithCredentials(_fakeValidTarget, _fakeValidUsername, _fakeValidPassword, _skipSsl);

            _mockFileService.VerifyAll();
            _mockCfCliService.VerifyAll();
            _mockErrorDialogWindowService.Verify(m => m.
                DisplayErrorDialog(CloudFoundryService.CcApiVersionUndetectableErrTitle, CloudFoundryService.CcApiVersionUndetectableErrMsg),
                    Times.Once);
        }

        [TestMethod]
        [TestCategory("MatchCliVersionToApiVersion")]
        public async Task MatchCliVersionToApiVersion_SetsCliVersionTo6_WhenApiMajorVersionIs2_AndBetweenMinors_128_149()
        {
            _mockCfCliService.Setup(mock => mock.
                TargetApi(_fakeValidTarget, true))
                    .Returns(new DetailedResult(true, null, _fakeSuccessCmdResult));

            _mockCfCliService.Setup(mock => mock.
                AuthenticateAsync(_fakeValidUsername, _fakeValidPassword))
                    .ReturnsAsync(new DetailedResult(true, null, _fakeSuccessCmdResult));

            int expectedCliVersion = 6;
            Version[] versionInputs = new Version[]
            {
                new Version(major: 2, minor: 149, build: 0),
                new Version(major: 2, minor: 138, build: 0),
                new Version(major: 2, minor: 128, build: 0),
            };

            foreach (Version mockApiVersion in versionInputs)
            {
                _mockCfCliService.Setup(m => m.
                    GetApiVersion()).ReturnsAsync(mockApiVersion);

                _mockFileService.SetupSet(m => m.
                    CliVersion = expectedCliVersion).Verifiable();

                var result = await _sut.LoginWithCredentials(_fakeValidTarget, _fakeValidUsername, _fakeValidPassword, _skipSsl);

                _mockFileService.VerifyAll();
                _mockCfCliService.VerifyAll();
            }
        }

        [TestMethod]
        [TestCategory("MatchCliVersionToApiVersion")]
        public async Task MatchCliVersionToApiVersion_SetsCliVersionTo6_WhenApiMajorVersionIs3_AndBetweenMinors_63_84()
        {
            _mockCfCliService.Setup(mock => mock.
                TargetApi(_fakeValidTarget, true))
                    .Returns(new DetailedResult(true, null, _fakeSuccessCmdResult));

            _mockCfCliService.Setup(mock => mock.
                AuthenticateAsync(_fakeValidUsername, _fakeValidPassword))
                    .ReturnsAsync(new DetailedResult(true, null, _fakeSuccessCmdResult));

            int expectedCliVersion = 6;
            Version[] versionInputs = new Version[]
            {
                new Version(major: 3, minor: 84, build: 0),
                new Version(major: 3, minor: 73, build: 0),
                new Version(major: 3, minor: 63, build: 0),
            };

            foreach (Version mockApiVersion in versionInputs)
            {
                _mockCfCliService.Setup(m => m.
                    GetApiVersion()).ReturnsAsync(mockApiVersion);

                _mockFileService.SetupSet(m => m.
                    CliVersion = expectedCliVersion).Verifiable();

                var result = await _sut.LoginWithCredentials(_fakeValidTarget, _fakeValidUsername, _fakeValidPassword, _skipSsl);

                _mockFileService.VerifyAll();
                _mockCfCliService.VerifyAll();
            }
        }

        [TestMethod]
        [TestCategory("MatchCliVersionToApiVersion")]
        public async Task MatchCliVersionToApiVersion_SetsCliVersionTo7_WhenApiMajorVersionIs2_AndAtOrAboveMinorVersion_150()
        {
            _mockCfCliService.Setup(mock => mock.
                TargetApi(_fakeValidTarget, true))
                    .Returns(new DetailedResult(true, null, _fakeSuccessCmdResult));

            _mockCfCliService.Setup(mock => mock.
                AuthenticateAsync(_fakeValidUsername, _fakeValidPassword))
                    .ReturnsAsync(new DetailedResult(true, null, _fakeSuccessCmdResult));

            int expectedCliVersion = 7;
            Version[] versionInputs = new Version[]
            {
                new Version(major: 2, minor: 150, build: 0),
                new Version(major: 2, minor: 189, build: 0),
                new Version(major: 2, minor: 150, build: 1),
            };

            foreach (Version mockApiVersion in versionInputs)
            {
                _mockCfCliService.Setup(m => m.
                    GetApiVersion()).ReturnsAsync(mockApiVersion);

                _mockFileService.SetupSet(m => m.
                    CliVersion = expectedCliVersion).Verifiable();

                var result = await _sut.LoginWithCredentials(_fakeValidTarget, _fakeValidUsername, _fakeValidPassword, _skipSsl);

                _mockFileService.VerifyAll();
                _mockCfCliService.VerifyAll();
            }
        }

        [TestMethod]
        [TestCategory("MatchCliVersionToApiVersion")]
        public async Task MatchCliVersionToApiVersion_SetsCliVersionTo7_WhenApiMajorVersionIs3_AndAtOrAboveMinorVersion_85()
        {
            _mockCfCliService.Setup(mock => mock.
                TargetApi(_fakeValidTarget, true))
                    .Returns(new DetailedResult(true, null, _fakeSuccessCmdResult));

            _mockCfCliService.Setup(mock => mock.
                AuthenticateAsync(_fakeValidUsername, _fakeValidPassword))
                    .ReturnsAsync(new DetailedResult(true, null, _fakeSuccessCmdResult));

            int expectedCliVersion = 7;
            Version[] versionInputs = new Version[]
            {
                new Version(major: 3, minor: 85, build: 0),
                new Version(major: 3, minor: 178, build: 0),
                new Version(major: 3, minor: 85, build: 1),
            };

            foreach (Version mockApiVersion in versionInputs)
            {
                _mockCfCliService.Setup(m => m.
                    GetApiVersion()).ReturnsAsync(mockApiVersion);

                _mockFileService.SetupSet(m => m.
                    CliVersion = expectedCliVersion).Verifiable();

                var result = await _sut.LoginWithCredentials(_fakeValidTarget, _fakeValidUsername, _fakeValidPassword, _skipSsl);

                _mockFileService.VerifyAll();
                _mockCfCliService.VerifyAll();
            }
        }

        [TestMethod]
        [TestCategory("GetOrgs")]
        public async Task GetOrgsForCfInstanceAsync_ReturnsFailedResult_WhenTokenCannotBeFound()
        {
            _mockCfCliService.Setup(m => m.
                GetOAuthToken())
                    .Returns((string)null);

            var result = await _sut.GetOrgsForCfInstanceAsync(FakeCfInstance);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsNull(result.CmdResult);
            Assert.IsNull(result.Content);

            Assert.IsTrue(_mockCfApiClient.Invocations.Count == 0);
            _mockLogger.Verify(m => m.Error(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("GetOrgs")]
        public async Task GetOrgsForCfInstanceAsync_RetriesWithFreshToken_WhenListOrgsThrowsException()
        {
            var fakeExceptionMsg = "junk";
            var fakeOrgsResponse = new List<CloudFoundryApiClient.Models.OrgsResponse.Org>
            {
                new CloudFoundryApiClient.Models.OrgsResponse.Org
                {
                    Name = _org1Name,
                    Guid = _org1Guid,
                },
                new CloudFoundryApiClient.Models.OrgsResponse.Org
                {
                    Name = _org2Name,
                    Guid = _org2Guid,
                },
            };

            var expectedResultContent = new List<CloudFoundryOrganization>
            {
                new CloudFoundryOrganization(_org1Name, _org1Guid, FakeCfInstance),
                new CloudFoundryOrganization(_org2Name, _org2Guid, FakeCfInstance),
            };

            _mockCfCliService.SetupSequence(m => m.
                GetOAuthToken())
                    .Returns(expiredAccessToken) // simulate stale cached token on first attempt
                    .Returns(_fakeValidAccessToken); // simulate fresh cached token on second attempt

            _mockCfApiClient.Setup(m => m.
                ListOrgs(FakeCfInstance.ApiAddress, expiredAccessToken))
                    .Throws(new Exception(fakeExceptionMsg));

            _mockCfApiClient.Setup(m => m.
                ListOrgs(FakeCfInstance.ApiAddress, _fakeValidAccessToken))
                    .ReturnsAsync(fakeOrgsResponse);

            var result = await _sut.GetOrgsForCfInstanceAsync(FakeCfInstance);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.AreEqual(null, result.CmdResult);
            Assert.AreEqual(expectedResultContent.Count, result.Content.Count);

            _mockCfCliService.Verify(m => m.ClearCachedAccessToken(), Times.Once);
            _mockCfApiClient.Verify(m => m.ListOrgs(FakeCfInstance.ApiAddress, It.IsAny<string>()), Times.Exactly(2));
            _mockLogger.Verify(m => m.Information(It.Is<string>(s => s.Contains("retry")), fakeExceptionMsg, It.IsAny<int>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("GetOrgs")]
        public async Task GetOrgsForCfInstanceAsync_ReturnsFailedResult_WhenListOrgsThrowsException_AndThereAreZeroRetriesLeft()
        {
            var fakeExceptionMsg = "junk";

            _mockCfCliService.Setup(m => m.
                GetOAuthToken())
                    .Returns(_fakeValidAccessToken);

            _mockCfApiClient.Setup(m => m.
                ListOrgs(FakeCfInstance.ApiAddress, _fakeValidAccessToken))
                    .Throws(new Exception(fakeExceptionMsg));

            var result = await _sut.GetOrgsForCfInstanceAsync(FakeCfInstance, retryAmount: 0);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsTrue(result.Explanation.Contains(fakeExceptionMsg));
            Assert.IsNull(result.CmdResult);
            Assert.IsNull(result.Content);

            _mockLogger.Verify(m => m.Error(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("GetOrgs")]
        public async Task GetOrgsForCfInstanceAsync_ReturnsSuccessfulResult_WhenListOrgsSucceeds()
        {
            var fakeOrgsResponse = new List<CloudFoundryApiClient.Models.OrgsResponse.Org>
            {
                new CloudFoundryApiClient.Models.OrgsResponse.Org
                {
                    Name = _org1Name,
                    Guid = _org1Guid,
                },
                new CloudFoundryApiClient.Models.OrgsResponse.Org
                {
                    Name = _org2Name,
                    Guid = _org2Guid,
                },
                new CloudFoundryApiClient.Models.OrgsResponse.Org
                {
                    Name = _org3Name,
                    Guid = _org3Guid,
                },
                new CloudFoundryApiClient.Models.OrgsResponse.Org
                {
                    Name = _org4Name,
                    Guid = _org4Guid,
                },
            };

            var expectedResultContent = new List<CloudFoundryOrganization>
            {
                new CloudFoundryOrganization(_org1Name, _org1Guid, FakeCfInstance),
                new CloudFoundryOrganization(_org2Name, _org2Guid, FakeCfInstance),
                new CloudFoundryOrganization(_org3Name, _org3Guid, FakeCfInstance),
                new CloudFoundryOrganization(_org4Name, _org4Guid, FakeCfInstance),
            };

            _mockCfCliService.Setup(m => m.
                GetOAuthToken())
                    .Returns(_fakeValidAccessToken);

            _mockCfApiClient.Setup(m => m.
                ListOrgs(FakeCfInstance.ApiAddress, _fakeValidAccessToken))
                    .ReturnsAsync(fakeOrgsResponse);

            DetailedResult<List<CloudFoundryOrganization>> result = await _sut.GetOrgsForCfInstanceAsync(FakeCfInstance);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.AreEqual(null, result.CmdResult);
            Assert.AreEqual(expectedResultContent.Count, result.Content.Count);

            for (int i = 0; i < expectedResultContent.Count; i++)
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
            _mockCfCliService.Setup(m => m.
                GetOAuthToken())
                    .Throws(new InvalidRefreshTokenException());

            DetailedResult<List<CloudFoundryOrganization>> result = await _sut.GetOrgsForCfInstanceAsync(FakeCfInstance);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.AreEqual(null, result.CmdResult);
            Assert.AreEqual(null, result.Content);
            Assert.AreEqual(FailureType.InvalidRefreshToken, result.FailureType);
        }

        [TestMethod]
        [TestCategory("GetSpaces")]
        public async Task GetSpacesForOrgAsync_ReturnsFailedResult_WhenTokenCannotBeFound()
        {
            _mockCfCliService.Setup(m => m.
                GetOAuthToken())
                    .Returns((string)null);

            var result = await _sut.GetSpacesForOrgAsync(FakeOrg);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsNull(result.CmdResult);
            Assert.IsNull(result.Content);

            Assert.IsTrue(_mockCfApiClient.Invocations.Count == 0);
            _mockLogger.Verify(m => m.Error(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("GetSpaces")]
        public async Task GetSpacesForOrgAsync_RetriesWithFreshToken_WhenListSpacesThrowsException()
        {
            var fakeExceptionMsg = "junk";
            var fakeSpacesResponse = new List<CloudFoundryApiClient.Models.SpacesResponse.Space>
            {
                new CloudFoundryApiClient.Models.SpacesResponse.Space
                {
                    Name = _space1Name,
                    Guid = _space1Guid,
                },
                new CloudFoundryApiClient.Models.SpacesResponse.Space
                {
                    Name = _space2Name,
                    Guid = _space2Guid,
                },
            };

            var expectedResultContent = new List<CloudFoundrySpace>
            {
                new CloudFoundrySpace(_space1Name, _space1Guid, FakeOrg),
                new CloudFoundrySpace(_space2Name, _space2Guid, FakeOrg),
            };

            _mockCfCliService.SetupSequence(m => m.
                GetOAuthToken())
                    .Returns(expiredAccessToken) // simulate stale cached token on first attempt
                    .Returns(_fakeValidAccessToken); // simulate fresh cached token on second attempt

            _mockCfApiClient.Setup(m => m.
                ListSpacesForOrg(FakeOrg.ParentCf.ApiAddress, expiredAccessToken, FakeOrg.OrgId))
                    .Throws(new Exception(fakeExceptionMsg));

            _mockCfApiClient.Setup(m => m.
                ListSpacesForOrg(FakeOrg.ParentCf.ApiAddress, _fakeValidAccessToken, FakeOrg.OrgId))
                    .ReturnsAsync(fakeSpacesResponse);

            var result = await _sut.GetSpacesForOrgAsync(FakeOrg);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.AreEqual(null, result.CmdResult);
            Assert.AreEqual(expectedResultContent.Count, result.Content.Count);

            _mockCfCliService.Verify(m => m.ClearCachedAccessToken(), Times.Once);
            _mockCfApiClient.Verify(m => m.ListSpacesForOrg(FakeOrg.ParentCf.ApiAddress, It.IsAny<string>(), FakeOrg.OrgId), Times.Exactly(2));
            _mockLogger.Verify(m => m.Information(It.Is<string>(s => s.Contains(fakeExceptionMsg) && s.Contains("retry"))), Times.Once);
        }

        [TestMethod]
        [TestCategory("GetSpaces")]
        public async Task GetSpacesForOrgAsync_ReturnsFailedResult_WhenListSpacesForOrgThrowsException_AndThereAreZeroRetriesLeft()
        {
            var fakeExceptionMsg = "junk";

            _mockCfCliService.Setup(m => m.
                GetOAuthToken())
                    .Returns(_fakeValidAccessToken);

            _mockCfApiClient.Setup(m => m.
                ListSpacesForOrg(FakeOrg.ParentCf.ApiAddress, _fakeValidAccessToken, FakeOrg.OrgId))
                    .Throws(new Exception(fakeExceptionMsg));

            var result = await _sut.GetSpacesForOrgAsync(FakeOrg, retryAmount: 0);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsTrue(result.Explanation.Contains(fakeExceptionMsg));
            Assert.IsNull(result.CmdResult);
            Assert.IsNull(result.Content);

            _mockLogger.Verify(m => m.Error(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("GetSpaces")]
        public async Task GetSpacesForOrgAsync_ReturnsFailedResult_WhenListSpacesForOrgThrowsException()
        {
            var fakeExceptionMsg = "junk";

            _mockCfCliService.Setup(m => m.
                GetOAuthToken())
                    .Returns(_fakeValidAccessToken);

            _mockCfApiClient.Setup(m => m.
                ListSpacesForOrg(FakeOrg.ParentCf.ApiAddress, _fakeValidAccessToken, FakeOrg.OrgId))
                    .Throws(new Exception(fakeExceptionMsg));

            var result = await _sut.GetSpacesForOrgAsync(FakeOrg);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsTrue(result.Explanation.Contains(fakeExceptionMsg));
            Assert.IsNull(result.CmdResult);
            Assert.IsNull(result.Content);

            _mockLogger.Verify(m => m.Error(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("GetSpaces")]
        public async Task GetSpacesForOrgAsync_ReturnsSuccessfulResult_WhenListSpacesSucceeds()
        {
            var fakeSpacesResponse = new List<CloudFoundryApiClient.Models.SpacesResponse.Space>
            {
                new CloudFoundryApiClient.Models.SpacesResponse.Space
                {
                    Name = _space1Name,
                    Guid = _space1Guid,
                },
                new CloudFoundryApiClient.Models.SpacesResponse.Space
                {
                    Name = _space2Name,
                    Guid = _space2Guid,
                },
                new CloudFoundryApiClient.Models.SpacesResponse.Space
                {
                    Name = _space3Name,
                    Guid = _space3Guid,
                },
                new CloudFoundryApiClient.Models.SpacesResponse.Space
                {
                    Name = _space4Name,
                    Guid = _space4Guid,
                },
            };

            var expectedResultContent = new List<CloudFoundrySpace>
            {
                new CloudFoundrySpace(_space1Name, _space1Guid, FakeOrg),
                new CloudFoundrySpace(_space2Name, _space2Guid, FakeOrg),
                new CloudFoundrySpace(_space3Name, _space3Guid, FakeOrg),
                new CloudFoundrySpace(_space4Name, _space4Guid, FakeOrg),
            };

            _mockCfCliService.Setup(m => m.
                GetOAuthToken())
                    .Returns(_fakeValidAccessToken);

            _mockCfApiClient.Setup(m => m.
                ListSpacesForOrg(FakeOrg.ParentCf.ApiAddress, _fakeValidAccessToken, FakeOrg.OrgId))
                    .ReturnsAsync(fakeSpacesResponse);

            DetailedResult<List<CloudFoundrySpace>> result = await _sut.GetSpacesForOrgAsync(FakeOrg);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.AreEqual(null, result.CmdResult);
            Assert.AreEqual(expectedResultContent.Count, result.Content.Count);

            for (int i = 0; i < expectedResultContent.Count; i++)
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
            _mockCfCliService.Setup(m => m.
                GetOAuthToken())
                    .Throws(new InvalidRefreshTokenException());

            DetailedResult<List<CloudFoundrySpace>> result = await _sut.GetSpacesForOrgAsync(FakeOrg);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.AreEqual(null, result.CmdResult);
            Assert.AreEqual(null, result.Content);
            Assert.AreEqual(FailureType.InvalidRefreshToken, result.FailureType);
        }

        [TestMethod]
        [TestCategory("GetApps")]
        public async Task GetAppsForSpaceAsync_ReturnsFailedResult_WhenTokenCannotBeFound()
        {
            _mockCfCliService.Setup(m => m.
                GetOAuthToken())
                    .Returns((string)null);

            var result = await _sut.GetAppsForSpaceAsync(FakeSpace);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsNull(result.CmdResult);
            Assert.IsNull(result.Content);

            Assert.IsTrue(_mockCfApiClient.Invocations.Count == 0);
            _mockLogger.Verify(m => m.Error(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("GetApps")]
        public async Task GetAppsForSpaceAsync_RetriesWithFreshToken_WhenListAppsThrowsException()
        {
            var fakeExceptionMsg = "junk";
            var fakeAppsResponse = new List<CloudFoundryApiClient.Models.AppsResponse.App>
            {
                new CloudFoundryApiClient.Models.AppsResponse.App
                {
                    Name = _app1Name,
                    Guid = _app1Guid,
                    State = _app1State,
                },
                new CloudFoundryApiClient.Models.AppsResponse.App
                {
                    Name = _app2Name,
                    Guid = _app2Guid,
                    State = _app2State,
                },
            };

            var expectedResultContent = new List<CloudFoundryApp>
            {
                new CloudFoundryApp(_app1Name, _app1Guid, FakeSpace, "fake state"),
                new CloudFoundryApp(_app2Name, _app2Guid, FakeSpace, "fake state"),
            };

            _mockCfCliService.SetupSequence(m => m.
                GetOAuthToken())
                    .Returns(expiredAccessToken) // simulate stale cached token on first attempt
                    .Returns(_fakeValidAccessToken); // simulate fresh cached token on second attempt

            _mockCfApiClient.Setup(m => m.
                ListAppsForSpace(FakeSpace.ParentOrg.ParentCf.ApiAddress, expiredAccessToken, FakeSpace.SpaceId))
                    .Throws(new Exception(fakeExceptionMsg));

            _mockCfApiClient.Setup(m => m.
                ListAppsForSpace(FakeSpace.ParentOrg.ParentCf.ApiAddress, _fakeValidAccessToken, FakeSpace.SpaceId))
                    .ReturnsAsync(fakeAppsResponse);

            var result = await _sut.GetAppsForSpaceAsync(FakeSpace);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.AreEqual(null, result.CmdResult);
            Assert.AreEqual(expectedResultContent.Count, result.Content.Count);

            _mockCfCliService.Verify(m => m.ClearCachedAccessToken(), Times.Once);
            _mockCfApiClient.Verify(m => m.ListAppsForSpace(FakeSpace.ParentOrg.ParentCf.ApiAddress, It.IsAny<string>(), FakeSpace.SpaceId), Times.Exactly(2));
            _mockLogger.Verify(m => m.Information(It.Is<string>(s => s.Contains("retry")), fakeExceptionMsg, It.IsAny<int>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("GetApps")]
        public async Task GetAppsForSpaceAsync_ReturnsFailedResult_WhenListAppsForSpaceThrowsException_AndThereAreZeroRetriesLeft()
        {
            var fakeExceptionMsg = "junk";

            _mockCfCliService.Setup(m => m.
                GetOAuthToken())
                    .Returns(_fakeValidAccessToken);

            _mockCfApiClient.Setup(m => m.
                ListAppsForSpace(FakeSpace.ParentOrg.ParentCf.ApiAddress, _fakeValidAccessToken, FakeSpace.SpaceId))
                    .Throws(new Exception(fakeExceptionMsg));

            var result = await _sut.GetAppsForSpaceAsync(FakeSpace, retryAmount: 0);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsTrue(result.Explanation.Contains(fakeExceptionMsg));
            Assert.IsNull(result.CmdResult);
            Assert.IsNull(result.Content);

            _mockLogger.Verify(m => m.Error(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("GetApps")]
        public async Task GetAppsForSpaceAsync_ReturnsFailedResult_WhenListAppsForSpaceThrowsException()
        {
            var fakeExceptionMsg = "junk";

            _mockCfCliService.Setup(m => m.
                GetOAuthToken())
                    .Returns(_fakeValidAccessToken);

            _mockCfApiClient.Setup(m => m.
                ListAppsForSpace(FakeSpace.ParentOrg.ParentCf.ApiAddress, _fakeValidAccessToken, FakeSpace.SpaceId))
                    .Throws(new Exception(fakeExceptionMsg));

            var result = await _sut.GetAppsForSpaceAsync(FakeSpace);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsTrue(result.Explanation.Contains(fakeExceptionMsg));
            Assert.IsNull(result.CmdResult);
            Assert.IsNull(result.Content);

            _mockLogger.Verify(m => m.Error(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("GetApps")]
        public async Task GetAppsForSpaceAsync_ReturnsSuccessfulResult_WhenListAppsSucceeds()
        {
            var fakeAppsResponse = new List<CloudFoundryApiClient.Models.AppsResponse.App>
            {
                new CloudFoundryApiClient.Models.AppsResponse.App
                {
                    Name = _app1Name,
                    Guid = _app1Guid,
                    State = _app1State,
                },
                new CloudFoundryApiClient.Models.AppsResponse.App
                {
                    Name = _app2Name,
                    Guid = _app2Guid,
                    State = _app2State,
                },
                new CloudFoundryApiClient.Models.AppsResponse.App
                {
                    Name = _app3Name,
                    Guid = _app3Guid,
                    State = _app3State,
                },
                new CloudFoundryApiClient.Models.AppsResponse.App
                {
                    Name = _app4Name,
                    Guid = _app4Guid,
                    State = _app4State,
                },
            };

            var expectedResultContent = new List<CloudFoundryApp>
            {
                new CloudFoundryApp(_app1Name, _app1Guid, FakeSpace, _app1State),
                new CloudFoundryApp(_app2Name, _app2Guid, FakeSpace, _app2State),
                new CloudFoundryApp(_app3Name, _app3Guid, FakeSpace, _app3State),
                new CloudFoundryApp(_app4Name, _app4Guid, FakeSpace, _app4State),
            };

            _mockCfCliService.Setup(m => m.
                GetOAuthToken())
                    .Returns(_fakeValidAccessToken);

            _mockCfApiClient.Setup(m => m.
                ListAppsForSpace(FakeSpace.ParentOrg.ParentCf.ApiAddress, _fakeValidAccessToken, FakeSpace.SpaceId))
                    .ReturnsAsync(fakeAppsResponse);

            DetailedResult<List<CloudFoundryApp>> result = await _sut.GetAppsForSpaceAsync(FakeSpace);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.AreEqual(null, result.CmdResult);
            Assert.AreEqual(expectedResultContent.Count, result.Content.Count);

            for (int i = 0; i < expectedResultContent.Count; i++)
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
            _mockCfCliService.Setup(m => m.
                GetOAuthToken())
                    .Throws(new InvalidRefreshTokenException());

            DetailedResult<List<CloudFoundryApp>> result = await _sut.GetAppsForSpaceAsync(FakeSpace);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.AreEqual(null, result.CmdResult);
            Assert.AreEqual(null, result.Content);
            Assert.AreEqual(FailureType.InvalidRefreshToken, result.FailureType);
        }

        [TestMethod]
        [TestCategory("GetBuildpacks")]
        public async Task GetBuildpacksAsync_ReturnsSuccessfulResult_WhenListBuildpacksSucceeds()
        {
            Buildpack fakeBp1 = new Buildpack
            {
                Name = "Bp1",
                Stack = "StackA",
            };
            Buildpack fakeBp2 = new Buildpack
            {
                Name = "Bp2",
                Stack = "StackA",
            };
            Buildpack fakeBp3 = new Buildpack
            {
                Name = "Bp3",
                Stack = "StackZ",
            };

            var fakeBuildpacksResponse = new List<Buildpack> { fakeBp1, fakeBp2, fakeBp3 };

            var expectedResultContent = new List<CfBuildpack>
            {
                new CfBuildpack
                {
                    Name = fakeBp1.Name,
                    Stack = fakeBp1.Stack,
                },
                new CfBuildpack
                {
                    Name = fakeBp2.Name,
                    Stack = fakeBp2.Stack,
                },
                new CfBuildpack
                {
                    Name = fakeBp3.Name,
                    Stack = fakeBp3.Stack,
                },
            };

            _mockCfCliService.Setup(m => m.GetOAuthToken()).Returns(_fakeAccessToken);

            _mockCfApiClient.Setup(m => m.ListBuildpacks(_fakeValidTarget, _fakeAccessToken)).ReturnsAsync(fakeBuildpacksResponse);

            var result = await _sut.GetBuildpacksAsync(_fakeValidTarget);

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(expectedResultContent.Count, result.Content.Count);
            foreach (CfBuildpack bp in result.Content)
            {
                Assert.IsTrue(expectedResultContent.Any(originalBuildpack => originalBuildpack.Name == bp.Name && originalBuildpack.Stack == bp.Stack));
            }
        }

        [TestMethod]
        [TestCategory("GetBuildpacks")]
        public async Task GetBuildpacksAsync_ReturnsFailedResult_WhenTokenRetrievalThrowsInvalidRefreshTokenException()
        {

            _mockCfCliService.Setup(m => m.
                GetOAuthToken())
                    .Throws(new InvalidRefreshTokenException());

            DetailedResult<List<CfBuildpack>> result = await _sut.GetBuildpacksAsync(_fakeValidTarget);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.AreEqual(null, result.CmdResult);
            Assert.AreEqual(null, result.Content);
            Assert.AreEqual(FailureType.InvalidRefreshToken, result.FailureType);
        }

        [TestMethod]
        [TestCategory("GetBuildpacks")]
        public async Task GetBuildpacksAsync_ReturnsFailedResult_WhenListBuildpacksThrowsException()
        {
            var fakeExceptionMsg = "junk";

            _mockCfCliService.Setup(m => m.
                GetOAuthToken())
                    .Returns(_fakeValidAccessToken);

            _mockCfApiClient.Setup(m => m.
                ListBuildpacks(_fakeValidTarget, _fakeValidAccessToken))
                    .Throws(new Exception(fakeExceptionMsg));

            var result = await _sut.GetBuildpacksAsync(_fakeValidTarget);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsTrue(result.Explanation.Contains(fakeExceptionMsg));
            Assert.IsNull(result.CmdResult);
            Assert.IsNull(result.Content);

            _mockLogger.Verify(m => m.Error(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("GetBuildpacks")]
        public async Task GetBuildpacksAsync_RetriesWithFreshToken_WhenListBuildpacksThrowsException()
        {
            var fakeExceptionMsg = "junk";
            Buildpack fakeBp1 = new Buildpack
            {
                Name = "Bp1",
                Stack = "StackA",
            };
            Buildpack fakeBp2 = new Buildpack
            {
                Name = "Bp2",
                Stack = "StackA",
            };
            Buildpack fakeBp3 = new Buildpack
            {
                Name = "Bp3",
                Stack = "StackZ",
            };

            var fakeBuildpacksResponse = new List<Buildpack> { fakeBp1, fakeBp2, fakeBp3 };

            var expectedResultContent = new List<CfBuildpack>
            {
                new CfBuildpack
                {
                    Name = fakeBp1.Name,
                    Stack = fakeBp1.Stack,
                },
                new CfBuildpack
                {
                    Name = fakeBp2.Name,
                    Stack = fakeBp2.Stack,
                },
                new CfBuildpack
                {
                    Name = fakeBp3.Name,
                    Stack = fakeBp3.Stack,
                },
            };

            _mockCfCliService.SetupSequence(m => m.
                GetOAuthToken())
                    .Returns(expiredAccessToken) // simulate stale cached token on first attempt
                    .Returns(_fakeValidAccessToken); // simulate fresh cached token on second attempt

            _mockCfApiClient.Setup(m => m.
                ListBuildpacks(_fakeValidTarget, expiredAccessToken))
                    .Throws(new Exception(fakeExceptionMsg));

            _mockCfApiClient.Setup(m => m.
                ListBuildpacks(_fakeValidTarget, _fakeValidAccessToken))
                    .ReturnsAsync(fakeBuildpacksResponse);

            var result = await _sut.GetBuildpacksAsync(_fakeValidTarget);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.AreEqual(null, result.CmdResult);
            Assert.AreEqual(expectedResultContent.Count, result.Content.Count);

            _mockCfCliService.Verify(m => m.ClearCachedAccessToken(), Times.Once);
            _mockCfApiClient.Verify(m => m.ListBuildpacks(_fakeValidTarget, expiredAccessToken), Times.Once);
            _mockCfApiClient.Verify(m => m.ListBuildpacks(_fakeValidTarget, _fakeValidAccessToken), Times.Once);
            _mockLogger.Verify(m => m.Information(It.Is<string>(s => s.Contains("retry")), fakeExceptionMsg, It.IsAny<int>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("GetBuildpacks")]
        public async Task GetBuildpacksAsync_ReturnsFailedResult_WhenListBuildpacksThrowsException_AndThereAreZeroRetriesLeft()
        {
            var fakeExceptionMsg = "junk";

            _mockCfCliService.Setup(m => m.
                GetOAuthToken())
                    .Returns(_fakeValidAccessToken);

            _mockCfApiClient.Setup(m => m.
                ListBuildpacks(_fakeValidTarget, _fakeValidAccessToken))
                    .Throws(new Exception(fakeExceptionMsg));

            var result = await _sut.GetBuildpacksAsync(_fakeValidTarget, retryAmount: 0);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsTrue(result.Explanation.Contains(fakeExceptionMsg));
            Assert.IsNull(result.CmdResult);
            Assert.IsNull(result.Content);

            _mockLogger.Verify(m => m.Error(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("GetBuildpacks")]
        public async Task GetBuildpacksAsync_ReturnsFailedResult_WhenTokenCannotBeFound()
        {
            _mockCfCliService.Setup(m => m.
                GetOAuthToken())
                    .Returns((string)null);

            var result = await _sut.GetBuildpacksAsync(_fakeValidTarget);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsNull(result.CmdResult);
            Assert.IsNull(result.Content);

            Assert.IsTrue(_mockCfApiClient.Invocations.Count == 0);
            _mockLogger.Verify(m => m.Error(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("StopApp")]
        public async Task StopAppAsync_ReturnsSuccessfulResult_AndUpdatesAppState()
        {
            FakeApp.State = "STARTED";

            _mockCfCliService.Setup(m => m.
                GetOAuthToken())
                    .Returns(_fakeValidAccessToken);

            _mockCfApiClient.Setup(m => m.
                StopAppWithGuid(FakeApp.ParentSpace.ParentOrg.ParentCf.ApiAddress, _fakeValidAccessToken, FakeApp.AppId))
                    .ReturnsAsync(true);

            DetailedResult result = await _sut.StopAppAsync(FakeApp);

            Assert.AreEqual("STOPPED", FakeApp.State);
            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.IsNull(result.CmdResult);
        }

        [TestMethod]
        [TestCategory("StopApp")]
        public async Task StopAppAsync_ReturnsFailedResult_WhenStopAppWithGuidReturnsFalse()
        {
            _mockCfCliService.Setup(m => m.
                GetOAuthToken())
                    .Returns(_fakeValidAccessToken);

            _mockCfApiClient.Setup(m => m.
                StopAppWithGuid(FakeApp.ParentSpace.ParentOrg.ParentCf.ApiAddress, _fakeValidAccessToken, FakeApp.AppId))
                    .ReturnsAsync(false);

            DetailedResult result = await _sut.StopAppAsync(FakeApp);

            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsNull(result.CmdResult);

            _mockLogger.Verify(m => m.Error(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("StopApp")]
        public async Task StopAppAsync_ReturnsFailedResult_WhenStopAppWithGuidThrowsException()
        {
            var fakeExceptionMsg = "junk";
            var appName = FakeApp.AppName;

            _mockCfCliService.Setup(m => m.
                GetOAuthToken())
                    .Returns(_fakeValidAccessToken);

            _mockCfApiClient.Setup(m => m.
                StopAppWithGuid(FakeApp.ParentSpace.ParentOrg.ParentCf.ApiAddress, _fakeValidAccessToken, FakeApp.AppId))
                    .Throws(new Exception(fakeExceptionMsg));

            DetailedResult result = await _sut.StopAppAsync(FakeApp);

            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsTrue(result.Explanation.Contains(fakeExceptionMsg));
            Assert.IsNull(result.CmdResult);

            _mockLogger.Verify(m => m.Error(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("StopApp")]
        public async Task StopAppAsync_RetriesWithFreshToken_WhenStopAppWithGuidThrowsException()
        {
            var fakeExceptionMsg = "junk";

            _mockCfCliService.SetupSequence(m => m.
                GetOAuthToken())
                    .Returns(expiredAccessToken) // simulate stale cached token on first attempt
                    .Returns(_fakeValidAccessToken); // simulate fresh cached token on second attempt

            _mockCfApiClient.Setup(m => m.
                StopAppWithGuid(FakeApp.ParentSpace.ParentOrg.ParentCf.ApiAddress, expiredAccessToken, FakeApp.AppId))
                    .Throws(new Exception(fakeExceptionMsg));

            _mockCfApiClient.Setup(m => m.
                StopAppWithGuid(FakeApp.ParentSpace.ParentOrg.ParentCf.ApiAddress, _fakeValidAccessToken, FakeApp.AppId))
                    .ReturnsAsync(true);

            var result = await _sut.StopAppAsync(FakeApp);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.IsNull(result.CmdResult);

            _mockCfCliService.Verify(m => m.ClearCachedAccessToken(), Times.Once);
            _mockCfApiClient.Verify(m => m.StopAppWithGuid(FakeSpace.ParentOrg.ParentCf.ApiAddress, It.IsAny<string>(), FakeApp.AppId), Times.Exactly(2));
            _mockLogger.Verify(m => m.Information(It.Is<string>(s => s.Contains("retry")), It.IsAny<string>(), fakeExceptionMsg, It.IsAny<int>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("StopApp")]
        public async Task StopAppAsync_ReturnsFailedResult_WhenStopAppWithGuidThrowsException_AndThereAreZeroRetriesLeft()
        {
            var fakeExceptionMsg = "junk";

            _mockCfCliService.Setup(m => m.
                GetOAuthToken())
                    .Returns(_fakeValidAccessToken);

            _mockCfApiClient.Setup(m => m.
                StopAppWithGuid(FakeApp.ParentSpace.ParentOrg.ParentCf.ApiAddress, _fakeValidAccessToken, FakeApp.AppId))
                    .Throws(new Exception(fakeExceptionMsg));

            DetailedResult result = await _sut.StopAppAsync(FakeApp, retryAmount: 0);

            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsTrue(result.Explanation.Contains(fakeExceptionMsg));
            Assert.IsNull(result.CmdResult);

            _mockLogger.Verify(m => m.Error(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("StopApp")]
        public async Task StopAppAsync_ReturnsFailedResult_WhenTokenRetrievalThrowsInvalidRefreshTokenException()
        {
            _mockCfCliService.Setup(m => m.
                GetOAuthToken())
                    .Throws(new InvalidRefreshTokenException());

            DetailedResult result = await _sut.StopAppAsync(FakeApp, retryAmount: 0);

            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsNull(result.CmdResult);
            Assert.AreEqual(FailureType.InvalidRefreshToken, result.FailureType);
        }

        [TestMethod]
        [TestCategory("StartApp")]
        public async Task StartAppAsync_ReturnsSuccessfulResult_AndUpdatesAppState()
        {
            FakeApp.State = "STOPPED";

            _mockCfCliService.Setup(m => m.
                GetOAuthToken())
                    .Returns(_fakeValidAccessToken);

            _mockCfApiClient.Setup(m => m.
                StartAppWithGuid(FakeApp.ParentSpace.ParentOrg.ParentCf.ApiAddress, _fakeValidAccessToken, FakeApp.AppId))
                    .ReturnsAsync(true);

            DetailedResult result = await _sut.StartAppAsync(FakeApp);

            Assert.AreEqual("STARTED", FakeApp.State);
            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.IsNull(result.CmdResult);
        }

        [TestMethod]
        [TestCategory("StartApp")]
        public async Task StartAppAsync_ReturnsFailedResult_WhenStartAppWithGuidReturnsFalse()
        {
            _mockCfCliService.Setup(m => m.
                GetOAuthToken())
                    .Returns(_fakeValidAccessToken);

            _mockCfApiClient.Setup(m => m.
                StartAppWithGuid(FakeApp.ParentSpace.ParentOrg.ParentCf.ApiAddress, _fakeValidAccessToken, FakeApp.AppId))
                    .ReturnsAsync(false);

            DetailedResult result = await _sut.StartAppAsync(FakeApp);

            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsNull(result.CmdResult);

            _mockLogger.Verify(m => m.Error(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("StartApp")]
        public async Task StartAppAsync_ReturnsFailedResult_WhenStartAppWithGuidThrowsException()
        {
            var fakeExceptionMsg = "junk";

            _mockCfCliService.Setup(m => m.
                GetOAuthToken())
                    .Returns(_fakeValidAccessToken);

            _mockCfApiClient.Setup(m => m.
                StartAppWithGuid(FakeApp.ParentSpace.ParentOrg.ParentCf.ApiAddress, _fakeValidAccessToken, FakeApp.AppId))
                    .Throws(new Exception(fakeExceptionMsg));

            DetailedResult result = await _sut.StartAppAsync(FakeApp);

            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsTrue(result.Explanation.Contains(fakeExceptionMsg));
            Assert.IsNull(result.CmdResult);

            _mockLogger.Verify(m => m.Error(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("StartApp")]
        public async Task StartAppAsync_RetriesWithFreshToken_WhenStartAppWithGuidThrowsException()
        {
            var fakeExceptionMsg = "junk";

            _mockCfCliService.SetupSequence(m => m.
                GetOAuthToken())
                    .Returns(expiredAccessToken) // simulate stale cached token on first attempt
                    .Returns(_fakeValidAccessToken); // simulate fresh cached token on second attempt

            _mockCfApiClient.Setup(m => m.
                StartAppWithGuid(FakeApp.ParentSpace.ParentOrg.ParentCf.ApiAddress, expiredAccessToken, FakeApp.AppId))
                    .Throws(new Exception(fakeExceptionMsg));

            _mockCfApiClient.Setup(m => m.
                StartAppWithGuid(FakeApp.ParentSpace.ParentOrg.ParentCf.ApiAddress, _fakeValidAccessToken, FakeApp.AppId))
                    .ReturnsAsync(true);

            var result = await _sut.StartAppAsync(FakeApp);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.IsNull(result.CmdResult);

            _mockCfCliService.Verify(m => m.ClearCachedAccessToken(), Times.Once);
            _mockCfApiClient.Verify(m => m.StartAppWithGuid(FakeSpace.ParentOrg.ParentCf.ApiAddress, It.IsAny<string>(), FakeApp.AppId), Times.Exactly(2));
            _mockLogger.Verify(m => m.Information(It.Is<string>(s => s.Contains("retry")), It.IsAny<string>(), fakeExceptionMsg, It.IsAny<int>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("StartApp")]
        public async Task StartAppAsync_ReturnsFailedResult_WhenStartAppWithGuidThrowsException_AndThereAreZeroRetriesLeft()
        {
            var fakeExceptionMsg = "junk";

            _mockCfCliService.Setup(m => m.
                GetOAuthToken())
                    .Returns(_fakeValidAccessToken);

            _mockCfApiClient.Setup(m => m.
                StartAppWithGuid(FakeApp.ParentSpace.ParentOrg.ParentCf.ApiAddress, _fakeValidAccessToken, FakeApp.AppId))
                    .Throws(new Exception(fakeExceptionMsg));

            DetailedResult result = await _sut.StartAppAsync(FakeApp, retryAmount: 0);

            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsTrue(result.Explanation.Contains(fakeExceptionMsg));
            Assert.IsNull(result.CmdResult);

            _mockLogger.Verify(m => m.Error(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("StartApp")]
        public async Task StartAppAsync_ReturnsFailedResult_WhenTokenRetrievalThrowsInvalidRefreshTokenException()
        {
            _mockCfCliService.Setup(m => m.
                GetOAuthToken())
                    .Throws(new InvalidRefreshTokenException());

            DetailedResult result = await _sut.StartAppAsync(FakeApp, retryAmount: 0);

            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsNull(result.CmdResult);
            Assert.AreEqual(FailureType.InvalidRefreshToken, result.FailureType);
        }

        [TestMethod]
        [TestCategory("DeleteApp")]
        public async Task DeleteAppAsync_ReturnsSuccessfulResult_AndUpdatesAppState()
        {
            FakeApp.State = "STOPPED";

            _mockCfCliService.Setup(m => m.
                GetOAuthToken())
                    .Returns(_fakeValidAccessToken);

            _mockCfApiClient.Setup(m => m.
                DeleteAppWithGuid(FakeApp.ParentSpace.ParentOrg.ParentCf.ApiAddress, _fakeValidAccessToken, FakeApp.AppId))
                    .ReturnsAsync(true);

            DetailedResult result = await _sut.DeleteAppAsync(FakeApp);

            Assert.AreEqual("DELETED", FakeApp.State);
            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.IsNull(result.CmdResult);
        }

        [TestMethod]
        [TestCategory("DeleteApp")]
        public async Task DeleteAppAsync_ReturnsFailedResult_WhenDeleteAppWithGuidReturnsFalse()
        {
            _mockCfCliService.Setup(m => m.
                GetOAuthToken())
                    .Returns(_fakeValidAccessToken);

            _mockCfApiClient.Setup(m => m.
                DeleteAppWithGuid(FakeApp.ParentSpace.ParentOrg.ParentCf.ApiAddress, _fakeValidAccessToken, FakeApp.AppId))
                    .ReturnsAsync(false);

            DetailedResult result = await _sut.DeleteAppAsync(FakeApp);

            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsNull(result.CmdResult);

            _mockLogger.Verify(m => m.Error(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("DeleteApp")]
        public async Task DeleteAppAsync_ReturnsFailedResult_WhenDeleteAppWithGuidThrowsException()
        {
            var fakeExceptionMsg = "junk";

            _mockCfCliService.Setup(m => m.
                GetOAuthToken())
                    .Returns(_fakeValidAccessToken);

            _mockCfApiClient.Setup(m => m.
                DeleteAppWithGuid(FakeApp.ParentSpace.ParentOrg.ParentCf.ApiAddress, _fakeValidAccessToken, FakeApp.AppId))
                    .Throws(new Exception(fakeExceptionMsg));

            DetailedResult result = await _sut.DeleteAppAsync(FakeApp);

            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsTrue(result.Explanation.Contains(fakeExceptionMsg));
            Assert.IsNull(result.CmdResult);

            _mockLogger.Verify(m => m.Error(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("DeleteApp")]
        public async Task DeleteAppAsync_RetriesWithFreshToken_WhenDeleteAppWithGuidThrowsException()
        {
            var fakeExceptionMsg = "junk";

            _mockCfCliService.SetupSequence(m => m.
                GetOAuthToken())
                    .Returns(expiredAccessToken) // simulate stale cached token on first attempt
                    .Returns(_fakeValidAccessToken); // simulate fresh cached token on second attempt

            _mockCfApiClient.Setup(m => m.
                DeleteAppWithGuid(FakeApp.ParentSpace.ParentOrg.ParentCf.ApiAddress, expiredAccessToken, FakeApp.AppId))
                    .Throws(new Exception(fakeExceptionMsg));

            _mockCfApiClient.Setup(m => m.
                DeleteAppWithGuid(FakeApp.ParentSpace.ParentOrg.ParentCf.ApiAddress, _fakeValidAccessToken, FakeApp.AppId))
                    .ReturnsAsync(true);

            var result = await _sut.DeleteAppAsync(FakeApp);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.IsNull(result.CmdResult);

            _mockCfCliService.Verify(m => m.ClearCachedAccessToken(), Times.Once);
            _mockCfApiClient.Verify(m => m.DeleteAppWithGuid(FakeSpace.ParentOrg.ParentCf.ApiAddress, It.IsAny<string>(), FakeApp.AppId), Times.Exactly(2));
            _mockLogger.Verify(m => m.Information(It.Is<string>(s => s.Contains("retry")), It.IsAny<string>(), fakeExceptionMsg, It.IsAny<int>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("DeleteApp")]
        public async Task DeleteAppAsync_ReturnsFailedResult_WhenDeleteAppWithGuidThrowsException_AndThereAreZeroRetriesLeft()
        {
            var fakeExceptionMsg = "junk";

            _mockCfCliService.Setup(m => m.
                GetOAuthToken())
                    .Returns(_fakeValidAccessToken);

            _mockCfApiClient.Setup(m => m.
                DeleteAppWithGuid(FakeApp.ParentSpace.ParentOrg.ParentCf.ApiAddress, _fakeValidAccessToken, FakeApp.AppId))
                    .Throws(new Exception(fakeExceptionMsg));

            DetailedResult result = await _sut.DeleteAppAsync(FakeApp, retryAmount: 0);

            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsTrue(result.Explanation.Contains(fakeExceptionMsg));
            Assert.IsNull(result.CmdResult);

            _mockLogger.Verify(m => m.Error(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("DeleteApp")]
        public async Task DeleteAppAsync_ReturnsFailedResult_WhenTokenRetrievalThrowsInvalidRefreshTokenException()
        {
            _mockCfCliService.Setup(m => m.
                GetOAuthToken())
                    .Throws(new InvalidRefreshTokenException());

            DetailedResult result = await _sut.DeleteAppAsync(FakeApp, retryAmount: 0);

            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsNull(result.CmdResult);
            Assert.AreEqual(FailureType.InvalidRefreshToken, result.FailureType);
        }

        [TestMethod]
        [TestCategory("DeployApp")]
        public async Task DeployAppAsync_ReturnsFalseResult_WhenCfPushCommandFails()
        {
            var expectedAppName = exampleManifest.Applications[0].Name;
            var expectedProjPath = exampleManifest.Applications[0].Path;

            const string fakeFailureExplanation = "junk";
            var fakeCfPushResponse = new DetailedResult(false, fakeFailureExplanation);

            _mockFileService.Setup(mock => mock.DirContainsFiles(It.IsAny<string>())).Returns(true);

            _mockFileService.Setup(mock => mock.GetUniquePathForTempFile($"temp_manifest_{expectedAppName}")).Returns(_fakeManifestPath);

            _mockCfCliService.Setup(mock =>
                mock.PushAppAsync(_fakeManifestPath, expectedProjPath, FakeApp.ParentSpace.ParentOrg.OrgName, FakeApp.ParentSpace.SpaceName, _fakeOutCallback, _fakeErrCallback))
                    .ReturnsAsync(fakeCfPushResponse);

            DetailedResult result = await _sut.DeployAppAsync(exampleManifest, null, FakeCfInstance, FakeOrg, FakeSpace, _fakeOutCallback, _fakeErrCallback);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Explanation.Contains(fakeFailureExplanation));

            _mockFileService.Verify(m => m.DeleteFile(_fakeManifestPath), Times.Once); // ensure temp manifest was deleted
        }

        [TestMethod]
        [TestCategory("DeployApp")]
        public async Task DeployAppAsync_ReturnsFalseResult_WhenManifestCreationFails()
        {
            var fakeManifestCreationException = new Exception("bummer dude");

            var expectedAppName = exampleManifest.Applications[0].Name;
            var expectedProjPath = exampleManifest.Applications[0].Path;

            _mockFileService.Setup(mock => mock.DirContainsFiles(It.IsAny<string>())).Returns(true);

            _mockFileService.Setup(mock => mock.GetUniquePathForTempFile($"temp_manifest_{expectedAppName}")).Returns(_fakeManifestPath);

            _mockFileService.Setup(mock => mock.WriteTextToFile(_fakeManifestPath, It.IsAny<string>())).Throws(fakeManifestCreationException);

            DetailedResult result = await _sut.DeployAppAsync(exampleManifest, null, FakeCfInstance, FakeOrg, FakeSpace, _fakeOutCallback, _fakeErrCallback);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Explanation.Contains(fakeManifestCreationException.Message));
        }

        [TestMethod]
        [TestCategory("DeployApp")]
        [DataRow("windows")]
        [DataRow("cflinuxfs3")]
        [DataRow("junk")]
        public async Task DeployAppAsync_ReturnsTrueResult_WhenCfTargetAndPushCommandsSucceed(string stack)
        {
            var expectedAppName = exampleManifest.Applications[0].Name;
            var expectedProjPath = exampleManifest.Applications[0].Path;

            _mockFileService.Setup(mock => mock.DirContainsFiles(It.IsAny<string>())).Returns(true);

            _mockFileService.Setup(mock => mock.GetUniquePathForTempFile($"temp_manifest_{expectedAppName}")).Returns(_fakeManifestPath);

            _mockCfCliService.Setup(mock =>
                mock.PushAppAsync(_fakeManifestPath, expectedProjPath, FakeApp.ParentSpace.ParentOrg.OrgName, FakeApp.ParentSpace.SpaceName, _fakeOutCallback, _fakeErrCallback))
                    .ReturnsAsync(_fakeSuccessDetailedResult);

            DetailedResult result = await _sut.DeployAppAsync(exampleManifest, null, FakeCfInstance, FakeOrg, FakeSpace, _fakeOutCallback, _fakeErrCallback);

            Assert.IsTrue(result.Succeeded);

            _mockFileService.Verify(m => m.DeleteFile(_fakeManifestPath), Times.Once); // ensure temp manifest was deleted
        }

        [TestMethod]
        [TestCategory("DeployApp")]
        public async Task DeployAppAsync_ReturnsFalseResult_WhenProjDirContainsNoFiles()
        {
            _mockFileService.Setup(mock => mock.DirContainsFiles(It.IsAny<string>())).Returns(false);

            DetailedResult result = await _sut.DeployAppAsync(exampleManifest, null, FakeCfInstance, FakeOrg, FakeSpace, _fakeOutCallback, _fakeErrCallback);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Explanation.Contains(CloudFoundryService.EmptyOutputDirMessage));

            // ensure temp manifest was never created
            _mockFileService.Verify(mock => mock.GetUniquePathForTempFile(It.IsAny<string>()), Times.Never);
            _mockFileService.Verify(mock => mock.WriteTextToFile(_fakeManifestPath, It.IsAny<string>()), Times.Never);

            _mockFileService.VerifyAll();

        }

        [TestMethod]
        [TestCategory("DeployApp")]
        public async Task DeployAppAsync_ReturnsFailedResult_WhenCfCliDeploymentThrowsInvalidRefreshTokenException()
        {
            var expectedAppName = exampleManifest.Applications[0].Name;
            var expectedProjPath = exampleManifest.Applications[0].Path;

            _mockFileService.Setup(mock => mock.DirContainsFiles(It.IsAny<string>())).Returns(true);

            _mockFileService.Setup(mock => mock.GetUniquePathForTempFile($"temp_manifest_{expectedAppName}")).Returns(_fakeManifestPath);

            _mockCfCliService.Setup(mock => mock.
                PushAppAsync(_fakeManifestPath, expectedProjPath, FakeOrg.OrgName, FakeSpace.SpaceName, _fakeOutCallback, _fakeErrCallback))
                    .Throws(new InvalidRefreshTokenException());

            DetailedResult result = await _sut.DeployAppAsync(exampleManifest, null, FakeCfInstance, FakeOrg, FakeSpace, _fakeOutCallback, _fakeErrCallback);

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
                Applications = new List<AppConfig>
                {
                    new AppConfig(),
                }
            };

            var defaultAppPath = @"fake\app\path";

            Assert.IsNull(appManifest.Applications[0].Path); // ensure manifest app path null

            _mockFileService.Setup(mock => mock.DirContainsFiles(It.IsAny<string>())).Returns(true);

            _mockFileService.Setup(mock => mock.GetUniquePathForTempFile(It.IsAny<string>())).Returns(_fakeManifestPath);

            _mockCfCliService.Setup(mock =>
                mock.PushAppAsync(_fakeManifestPath, defaultAppPath, FakeApp.ParentSpace.ParentOrg.OrgName, FakeApp.ParentSpace.SpaceName, _fakeOutCallback, _fakeErrCallback))
                    .ReturnsAsync(_fakeSuccessDetailedResult);

            DetailedResult result = await _sut.DeployAppAsync(exampleManifest, defaultAppPath, FakeCfInstance, FakeOrg, FakeSpace, _fakeOutCallback, _fakeErrCallback);

            _mockCfCliService.Verify(mock =>
               mock.PushAppAsync(_fakeManifestPath, defaultAppPath, FakeApp.ParentSpace.ParentOrg.OrgName, FakeApp.ParentSpace.SpaceName, _fakeOutCallback, _fakeErrCallback));
        }

        [TestMethod]
        [TestCategory("DeployApp")]
        public async Task DeployAppAsync_UsesManifestAppPath_WhenManifestAppPathIsNotNull()
        {
            var appManifest = new AppManifest()
            {
                Applications = new List<AppConfig>
                {
                    new AppConfig()
                    {
                        Path = @"manifest\app\path",
                    },
                }
            };

            var defaultAppPath = @"fake\app\path";
            var manifestAppPath = appManifest.Applications[0].Path;

            Assert.IsNotNull(appManifest.Applications[0].Path); // ensure manifest app path is not null

            _mockFileService.Setup(mock => mock.DirContainsFiles(It.IsAny<string>())).Returns(true);

            _mockFileService.Setup(mock => mock.GetUniquePathForTempFile(It.IsAny<string>())).Returns(_fakeManifestPath);

            _mockCfCliService.Setup(mock =>
                mock.PushAppAsync(_fakeManifestPath, manifestAppPath, FakeApp.ParentSpace.ParentOrg.OrgName, FakeApp.ParentSpace.SpaceName, _fakeOutCallback, _fakeErrCallback))
                    .ReturnsAsync(_fakeSuccessDetailedResult);

            DetailedResult result = await _sut.DeployAppAsync(appManifest, defaultAppPath, FakeCfInstance, FakeOrg, FakeSpace, _fakeOutCallback, _fakeErrCallback);

            _mockCfCliService.Verify(mock =>
               mock.PushAppAsync(_fakeManifestPath, manifestAppPath, FakeApp.ParentSpace.ParentOrg.OrgName, FakeApp.ParentSpace.SpaceName, _fakeOutCallback, _fakeErrCallback));
        }

        [TestMethod]
        [TestCategory("GetRecentLogs")]
        public async Task GetRecentLogs_ReturnsSuccessResult_WhenWrappedMethodSucceeds()
        {
            var logsStub = "These are fake app logs!\n[12:16:04] App took a nap.";
            var fakeLogsResult = new DetailedResult<string>(logsStub, true, null, _fakeSuccessCmdResult);

            _mockCfCliService.Setup(m => m
                .GetRecentAppLogs(FakeApp.AppName, FakeOrg.OrgName, FakeSpace.SpaceName))
                    .ReturnsAsync(fakeLogsResult);

            var result = await _sut.GetRecentLogs(FakeApp);

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
                .GetRecentAppLogs(FakeApp.AppName, FakeOrg.OrgName, FakeSpace.SpaceName))
                    .ReturnsAsync(fakeLogsResult);

            var result = await _sut.GetRecentLogs(FakeApp);

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
                .GetRecentAppLogs(FakeApp.AppName, FakeOrg.OrgName, FakeSpace.SpaceName))
                    .Throws(new InvalidRefreshTokenException());

            var result = await _sut.GetRecentLogs(FakeApp);

            Assert.IsNull(result.Content);
            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Explanation.Contains("Unable to retrieve app logs"));
            Assert.IsTrue(result.Explanation.Contains(FakeApp.AppName));
            Assert.IsTrue(result.Explanation.Contains("Please log back in to re-authenticate"));
            Assert.AreEqual(FailureType.InvalidRefreshToken, result.FailureType);
        }

        [TestMethod]
        [TestCategory("CreateManifestFile")]
        public void CreateManifestFile_ReturnsSuccessfulResult_WhenFileCreatedAtGivenPath()
        {
            string pathToFileCreation = "some//junk//path";
            string fakeManifestContent = "some yaml";
            string expectedWriteStr = $"---\n{fakeManifestContent}";

            _mockSerializationService.Setup(m => m.SerializeCfAppManifest(exampleManifest))
                .Returns(fakeManifestContent);

            _mockFileService.Setup(m => m.WriteTextToFile(pathToFileCreation, expectedWriteStr))
                .Verifiable();

            var result = _sut.CreateManifestFile(pathToFileCreation, exampleManifest);

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
            string pathToFileCreation = "some//junk//path";
            string fakeExceptionMsg = "Couldn't create file because... we want to simulate an exception :)";
            var fileCreationException = new Exception(fakeExceptionMsg);

            _mockFileService.Setup(m => m.WriteTextToFile(pathToFileCreation, It.IsAny<string>())).Throws(fileCreationException);

            var result = _sut.CreateManifestFile(pathToFileCreation, exampleManifest);

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
            _mockCfCliService.Setup(m => m.
                GetOAuthToken())
                    .Returns((string)null);

            var result = await _sut.GetStackNamesAsync(FakeCfInstance);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsNull(result.CmdResult);
            Assert.IsNull(result.Content);

            Assert.IsTrue(_mockCfApiClient.Invocations.Count == 0);
            _mockLogger.Verify(m => m.Error(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("GetStacks")]
        public async Task GetStackNamesAsync_RetriesWithFreshToken_WhenListStacksThrowsException()
        {
            var fakeExceptionMsg = "junk";
            var fakeStacksResponse = new List<CloudFoundryApiClient.Models.StacksResponse.Stack>
            {
                new CloudFoundryApiClient.Models.StacksResponse.Stack
                {
                    Name = _stack1Name,
                    Guid = _stack1Guid,
                },
                new CloudFoundryApiClient.Models.StacksResponse.Stack
                {
                    Name = _stack2Name,
                    Guid = _stack2Guid,
                },
            };

            var expectedResultContent = new List<string>
            {
                _stack1Name,
                _stack2Name,
            };

            _mockCfCliService.SetupSequence(m => m.
                GetOAuthToken())
                    .Returns(expiredAccessToken) // simulate stale cached token on first attempt
                    .Returns(_fakeValidAccessToken); // simulate fresh cached token on second attempt

            _mockCfApiClient.Setup(m => m.
                ListStacks(FakeCfInstance.ApiAddress, expiredAccessToken))
                    .Throws(new Exception(fakeExceptionMsg));

            _mockCfApiClient.Setup(m => m.
                ListStacks(FakeCfInstance.ApiAddress, _fakeValidAccessToken))
                    .ReturnsAsync(fakeStacksResponse);

            var result = await _sut.GetStackNamesAsync(FakeCfInstance);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.AreEqual(null, result.CmdResult);
            Assert.AreEqual(expectedResultContent.Count, result.Content.Count);

            _mockCfCliService.Verify(m => m.ClearCachedAccessToken(), Times.Once);
            _mockCfApiClient.Verify(m => m.ListStacks(FakeCfInstance.ApiAddress, It.IsAny<string>()), Times.Exactly(2));
            _mockLogger.Verify(m => m.Information(It.Is<string>(s => s.Contains("retry")), fakeExceptionMsg, It.IsAny<int>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("GetStacks")]
        public async Task GetStackNamesAsync_ReturnsFailedResult_WhenListStacksThrowsException_AndThereAreZeroRetriesLeft()
        {
            var fakeExceptionMsg = "junk";

            _mockCfCliService.Setup(m => m.
                GetOAuthToken())
                    .Returns(_fakeValidAccessToken);

            _mockCfApiClient.Setup(m => m.
                ListStacks(FakeCfInstance.ApiAddress, _fakeValidAccessToken))
                    .Throws(new Exception(fakeExceptionMsg));

            var result = await _sut.GetStackNamesAsync(FakeCfInstance, retryAmount: 0);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsTrue(result.Explanation.Contains(fakeExceptionMsg));
            Assert.IsNull(result.CmdResult);
            Assert.IsNull(result.Content);

            _mockLogger.Verify(m => m.Error(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("GetStacks")]
        public async Task GetStackNamesAsync_ReturnsSuccessfulResult_WhenListStacksSucceeds()
        {
            var fakeStacksResponse = new List<CloudFoundryApiClient.Models.StacksResponse.Stack>
            {
                new CloudFoundryApiClient.Models.StacksResponse.Stack
                {
                    Name = _stack1Name,
                    Guid = _stack1Guid,
                },
                new CloudFoundryApiClient.Models.StacksResponse.Stack
                {
                    Name = _stack2Name,
                    Guid = _stack2Guid,
                },
                new CloudFoundryApiClient.Models.StacksResponse.Stack
                {
                    Name = _stack3Name,
                    Guid = _stack3Guid,
                },
                new CloudFoundryApiClient.Models.StacksResponse.Stack
                {
                    Name = _stack4Name,
                    Guid = _stack4Guid,
                },
            };

            var expectedResultContent = new List<string>
            {
                _stack1Name,
                _stack2Name,
                _stack3Name,
                _stack4Name,
            };

            _mockCfCliService.Setup(m => m.
                GetOAuthToken())
                    .Returns(_fakeValidAccessToken);

            _mockCfApiClient.Setup(m => m.
                ListStacks(FakeCfInstance.ApiAddress, _fakeValidAccessToken))
                    .ReturnsAsync(fakeStacksResponse);

            DetailedResult<List<string>> result = await _sut.GetStackNamesAsync(FakeCfInstance);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.AreEqual(null, result.CmdResult);
            Assert.AreEqual(expectedResultContent.Count, result.Content.Count);

            for (int i = 0; i < expectedResultContent.Count; i++)
            {
                Assert.AreEqual(expectedResultContent[i], result.Content[i]);
            }
        }

        [TestMethod]
        [TestCategory("GetStacks")]
        public async Task GetStackNamesAsync_ReturnsFailedResult_WhenTokenRetrievalThrowsInvalidRefreshTokenException()
        {
            _mockCfCliService.Setup(m => m.
                GetOAuthToken())
                    .Throws(new InvalidRefreshTokenException());

            DetailedResult<List<string>> result = await _sut.GetStackNamesAsync(FakeCfInstance);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.AreEqual(null, result.CmdResult);
            Assert.AreEqual(null, result.Content);
            Assert.AreEqual(FailureType.InvalidRefreshToken, result.FailureType);
        }

        [TestMethod]
        [TestCategory("GetSsoPrompt")]
        public async Task GetSsoPrompt_ReturnsSuccessfulResult_WhenLoginServerInfoRequestSucceeds()
        {
            var fakePasscode = "fake sso passcode";
            var fakeLoginInfoResponse = new LoginInfoResponse
            {
                Prompts = new Dictionary<string, string[]>
                {
                    { CloudFoundryService.CfApiSsoPromptKey, new[] {"fake content type", fakePasscode}}
                }
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
            var fakeLoginInfoResponse = new LoginInfoResponse
            {
                Prompts = new Dictionary<string, string[]>
                {
                    { "some irrelevant key", new[] {"fake content type", "some content"}}
                }
            };

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
        [TestCategory("StreamAppLogs")]
        [DynamicData(nameof(GetAppLogStreamArgs), DynamicDataSourceType.Method)]
        public void StreamAppLogs_ReturnsSuccessfulResult_WhenCfCliServiceSucceeds(CloudFoundryApp app, Action<string> stdOutDel, Action<string> stdErrDel)
        {
            var expectedAppName = app.AppName;
            var expectedSpaceName = app.ParentSpace.SpaceName;
            var expectedOrgName = app.ParentSpace.ParentOrg.OrgName;
            var fakeLogStreamProcess = new Process();
            var fakeSuccessResponse = new DetailedResult<Process>
            {
                Succeeded = true,
                Content = fakeLogStreamProcess,
            };

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
            var fakeFailedResponse = new DetailedResult<Process>
            {
                Succeeded = false,
                Explanation = ":(",
            };
            _mockCfCliService.Setup(m => m.StreamAppLogs(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Action<string>>(), It.IsAny<Action<string>>())).Returns(fakeFailedResponse);

            var result = _sut.StreamAppLogs(FakeApp, _fakeOutCallback, _fakeErrCallback);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(result.Explanation, fakeFailedResponse.Explanation);
        }

        [TestMethod]
        [TestCategory("StreamAppLogs")]
        public void StreamAppLogs_ReturnsFailedResult_WhenCfCliServiceThrowsException()
        {
            var fakeException = new Exception(":)");
            _mockCfCliService.Setup(m => m.StreamAppLogs(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Action<string>>(), It.IsAny<Action<string>>())).Throws(fakeException);

            var result = _sut.StreamAppLogs(FakeApp, _fakeOutCallback, _fakeErrCallback);
            
            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(result.Explanation, fakeException.Message);
        }

        private static IEnumerable<object[]> GetAppLogStreamArgs()
        {
            CloudFoundryOrganization FakeOrg2 = new CloudFoundryOrganization("fake org 2", "fake org guid 2", null);
            CloudFoundrySpace FakeSpace2 = new CloudFoundrySpace("fake space 2", "fake space guid 2", FakeOrg2);
            CloudFoundryApp FakeApp2 = new CloudFoundryApp("fake app 2", "fake app guid 2", FakeSpace2, null);
            Action<string> fakeOutCallback = (string s) => { };
            Action<string> fakeOutCallback2 = (string s) => { };
            Action<string> fakeErrCallback = (string s) => { };
            Action<string> fakeErrCallback2 = (string s) => { };

            yield return new object[] { FakeApp, fakeOutCallback, fakeErrCallback };
            yield return new object[] { FakeApp2, fakeOutCallback2, fakeErrCallback2 };
        }
    }
}