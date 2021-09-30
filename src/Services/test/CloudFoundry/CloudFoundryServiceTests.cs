using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Serilog;
using Tanzu.Toolkit.CloudFoundryApiClient;
using Tanzu.Toolkit.Models;
using Tanzu.Toolkit.Services.CfCli;
using Tanzu.Toolkit.Services.CloudFoundry;
using Tanzu.Toolkit.Services.CommandProcess;
using Tanzu.Toolkit.Services.CommandProcess;
using Tanzu.Toolkit.Services.Dialog;
using Tanzu.Toolkit.Services.ErrorDialog;
using Tanzu.Toolkit.Services.FileLocator;
using Tanzu.Toolkit.Services.Logging;
using static Tanzu.Toolkit.Services.OutputHandler.OutputHandler;

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
        private Mock<IFileLocatorService> _mockFileLocatorService;
        private Mock<ILoggingService> _mockLoggingService;
        private Mock<ILogger> _mockLogger;

        private readonly bool _defaultFullFWFlag = false;

        [TestInitialize]
        public void TestInit()
        {
            var serviceCollection = new ServiceCollection();
            _mockCfApiClient = new Mock<ICfApiClient>();
            _mockCfCliService = new Mock<ICfCliService>();
            _mockErrorDialogWindowService = new Mock<IErrorDialog>();
            _mockCommandProcessService = new Mock<ICommandProcessService>();
            _mockFileLocatorService = new Mock<IFileLocatorService>();
            _mockLoggingService = new Mock<ILoggingService>();

            _mockLogger = new Mock<ILogger>();
            _mockLoggingService.SetupGet(m => m.Logger).Returns(_mockLogger.Object);

            serviceCollection.AddSingleton(_mockCfApiClient.Object);
            serviceCollection.AddSingleton(_mockCfCliService.Object);
            serviceCollection.AddSingleton(_mockErrorDialogWindowService.Object);
            serviceCollection.AddSingleton(_mockCommandProcessService.Object);
            serviceCollection.AddSingleton(_mockFileLocatorService.Object);
            serviceCollection.AddSingleton(_mockLoggingService.Object);

            _services = serviceCollection.BuildServiceProvider();

            _sut = new CloudFoundryService(_services);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _mockCfApiClient.VerifyAll();
            _mockCfCliService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("ConnectToCfAsync")]
        public async Task ConnectToCFAsync_ThrowsExceptions_WhenParametersAreInvalid()
        {
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => _sut.ConnectToCFAsync(null, null, null, null, false));
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => _sut.ConnectToCFAsync(string.Empty, null, null, null, false));
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => _sut.ConnectToCFAsync("Junk", null, null, null, false));
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => _sut.ConnectToCFAsync("Junk", string.Empty, null, null, false));
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => _sut.ConnectToCFAsync("Junk", "Junk", null, null, false));
        }

        [TestMethod]
        [TestCategory("ConnectToCfAsync")]
        public async Task ConnectToCFAsync_ReturnsConnectResult_WhenLoginSucceeds()
        {
            _mockCfCliService.Setup(mock => mock.
                TargetApi(_fakeValidTarget, true))
                    .Returns(new DetailedResult(true, null, _fakeSuccessCmdResult));

            _mockCfCliService.Setup(mock => mock.
                AuthenticateAsync(_fakeValidUsername, _fakeValidPassword))
                    .ReturnsAsync(new DetailedResult(true, null, _fakeSuccessCmdResult));

            ConnectResult result = await _sut.ConnectToCFAsync(_fakeValidTarget, _fakeValidUsername, _fakeValidPassword, _fakeHttpProxy, _skipSsl);

            Assert.IsTrue(result.IsLoggedIn);
            Assert.IsNull(result.ErrorMessage);
            _mockCfCliService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("ConnectToCfAsync")]
        public async Task ConnectToCFAsync_ReturnsConnectResult_WhenLoginFails_BecuaseTargetApiFails()
        {
            _mockCfCliService.Setup(mock => mock.TargetApi(_fakeValidTarget, true))
                .Returns(new DetailedResult(false, "fake failure message", _fakeFailureCmdResult));

            ConnectResult result = await _sut.ConnectToCFAsync(_fakeValidTarget, _fakeValidUsername, _fakeValidPassword, _fakeHttpProxy, _skipSsl);

            Assert.IsFalse(result.IsLoggedIn);
            Assert.IsTrue(result.ErrorMessage.Contains(CloudFoundryService.LoginFailureMessage));
            _mockCfCliService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("ConnectToCfAsync")]
        public async Task ConnectToCFAsync_ReturnsConnectResult_WhenLoginFails_BecuaseAuthenticateFails()
        {
            _mockCfCliService.Setup(mock => mock.TargetApi(_fakeValidTarget, true))
                .Returns(new DetailedResult(true, null, _fakeSuccessCmdResult));
            _mockCfCliService.Setup(mock => mock.AuthenticateAsync(_fakeValidUsername, _fakeValidPassword))
                .ReturnsAsync(new DetailedResult(false, "fake failure message", _fakeFailureCmdResult));

            ConnectResult result = await _sut.ConnectToCFAsync(_fakeValidTarget, _fakeValidUsername, _fakeValidPassword, _fakeHttpProxy, _skipSsl);

            Assert.IsFalse(result.IsLoggedIn);
            Assert.IsTrue(result.ErrorMessage.Contains(CloudFoundryService.LoginFailureMessage));
            _mockCfCliService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("ConnectToCfAsync")]
        public async Task ConnectToCFAsync_ReturnsConnectResult_WhenLoginFails_DueToMissingCmdResult()
        {
            DetailedResult cfExeMissingResult = new DetailedResult(true, "we couldn't find cf.exe", null);

            _mockCfCliService.Setup(mock => mock.
                TargetApi(_fakeValidTarget, true))
                    .Returns(cfExeMissingResult);

            ConnectResult result = await _sut.ConnectToCFAsync(_fakeValidTarget, _fakeValidUsername, _fakeValidPassword, _fakeHttpProxy, _skipSsl);

            Assert.IsFalse(result.IsLoggedIn);
            Assert.IsTrue(result.ErrorMessage.Contains(CloudFoundryService.LoginFailureMessage));
            _mockCfCliService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("ConnectToCfAsync")]
        public async Task ConnectToCFAsync_ReturnsConnectResult_WhenLoginFails_BecuaseAuthResultCmdDetailsAreNull()
        {
            _mockCfCliService.Setup(mock => mock.TargetApi(_fakeValidTarget, true))
                .Returns(new DetailedResult(true, null, _fakeSuccessCmdResult));
            _mockCfCliService.Setup(mock => mock.AuthenticateAsync(_fakeValidUsername, _fakeValidPassword))
                .ReturnsAsync(new DetailedResult(false, "fake failure message", null));

            ConnectResult result = await _sut.ConnectToCFAsync(_fakeValidTarget, _fakeValidUsername, _fakeValidPassword, _fakeHttpProxy, _skipSsl);

            Assert.IsFalse(result.IsLoggedIn);
            Assert.IsTrue(result.ErrorMessage.Contains(CloudFoundryService.LoginFailureMessage));
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

            ConnectResult result = await _sut.ConnectToCFAsync(_fakeValidTarget, _fakeValidUsername, _fakeValidPassword, _fakeHttpProxy, _skipSsl);

            Assert.IsTrue(result.ErrorMessage.Contains(baseMessage));
            Assert.IsTrue(result.ErrorMessage.Contains(innerMessage));
            Assert.IsTrue(result.ErrorMessage.Contains(outerMessage));
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

            var result = await _sut.ConnectToCFAsync(_fakeValidTarget, _fakeValidUsername, _fakeValidPassword, _fakeHttpProxy, _skipSsl);

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

            _mockFileLocatorService.SetupSet(m => m.
                CliVersion = expectedCliVersion).Verifiable();

            ConnectResult result = await _sut.ConnectToCFAsync(_fakeValidTarget, _fakeValidUsername, _fakeValidPassword, _fakeHttpProxy, _skipSsl);

            _mockFileLocatorService.VerifyAll();
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

                _mockFileLocatorService.SetupSet(m => m.
                    CliVersion = expectedCliVersion).Verifiable();

                ConnectResult result = await _sut.ConnectToCFAsync(_fakeValidTarget, _fakeValidUsername, _fakeValidPassword, _fakeHttpProxy, _skipSsl);

                _mockFileLocatorService.VerifyAll();
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

                _mockFileLocatorService.SetupSet(m => m.
                    CliVersion = expectedCliVersion).Verifiable();

                ConnectResult result = await _sut.ConnectToCFAsync(_fakeValidTarget, _fakeValidUsername, _fakeValidPassword, _fakeHttpProxy, _skipSsl);

                _mockFileLocatorService.VerifyAll();
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

                _mockFileLocatorService.SetupSet(m => m.
                    CliVersion = expectedCliVersion).Verifiable();

                ConnectResult result = await _sut.ConnectToCFAsync(_fakeValidTarget, _fakeValidUsername, _fakeValidPassword, _fakeHttpProxy, _skipSsl);

                _mockFileLocatorService.VerifyAll();
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

                _mockFileLocatorService.SetupSet(m => m.
                    CliVersion = expectedCliVersion).Verifiable();

                ConnectResult result = await _sut.ConnectToCFAsync(_fakeValidTarget, _fakeValidUsername, _fakeValidPassword, _fakeHttpProxy, _skipSsl);

                _mockFileLocatorService.VerifyAll();
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
            const string fakeFailureExplanation = "junk";
            var fakeCfPushResponse = new DetailedResult(false, fakeFailureExplanation);

            _mockCfCliService.Setup(mock =>
                mock.PushAppAsync(FakeApp.AppName, FakeApp.ParentSpace.ParentOrg.OrgName, FakeApp.ParentSpace.SpaceName, null, null, _fakeProjectPath, null, null, null, null))
                    .ReturnsAsync(fakeCfPushResponse);

            _mockFileLocatorService.Setup(mock => mock.DirContainsFiles(It.IsAny<string>())).Returns(true);

            DetailedResult result = await _sut.DeployAppAsync(FakeCfInstance, FakeOrg, FakeSpace, FakeApp.AppName, _fakeProjectPath, _defaultFullFWFlag, stdOutCallback: null, stdErrCallback: null, stack: null, binaryDeployment: false, projectName: null, manifestPath: null);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Explanation.Contains(fakeFailureExplanation));
        }

        [TestMethod]
        [TestCategory("DeployApp")]
        [DataRow("windows")]
        [DataRow("cflinuxfs3")]
        [DataRow("junk")]
        public async Task DeployAppAsync_ReturnsTrueResult_WhenCfTargetAndPushCommandsSucceed(string stack)
        {
            _mockCfCliService.Setup(mock =>
                mock.PushAppAsync(FakeApp.AppName, FakeApp.ParentSpace.ParentOrg.OrgName, FakeApp.ParentSpace.SpaceName, null, null, _fakeProjectPath, null, stack, null, null))
                    .ReturnsAsync(_fakeSuccessDetailedResult);

            _mockFileLocatorService.Setup(mock => mock.DirContainsFiles(It.IsAny<string>())).Returns(true);

            DetailedResult result = await _sut.DeployAppAsync(FakeCfInstance, FakeOrg, FakeSpace, FakeApp.AppName, _fakeProjectPath, _defaultFullFWFlag, stdOutCallback: null, stdErrCallback: null, stack: stack, binaryDeployment: false, projectName: null, manifestPath: null);

            Assert.IsTrue(result.Succeeded);
        }

        [TestMethod]
        [TestCategory("DeployApp")]
        public async Task DeployAppAsync_SpecifiesHWCBuildpack_AndWindowsStack_WhenFullFWDeploymentIsTrue()
        {
            string expectedBuildpackValue = "hwc_buildpack";
            string expectedStackValue = "windows";

            _mockCfCliService.Setup(mock =>
                mock.PushAppAsync(FakeApp.AppName, FakeApp.ParentSpace.ParentOrg.OrgName, FakeApp.ParentSpace.SpaceName, null, null, _fakeProjectPath, expectedBuildpackValue, expectedStackValue, null, null))
                    .ReturnsAsync(_fakeSuccessDetailedResult);

            _mockFileLocatorService.Setup(mock => mock.DirContainsFiles(It.IsAny<string>())).Returns(true);

            bool fullFWIndicator = true;
            DetailedResult result = await _sut.DeployAppAsync(FakeCfInstance, FakeOrg, FakeSpace, FakeApp.AppName, _fakeProjectPath, fullFWIndicator, stdOutCallback: null, stdErrCallback: null, stack: null, binaryDeployment: false, projectName: null, manifestPath: null);

            Assert.IsTrue(result.Succeeded);
        }

        [TestMethod]
        [TestCategory("DeployApp")]
        public async Task DeployAppAsync_ReturnsFalseResult_WhenProjDirContainsNoFiles()
        {
            _mockFileLocatorService.Setup(mock => mock.DirContainsFiles(It.IsAny<string>())).Returns(false);

            var result = await _sut.DeployAppAsync(FakeCfInstance, FakeOrg, FakeSpace, FakeApp.AppName, _fakeProjectPath, _defaultFullFWFlag, stdOutCallback: null, stdErrCallback: null, stack: null, binaryDeployment: true, projectName: null, manifestPath: null);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Explanation.Contains(CloudFoundryService.EmptyOutputDirMessage));
            _mockFileLocatorService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("DeployApp")]
        public async Task DeployAppAsync_ReturnsFailedResult_WhenCfCliDeploymentThrowsInvalidRefreshTokenException()
        {
            _mockFileLocatorService.Setup(mock => mock.DirContainsFiles(It.IsAny<string>())).Returns(true);

            _mockCfCliService.Setup(mock => mock.
                PushAppAsync(FakeApp.AppName, FakeOrg.OrgName, FakeSpace.SpaceName, It.IsAny<StdOutDelegate>(), It.IsAny<StdErrDelegate>(), _fakeProjectPath, It.IsAny<string>(), It.IsAny<string>(), null, null))
                    .Throws(new InvalidRefreshTokenException());

            var result = await _sut.DeployAppAsync(FakeCfInstance, FakeOrg, FakeSpace, FakeApp.AppName, _fakeProjectPath, _defaultFullFWFlag, stdOutCallback: null, stdErrCallback: null, stack: null, binaryDeployment: false, projectName: null, manifestPath: null);

            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.AreEqual(FailureType.InvalidRefreshToken, result.FailureType);
        }

        [TestMethod]
        [TestCategory("DeployApp")]
        public async Task DeployAppAsync_SpecifiesBuildpack_AndStartCommand_WhenBinaryDeploymentIsTrue()
        {
            _mockFileLocatorService.Setup(mock => mock.DirContainsFiles(It.IsAny<string>())).Returns(true);

            var expectedProjectName = "junk proj name";
            var expectedBuildpack = "binary_buildpack";
            var expectedStartCommand = $"cmd /c .\\{expectedProjectName} --urls=http://*:%PORT%";

            _mockCfCliService.Setup(mock =>
                mock.PushAppAsync(FakeApp.AppName,
                                  FakeApp.ParentSpace.ParentOrg.OrgName,
                                  FakeApp.ParentSpace.SpaceName,
                                  It.IsAny<StdOutDelegate>(),
                                  It.IsAny<StdErrDelegate>(),
                                  _fakeProjectPath,
                                  expectedBuildpack,
                                  It.IsAny<string>(),
                                  expectedStartCommand,
                                  null))
                    .ReturnsAsync(_fakeSuccessDetailedResult);

            await _sut.DeployAppAsync(FakeCfInstance,
                                      FakeOrg,
                                      FakeSpace,
                                      FakeApp.AppName,
                                      _fakeProjectPath,
                                      _defaultFullFWFlag,
                                      null,
                                      null,
                                      manifestPath: null,
                                      binaryDeployment: true,
                                      projectName: expectedProjectName,
                                      stack: "windows");

            _mockCfCliService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("DeployApp")]
        public async Task DeployAppAsync_SpecifiesDotNetCoreBuildpack_WhenBinaryDeploymentIsTrue_AndStackIsLinux()
        {
            _mockFileLocatorService.Setup(mock => mock.DirContainsFiles(It.IsAny<string>())).Returns(true);

            var expectedProjectName = "junk proj name";
            var expectedBuildpack = "dotnet_core_buildpack";
            string expectedStartCommand = null;

            _mockCfCliService.Setup(mock =>
                mock.PushAppAsync(FakeApp.AppName,
                                  FakeApp.ParentSpace.ParentOrg.OrgName,
                                  FakeApp.ParentSpace.SpaceName,
                                  It.IsAny<StdOutDelegate>(),
                                  It.IsAny<StdErrDelegate>(),
                                  _fakeProjectPath,
                                  expectedBuildpack,
                                  It.IsAny<string>(),
                                  expectedStartCommand,
                                  null))
                    .ReturnsAsync(_fakeSuccessDetailedResult);

            await _sut.DeployAppAsync(FakeCfInstance,
                                      FakeOrg,
                                      FakeSpace,
                                      FakeApp.AppName,
                                      _fakeProjectPath,
                                      _defaultFullFWFlag,
                                      null,
                                      null,
                                      manifestPath: null,
                                      binaryDeployment: true,
                                      projectName: expectedProjectName,
                                      stack: "cflinuxfs3");

            _mockCfCliService.VerifyAll();
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
    }
}
