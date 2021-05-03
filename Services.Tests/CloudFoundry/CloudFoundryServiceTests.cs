using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tanzu.Toolkit.VisualStudio.Models;
using Tanzu.Toolkit.VisualStudio.Services.CloudFoundry;
using Tanzu.Toolkit.VisualStudio.Services.CmdProcess;
using static Tanzu.Toolkit.VisualStudio.Services.OutputHandler.OutputHandler;

namespace Tanzu.Toolkit.VisualStudio.Services.Tests.CloudFoundry
{
    [TestClass()]
    public class CloudFoundryServiceTests : ServicesTestSupport
    {
        [TestInitialize()]
        public void TestInit()
        {
            cfService = new CloudFoundryService(services);
            fakeCfInstance = new CloudFoundryInstance("fake cf", fakeValidTarget, fakeValidAccessToken);
            fakeOrg = new CloudFoundryOrganization("fake org", "fake org guid", fakeCfInstance);
            fakeSpace = new CloudFoundrySpace("fake space", "fake space guid", fakeOrg);
            fakeApp = new CloudFoundryApp("fake app", "fake app guid", fakeSpace, null);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            mockCfApiClient.VerifyAll();
            mockCfCliService.VerifyAll();
        }


        [TestMethod]
        [TestCategory("ConnectToCfAsync")]
        public async Task ConnectToCFAsync_ThrowsExceptions_WhenParametersAreInvalid()
        {
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => cfService.ConnectToCFAsync(null, null, null, null, false));
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => cfService.ConnectToCFAsync(string.Empty, null, null, null, false));
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => cfService.ConnectToCFAsync("Junk", null, null, null, false));
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => cfService.ConnectToCFAsync("Junk", string.Empty, null, null, false));
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => cfService.ConnectToCFAsync("Junk", "Junk", null, null, false));
        }

        [TestMethod]
        [TestCategory("ConnectToCfAsync")]
        public async Task ConnectToCFAsync_ReturnsConnectResult_WhenLoginSucceeds()
        {
            mockCfCliService.Setup(mock => mock.TargetApi(fakeValidTarget, true))
                .Returns(new DetailedResult(true, null, fakeSuccessCmdResult));
            mockCfCliService.Setup(mock => mock.GetOAuthToken()).Returns(fakeValidAccessToken);
            mockCfCliService.Setup(mock => mock.AuthenticateAsync(fakeValidUsername, fakeValidPassword))
                .ReturnsAsync(new DetailedResult(true, null, fakeSuccessCmdResult));

            ConnectResult result = await cfService.ConnectToCFAsync(fakeValidTarget, fakeValidUsername, fakeValidPassword, fakeHttpProxy, skipSsl);

            Assert.IsTrue(result.IsLoggedIn);
            Assert.IsNull(result.ErrorMessage);
            Assert.AreEqual(fakeValidAccessToken, result.Token);
            mockCfCliService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("ConnectToCfAsync")]
        public async Task ConnectToCFAsync_ReturnsConnectResult_WhenLoginFails_BecuaseTargetApiFails()
        {
            mockCfCliService.Setup(mock => mock.TargetApi(fakeValidTarget, true))
                .Returns(new DetailedResult(false, "fake failure message", fakeFailureCmdResult));

            ConnectResult result = await cfService.ConnectToCFAsync(fakeValidTarget, fakeValidUsername, fakeValidPassword, fakeHttpProxy, skipSsl);

            Assert.IsFalse(result.IsLoggedIn);
            Assert.IsTrue(result.ErrorMessage.Contains(cfService.LoginFailureMessage));
            Assert.IsNull(result.Token);
            mockCfCliService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("ConnectToCfAsync")]
        public async Task ConnectToCFAsync_ReturnsConnectResult_WhenLoginFails_BecuaseAuthenticateFails()
        {
            mockCfCliService.Setup(mock => mock.TargetApi(fakeValidTarget, true))
                .Returns(new DetailedResult(true, null, fakeSuccessCmdResult));
            mockCfCliService.Setup(mock => mock.AuthenticateAsync(fakeValidUsername, fakeValidPassword))
                .ReturnsAsync(new DetailedResult(false, "fake failure message", fakeFailureCmdResult));

            ConnectResult result = await cfService.ConnectToCFAsync(fakeValidTarget, fakeValidUsername, fakeValidPassword, fakeHttpProxy, skipSsl);

            Assert.IsFalse(result.IsLoggedIn);
            Assert.IsTrue(result.ErrorMessage.Contains(cfService.LoginFailureMessage));
            Assert.IsNull(result.Token);
            mockCfCliService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("ConnectToCfAsync")]
        public async Task ConnectToCFAsync_ReturnsConnectResult_WhenLoginFails_DueToMissingCmdResult()
        {
            DetailedResult cfExeMissingResult = new DetailedResult(true, "we couldn't find cf.exe", null);

            mockCfCliService.Setup(mock => mock.
                TargetApi(fakeValidTarget, true))
                    .Returns(cfExeMissingResult);

            ConnectResult result = await cfService.ConnectToCFAsync(fakeValidTarget, fakeValidUsername, fakeValidPassword, fakeHttpProxy, skipSsl);

            Assert.IsFalse(result.IsLoggedIn);
            Assert.IsTrue(result.ErrorMessage.Contains(cfService.LoginFailureMessage));
            Assert.IsNull(result.Token);
            mockCfCliService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("ConnectToCfAsync")]
        public async Task ConnectToCFAsync_ReturnsConnectResult_WhenLoginFails_BecuaseAuthResultCmdDetailsAreNull()
        {
            mockCfCliService.Setup(mock => mock.TargetApi(fakeValidTarget, true))
                .Returns(new DetailedResult(true, null, fakeSuccessCmdResult));
            mockCfCliService.Setup(mock => mock.AuthenticateAsync(fakeValidUsername, fakeValidPassword))
                .ReturnsAsync(new DetailedResult(false, "fake failure message", null));

            ConnectResult result = await cfService.ConnectToCFAsync(fakeValidTarget, fakeValidUsername, fakeValidPassword, fakeHttpProxy, skipSsl);

            Assert.IsFalse(result.IsLoggedIn);
            Assert.IsTrue(result.ErrorMessage.Contains(cfService.LoginFailureMessage));
            Assert.IsNull(result.Token);
            mockCfCliService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("ConnectToCfAsync")]
        public async Task ConnectToCFAsync_ReturnsConnectResult_WhenLoginFails_BecuaseOfInvalidOAuthToken()
        {
            string unacquiredToken = null;
            mockCfCliService.Setup(mock => mock.TargetApi(fakeValidTarget, true))
                .Returns(new DetailedResult(true, null, fakeSuccessCmdResult));
            mockCfCliService.Setup(mock => mock.AuthenticateAsync(fakeValidUsername, fakeValidPassword))
                .ReturnsAsync(new DetailedResult(true, null, fakeSuccessCmdResult));
            mockCfCliService.Setup(mock => mock.GetOAuthToken()).Returns(unacquiredToken);

            ConnectResult result = await cfService.ConnectToCFAsync(fakeValidTarget, fakeValidUsername, fakeValidPassword, fakeHttpProxy, skipSsl);

            Assert.IsFalse(result.IsLoggedIn);
            Assert.IsTrue(result.ErrorMessage.Contains(cfService.LoginFailureMessage));
            Assert.IsNull(result.Token);
            mockCfCliService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("ConnectToCfAsync")]
        public async Task ConnectToCfAsync_IncludesNestedExceptionMessages_WhenExceptionIsThrown()
        {
            string baseMessage = "base exception message";
            string innerMessage = "inner exception message";
            string outerMessage = "outer exception message";
            Exception multilayeredException = new Exception(outerMessage, new Exception(innerMessage, new Exception(baseMessage)));

            mockCfCliService.Setup(mock => mock.TargetApi(fakeValidTarget, true))
                .Returns(new DetailedResult(true, null, fakeSuccessCmdResult));
            mockCfCliService.Setup(mock => mock.AuthenticateAsync(fakeValidUsername, fakeValidPassword))
                .ReturnsAsync(new DetailedResult(true, null, fakeSuccessCmdResult));
            mockCfCliService.Setup(mock => mock.GetOAuthToken()).Throws(multilayeredException);

            ConnectResult result = await cfService.ConnectToCFAsync(fakeValidTarget, fakeValidUsername, fakeValidPassword, fakeHttpProxy, skipSsl);

            Assert.IsNull(result.Token);
            Assert.IsTrue(result.ErrorMessage.Contains(baseMessage));
            Assert.IsTrue(result.ErrorMessage.Contains(innerMessage));
            Assert.IsTrue(result.ErrorMessage.Contains(outerMessage));
            mockCfCliService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("ConnectToCfAsync")]
        public async Task ConnectToCfAsync_InvokesCfApiAndCfAuthCommands()
        {
            var cfApiArgs = $"api {fakeValidTarget}{(skipSsl ? " --skip-ssl-validation" : string.Empty)}";
            var fakeCfApiResponse = new DetailedResult(true, null, new CmdResult(null, null, 0));
            var fakePasswordStr = new System.Net.NetworkCredential(string.Empty, fakeValidPassword).Password;
            var cfAuthArgs = $"auth {fakeValidUsername} {fakePasswordStr}";
            var fakeCfAuthResponse = new DetailedResult(true, null, new CmdResult(null, null, 0));

            mockCfCliService.Setup(mock => mock.
              TargetApi(fakeValidTarget, It.IsAny<bool>()))
                .Returns(fakeCfApiResponse);

            mockCfCliService.Setup(mock => mock.
              AuthenticateAsync(fakeValidUsername, fakeValidPassword))
                .ReturnsAsync(fakeCfAuthResponse);

            var result = await cfService.ConnectToCFAsync(fakeValidTarget, fakeValidUsername, fakeValidPassword, fakeHttpProxy, skipSsl);

            mockCfCliService.VerifyAll();
        }



        [TestMethod]
        [TestCategory("MatchCliVersionToApiVersion")]
        public async Task MatchCliVersionToApiVersion_SetsCliVersionTo7_AndRaisesErrorDialog_WhenApiVersionCouldNotBeDetected()
        {
            mockCfCliService.Setup(mock => mock.
                TargetApi(fakeValidTarget, true))
                    .Returns(new DetailedResult(true, null, fakeSuccessCmdResult));

            mockCfCliService.Setup(mock => mock.
                GetOAuthToken()).Returns(fakeValidAccessToken);

            mockCfCliService.Setup(mock => mock.
                AuthenticateAsync(fakeValidUsername, fakeValidPassword))
                    .ReturnsAsync(new DetailedResult(true, null, fakeSuccessCmdResult));

            int expectedCliVersion = 7;

            mockCfCliService.Setup(m => m.
                GetApiVersion()).ReturnsAsync((Version)null);

            mockFileLocatorService.SetupSet(m => m.
                CliVersion = expectedCliVersion).Verifiable();

            ConnectResult result = await cfService.ConnectToCFAsync(fakeValidTarget, fakeValidUsername, fakeValidPassword, fakeHttpProxy, skipSsl);

            mockFileLocatorService.VerifyAll();
            mockCfCliService.VerifyAll();
            mockDialogService.Verify(m => m.
                DisplayErrorDialog(CloudFoundryService.ccApiVersionUndetectableErrTitle, CloudFoundryService.ccApiVersionUndetectableErrMsg),
                    Times.Once);
        }

        [TestMethod]
        [TestCategory("MatchCliVersionToApiVersion")]
        public async Task MatchCliVersionToApiVersion_SetsCliVersionTo6_WhenApiMajorVersionIs2_AndBetweenMinors_128_149()
        {
            mockCfCliService.Setup(mock => mock.
                TargetApi(fakeValidTarget, true))
                    .Returns(new DetailedResult(true, null, fakeSuccessCmdResult));

            mockCfCliService.Setup(mock => mock.
                GetOAuthToken()).Returns(fakeValidAccessToken);

            mockCfCliService.Setup(mock => mock.
                AuthenticateAsync(fakeValidUsername, fakeValidPassword))
                    .ReturnsAsync(new DetailedResult(true, null, fakeSuccessCmdResult));

            int expectedCliVersion = 6;
            Version[] versionInputs = new Version[]
            {
                new Version(major: 2, minor: 149, build: 0),
                new Version(major: 2, minor: 138, build: 0),
                new Version(major: 2, minor: 128, build: 0),
            };

            foreach (Version mockApiVersion in versionInputs)
            {
                mockCfCliService.Setup(m => m.
                    GetApiVersion()).ReturnsAsync(mockApiVersion);

                mockFileLocatorService.SetupSet(m => m.
                    CliVersion = expectedCliVersion).Verifiable();

                ConnectResult result = await cfService.ConnectToCFAsync(fakeValidTarget, fakeValidUsername, fakeValidPassword, fakeHttpProxy, skipSsl);

                mockFileLocatorService.VerifyAll();
                mockCfCliService.VerifyAll();
            }
        }

        [TestMethod]
        [TestCategory("MatchCliVersionToApiVersion")]
        public async Task MatchCliVersionToApiVersion_SetsCliVersionTo6_WhenApiMajorVersionIs3_AndBetweenMinors_63_84()
        {
            mockCfCliService.Setup(mock => mock.
                TargetApi(fakeValidTarget, true))
                    .Returns(new DetailedResult(true, null, fakeSuccessCmdResult));

            mockCfCliService.Setup(mock => mock.
                GetOAuthToken()).Returns(fakeValidAccessToken);

            mockCfCliService.Setup(mock => mock.
                AuthenticateAsync(fakeValidUsername, fakeValidPassword))
                    .ReturnsAsync(new DetailedResult(true, null, fakeSuccessCmdResult));

            int expectedCliVersion = 6;
            Version[] versionInputs = new Version[]
            {
                new Version(major: 3, minor: 84, build: 0),
                new Version(major: 3, minor: 73, build: 0),
                new Version(major: 3, minor: 63, build: 0),
            };

            foreach (Version mockApiVersion in versionInputs)
            {
                mockCfCliService.Setup(m => m.
                    GetApiVersion()).ReturnsAsync(mockApiVersion);

                mockFileLocatorService.SetupSet(m => m.
                    CliVersion = expectedCliVersion).Verifiable();

                ConnectResult result = await cfService.ConnectToCFAsync(fakeValidTarget, fakeValidUsername, fakeValidPassword, fakeHttpProxy, skipSsl);

                mockFileLocatorService.VerifyAll();
                mockCfCliService.VerifyAll();
            }
        }

        [TestMethod]
        [TestCategory("MatchCliVersionToApiVersion")]
        public async Task MatchCliVersionToApiVersion_SetsCliVersionTo7_WhenApiMajorVersionIs2_AndAtOrAboveMinorVersion_150()
        {
            mockCfCliService.Setup(mock => mock.
                TargetApi(fakeValidTarget, true))
                    .Returns(new DetailedResult(true, null, fakeSuccessCmdResult));

            mockCfCliService.Setup(mock => mock.
                GetOAuthToken()).Returns(fakeValidAccessToken);

            mockCfCliService.Setup(mock => mock.
                AuthenticateAsync(fakeValidUsername, fakeValidPassword))
                    .ReturnsAsync(new DetailedResult(true, null, fakeSuccessCmdResult));

            int expectedCliVersion = 7;
            Version[] versionInputs = new Version[]
            {
                new Version(major: 2, minor: 150, build: 0),
                new Version(major: 2, minor: 189, build: 0),
                new Version(major: 2, minor: 150, build: 1),
            };

            foreach (Version mockApiVersion in versionInputs)
            {
                mockCfCliService.Setup(m => m.
                    GetApiVersion()).ReturnsAsync(mockApiVersion);

                mockFileLocatorService.SetupSet(m => m.
                    CliVersion = expectedCliVersion).Verifiable();

                ConnectResult result = await cfService.ConnectToCFAsync(fakeValidTarget, fakeValidUsername, fakeValidPassword, fakeHttpProxy, skipSsl);

                mockFileLocatorService.VerifyAll();
                mockCfCliService.VerifyAll();
            }
        }

        [TestMethod]
        [TestCategory("MatchCliVersionToApiVersion")]
        public async Task MatchCliVersionToApiVersion_SetsCliVersionTo7_WhenApiMajorVersionIs3_AndAtOrAboveMinorVersion_85()
        {
            mockCfCliService.Setup(mock => mock.
                TargetApi(fakeValidTarget, true))
                    .Returns(new DetailedResult(true, null, fakeSuccessCmdResult));

            mockCfCliService.Setup(mock => mock.
                GetOAuthToken()).Returns(fakeValidAccessToken);

            mockCfCliService.Setup(mock => mock.
                AuthenticateAsync(fakeValidUsername, fakeValidPassword))
                    .ReturnsAsync(new DetailedResult(true, null, fakeSuccessCmdResult));

            int expectedCliVersion = 7;
            Version[] versionInputs = new Version[]
            {
                new Version(major: 3, minor: 85, build: 0),
                new Version(major: 3, minor: 178, build: 0),
                new Version(major: 3, minor: 85, build: 1),
            };

            foreach (Version mockApiVersion in versionInputs)
            {
                mockCfCliService.Setup(m => m.
                    GetApiVersion()).ReturnsAsync(mockApiVersion);

                mockFileLocatorService.SetupSet(m => m.
                    CliVersion = expectedCliVersion).Verifiable();

                ConnectResult result = await cfService.ConnectToCFAsync(fakeValidTarget, fakeValidUsername, fakeValidPassword, fakeHttpProxy, skipSsl);

                mockFileLocatorService.VerifyAll();
                mockCfCliService.VerifyAll();
            }
        }



        [TestMethod]
        [TestCategory("GetOrgs")]
        public async Task GetOrgsForCfInstanceAsync_ReturnsFailedResult_WhenTokenCannotBeFound()
        {
            mockCfCliService.Setup(m => m.
                GetOAuthToken())
                    .Returns((string)null);

            var result = await cfService.GetOrgsForCfInstanceAsync(fakeCfInstance);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsNull(result.CmdDetails);
            Assert.IsNull(result.Content);

            Assert.IsTrue(mockCfApiClient.Invocations.Count == 0);
            mockLogger.Verify(m => m.Error(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("GetOrgs")]
        public async Task GetOrgsForCfInstanceAsync_ReturnsFailedResult_WhenListOrgsThrowsException()
        {
            var fakeExceptionMsg = "junk";
            
            mockCfCliService.Setup(m => m.
                GetOAuthToken())
                    .Returns(fakeValidAccessToken);

            mockCfApiClient.Setup(m => m.
                ListOrgs(fakeCfInstance.ApiAddress, fakeValidAccessToken))
                    .Throws(new Exception(fakeExceptionMsg));

            var result = await cfService.GetOrgsForCfInstanceAsync(fakeCfInstance);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsTrue(result.Explanation.Contains(fakeExceptionMsg));
            Assert.IsNull(result.CmdDetails);
            Assert.IsNull(result.Content);

            mockLogger.Verify(m => m.Error(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("GetOrgs")]
        public async Task GetOrgsForCfInstanceAsync_ReturnsSuccessfulResult_WhenListOrgsSucceeds()
        {
            var fakeOrgsResponse = new List<CloudFoundryApiClient.Models.OrgsResponse.Org>
            {
                new CloudFoundryApiClient.Models.OrgsResponse.Org
                {
                    Name = org1Name,
                    Guid = org1Guid,
                },
                new CloudFoundryApiClient.Models.OrgsResponse.Org
                {
                    Name = org2Name,
                    Guid = org2Guid,
                },
                new CloudFoundryApiClient.Models.OrgsResponse.Org
                {
                    Name = org3Name,
                    Guid = org3Guid,
                },
                new CloudFoundryApiClient.Models.OrgsResponse.Org
                {
                    Name = org4Name,
                    Guid = org4Guid,
                },
            };

            var expectedResultContent = new List<CloudFoundryOrganization>
            {
                new CloudFoundryOrganization(org1Name, org1Guid, fakeCfInstance),
                new CloudFoundryOrganization(org2Name, org2Guid, fakeCfInstance),
                new CloudFoundryOrganization(org3Name, org3Guid, fakeCfInstance),
                new CloudFoundryOrganization(org4Name, org4Guid, fakeCfInstance)
            };
            
            mockCfCliService.Setup(m => m.
                GetOAuthToken())
                    .Returns(fakeValidAccessToken);

            mockCfApiClient.Setup(m => m.
                ListOrgs(fakeCfInstance.ApiAddress, fakeValidAccessToken))
                    .ReturnsAsync(fakeOrgsResponse);

            DetailedResult<List<CloudFoundryOrganization>> result = await cfService.GetOrgsForCfInstanceAsync(fakeCfInstance);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.AreEqual(null, result.CmdDetails);
            Assert.AreEqual(expectedResultContent.Count, result.Content.Count);

            for (int i = 0; i < expectedResultContent.Count; i++)
            {
                Assert.AreEqual(expectedResultContent[i].OrgId, result.Content[i].OrgId);
                Assert.AreEqual(expectedResultContent[i].OrgName, result.Content[i].OrgName);
                Assert.AreEqual(expectedResultContent[i].ParentCf, result.Content[i].ParentCf);
            }
        }

        

        [TestMethod]
        [TestCategory("GetSpaces")]
        public async Task GetSpacesForOrgAsync_ReturnsFailedResult_WhenTokenCannotBeFound()
        {
            mockCfCliService.Setup(m => m.
                GetOAuthToken())
                    .Returns((string)null);

            var result = await cfService.GetSpacesForOrgAsync(fakeOrg);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsNull(result.CmdDetails);
            Assert.IsNull(result.Content);

            Assert.IsTrue(mockCfApiClient.Invocations.Count == 0);
            mockLogger.Verify(m => m.Error(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("GetSpaces")]
        public async Task GetSpacesForOrgAsync_ReturnsFailedResult_WhenListSpacesForOrgThrowsException()
        {
            var fakeExceptionMsg = "junk";
            
            mockCfCliService.Setup(m => m.
                GetOAuthToken())
                    .Returns(fakeValidAccessToken);

            mockCfApiClient.Setup(m => m.
                ListSpacesForOrg(fakeOrg.ParentCf.ApiAddress, fakeValidAccessToken, fakeOrg.OrgId))
                    .Throws(new Exception(fakeExceptionMsg));

            var result = await cfService.GetSpacesForOrgAsync(fakeOrg);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsTrue(result.Explanation.Contains(fakeExceptionMsg));
            Assert.IsNull(result.CmdDetails);
            Assert.IsNull(result.Content);

            mockLogger.Verify(m => m.Error(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("GetSpaces")]
        public async Task GetSpacesForOrgAsync_ReturnsSuccessfulResult_WhenListSpacesSucceeds()
        {
            var fakeSpacesResponse = new List<CloudFoundryApiClient.Models.SpacesResponse.Space>
            {
                new CloudFoundryApiClient.Models.SpacesResponse.Space
                {
                    Name = space1Name,
                    Guid = space1Guid,
                },
                new CloudFoundryApiClient.Models.SpacesResponse.Space
                {
                    Name = space2Name,
                    Guid = space2Guid,
                },
                new CloudFoundryApiClient.Models.SpacesResponse.Space
                {
                    Name = space3Name,
                    Guid = space3Guid,
                },
                new CloudFoundryApiClient.Models.SpacesResponse.Space
                {
                    Name = space4Name,
                    Guid = space4Guid,
                },
            };

            var expectedResultContent = new List<CloudFoundrySpace>
            {
                new CloudFoundrySpace(space1Name, space1Guid, fakeOrg),
                new CloudFoundrySpace(space2Name, space2Guid, fakeOrg),
                new CloudFoundrySpace(space3Name, space3Guid, fakeOrg),
                new CloudFoundrySpace(space4Name, space4Guid, fakeOrg)
            };
            
            mockCfCliService.Setup(m => m.
                GetOAuthToken())
                    .Returns(fakeValidAccessToken);

            mockCfApiClient.Setup(m => m.
                ListSpacesForOrg(fakeOrg.ParentCf.ApiAddress, fakeValidAccessToken, fakeOrg.OrgId))
                    .ReturnsAsync(fakeSpacesResponse);

            DetailedResult<List<CloudFoundrySpace>> result = await cfService.GetSpacesForOrgAsync(fakeOrg);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.AreEqual(null, result.CmdDetails);
            Assert.AreEqual(expectedResultContent.Count, result.Content.Count);

            for (int i = 0; i < expectedResultContent.Count; i++)
            {
                Assert.AreEqual(expectedResultContent[i].SpaceId, result.Content[i].SpaceId);
                Assert.AreEqual(expectedResultContent[i].SpaceName, result.Content[i].SpaceName);
                Assert.AreEqual(expectedResultContent[i].ParentOrg, result.Content[i].ParentOrg);
            }
        }



        [TestMethod]
        [TestCategory("GetApps")]
        public async Task GetAppsForSpaceAsync_ReturnsFailedResult_WhenTokenCannotBeFound()
        {
            mockCfCliService.Setup(m => m.
                GetOAuthToken())
                    .Returns((string)null);

            var result = await cfService.GetAppsForSpaceAsync(fakeSpace);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsNull(result.CmdDetails);
            Assert.IsNull(result.Content);

            Assert.IsTrue(mockCfApiClient.Invocations.Count == 0);
            mockLogger.Verify(m => m.Error(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("GetApps")]
        public async Task GetAppsForSpaceAsync_ReturnsFailedResult_WhenListAppsForSpaceThrowsException()
        {
            var fakeExceptionMsg = "junk";
            
            mockCfCliService.Setup(m => m.
                GetOAuthToken())
                    .Returns(fakeValidAccessToken);

            mockCfApiClient.Setup(m => m.
                ListAppsForSpace(fakeSpace.ParentOrg.ParentCf.ApiAddress, fakeValidAccessToken, fakeSpace.SpaceId))
                    .Throws(new Exception(fakeExceptionMsg));

            var result = await cfService.GetAppsForSpaceAsync(fakeSpace);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsTrue(result.Explanation.Contains(fakeExceptionMsg));
            Assert.IsNull(result.CmdDetails);
            Assert.IsNull(result.Content);

            mockLogger.Verify(m => m.Error(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("GetApps")]
        public async Task GetAppsForSpaceAsync_ReturnsSuccessfulResult_WhenListAppsSucceeds()
        {
            var fakeAppsResponse = new List<CloudFoundryApiClient.Models.AppsResponse.App>
            {
                new CloudFoundryApiClient.Models.AppsResponse.App
                {
                    Name = app1Name,
                    Guid = app1Guid,
                    State = app1State,
                },
                new CloudFoundryApiClient.Models.AppsResponse.App
                {
                    Name = app2Name,
                    Guid = app2Guid,
                    State = app2State,
                },
                new CloudFoundryApiClient.Models.AppsResponse.App
                {
                    Name = app3Name,
                    Guid = app3Guid,
                    State = app3State,
                },
                new CloudFoundryApiClient.Models.AppsResponse.App
                {
                    Name = app4Name,
                    Guid = app4Guid,
                    State = app4State,
                },
            };

            var expectedResultContent = new List<CloudFoundryApp>
            {
                new CloudFoundryApp(app1Name, app1Guid, fakeSpace, app1State),
                new CloudFoundryApp(app2Name, app2Guid, fakeSpace, app2State),
                new CloudFoundryApp(app3Name, app3Guid, fakeSpace, app3State),
                new CloudFoundryApp(app4Name, app4Guid, fakeSpace, app4State)
            };
            
            mockCfCliService.Setup(m => m.
                GetOAuthToken())
                    .Returns(fakeValidAccessToken);

            mockCfApiClient.Setup(m => m.
                ListAppsForSpace(fakeSpace.ParentOrg.ParentCf.ApiAddress, fakeValidAccessToken, fakeSpace.SpaceId))
                    .ReturnsAsync(fakeAppsResponse);

            DetailedResult<List<CloudFoundryApp>> result = await cfService.GetAppsForSpaceAsync(fakeSpace);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.AreEqual(null, result.CmdDetails);
            Assert.AreEqual(expectedResultContent.Count, result.Content.Count);

            for (int i = 0; i < expectedResultContent.Count; i++)
            {
                Assert.AreEqual(expectedResultContent[i].AppId, result.Content[i].AppId);
                Assert.AreEqual(expectedResultContent[i].AppName, result.Content[i].AppName);
                Assert.AreEqual(expectedResultContent[i].ParentSpace, result.Content[i].ParentSpace);
            }
        }



        [TestMethod]
        [TestCategory("AddCloudFoundryInstance")]
        public void AddCloudFoundryInstance_ThrowsException_WhenNameAlreadyExists()
        {
            var duplicateName = "fake name";
            cfService.AddCloudFoundryInstance(duplicateName, null, null);
            Exception expectedException = null;

            try
            {
                cfService.AddCloudFoundryInstance(duplicateName, null, null);

            }
            catch (Exception e)
            {
                expectedException = e;
            }

            Assert.IsNotNull(expectedException);
            Assert.IsTrue(expectedException.Message.Contains(duplicateName));
            Assert.IsTrue(expectedException.Message.Contains("already exists"));
        }


        [TestMethod]
        [TestCategory("RemoveCloudFoundryInstance")]
        public void RemoveCloudFoundryInstance_RemovesItemFromDictionary()
        {
            var sut = new CloudFoundryService(services)
            {
                CloudFoundryInstances = new Dictionary<string, CloudFoundryInstance>
                {
                    { fakeCfInstance.InstanceName, fakeCfInstance }
                }
            };

            Assert.AreEqual(1, sut.CloudFoundryInstances.Count);

            sut.RemoveCloudFoundryInstance(fakeCfInstance.InstanceName);

            Assert.AreEqual(0, sut.CloudFoundryInstances.Count);
        }

        [TestMethod]
        [TestCategory("RemoveCloudFoundryInstance")]
        public void RemoveCloudFoundryInstance_DoesNothing_WhenItemNotInDictionary()
        {
            var sut = new CloudFoundryService(services)
            {
                CloudFoundryInstances = new Dictionary<string, CloudFoundryInstance>
                {
                    { fakeCfInstance.InstanceName, fakeCfInstance }
                }
            };

            Assert.AreEqual(1, sut.CloudFoundryInstances.Count);

            sut.RemoveCloudFoundryInstance("nonexistent item");

            Assert.AreEqual(1, sut.CloudFoundryInstances.Count);
        }


        [TestMethod]
        [TestCategory("StopApp")]
        public async Task StopAppAsync_ReturnsSuccessfulResult_AndUpdatesAppState()
        {
            fakeApp.State = "STARTED";

            mockCfCliService.Setup(m => m.
                GetOAuthToken())
                    .Returns(fakeValidAccessToken);

            mockCfApiClient.Setup(m => m.
                StopAppWithGuid(fakeApp.ParentSpace.ParentOrg.ParentCf.ApiAddress, fakeValidAccessToken, fakeApp.AppId))
                    .ReturnsAsync(true);

            DetailedResult result = await cfService.StopAppAsync(fakeApp);

            Assert.AreEqual("STOPPED", fakeApp.State);
            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.IsNull(result.CmdDetails);
        }

        [TestMethod]
        [TestCategory("StopApp")]
        public async Task StopAppAsync_ReturnsFailedResult_WhenStopAppWithGuidReturnsFalse()
        {
            mockCfCliService.Setup(m => m.
                GetOAuthToken())
                    .Returns(fakeValidAccessToken);

            mockCfApiClient.Setup(m => m.
                StopAppWithGuid(fakeApp.ParentSpace.ParentOrg.ParentCf.ApiAddress, fakeValidAccessToken, fakeApp.AppId))
                    .ReturnsAsync(false);

            DetailedResult result = await cfService.StopAppAsync(fakeApp);

            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsNull(result.CmdDetails);

            mockLogger.Verify(m => m.Error(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("StopApp")]
        public async Task StopAppAsync_ReturnsFailedResult_WhenStopAppWithGuidThrowsException()
        {
            var fakeExceptionMsg = "junk";

            mockCfCliService.Setup(m => m.
                GetOAuthToken())
                    .Returns(fakeValidAccessToken);

            mockCfApiClient.Setup(m => m.
                StopAppWithGuid(fakeApp.ParentSpace.ParentOrg.ParentCf.ApiAddress, fakeValidAccessToken, fakeApp.AppId))
                    .Throws(new Exception(fakeExceptionMsg));

            DetailedResult result = await cfService.StopAppAsync(fakeApp);

            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsTrue(result.Explanation.Contains(fakeExceptionMsg));
            Assert.IsNull(result.CmdDetails);

            mockLogger.Verify(m => m.Error(It.IsAny<string>()), Times.Once);
        }
        

        [TestMethod]
        [TestCategory("StartApp")]
        public async Task StartAppAsync_ReturnsSuccessfulResult_AndUpdatesAppState()
        {
            fakeApp.State = "STOPPED";

            mockCfCliService.Setup(m => m.
                GetOAuthToken())
                    .Returns(fakeValidAccessToken);

            mockCfApiClient.Setup(m => m.
                StartAppWithGuid(fakeApp.ParentSpace.ParentOrg.ParentCf.ApiAddress, fakeValidAccessToken, fakeApp.AppId))
                    .ReturnsAsync(true);

            DetailedResult result = await cfService.StartAppAsync(fakeApp);

            Assert.AreEqual("STARTED", fakeApp.State);
            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.IsNull(result.CmdDetails);
        }

        [TestMethod]
        [TestCategory("StartApp")]
        public async Task StartAppAsync_ReturnsFailedResult_WhenStartAppWithGuidReturnsFalse()
        {
            mockCfCliService.Setup(m => m.
                GetOAuthToken())
                    .Returns(fakeValidAccessToken);

            mockCfApiClient.Setup(m => m.
                StartAppWithGuid(fakeApp.ParentSpace.ParentOrg.ParentCf.ApiAddress, fakeValidAccessToken, fakeApp.AppId))
                    .ReturnsAsync(false);

            DetailedResult result = await cfService.StartAppAsync(fakeApp);

            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsNull(result.CmdDetails);

            mockLogger.Verify(m => m.Error(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("StartApp")]
        public async Task StartAppAsync_ReturnsFailedResult_WhenStartAppWithGuidThrowsException()
        {
            var fakeExceptionMsg = "junk";

            mockCfCliService.Setup(m => m.
                GetOAuthToken())
                    .Returns(fakeValidAccessToken);

            mockCfApiClient.Setup(m => m.
                StartAppWithGuid(fakeApp.ParentSpace.ParentOrg.ParentCf.ApiAddress, fakeValidAccessToken, fakeApp.AppId))
                    .Throws(new Exception(fakeExceptionMsg));

            DetailedResult result = await cfService.StartAppAsync(fakeApp);

            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsTrue(result.Explanation.Contains(fakeExceptionMsg));
            Assert.IsNull(result.CmdDetails);

            mockLogger.Verify(m => m.Error(It.IsAny<string>()), Times.Once);
        }
        

        [TestMethod]
        [TestCategory("DeleteApp")]
        public async Task DeleteAppAsync_ReturnsSuccessfulResult_AndUpdatesAppState()
        {
            fakeApp.State = "STOPPED";

            mockCfCliService.Setup(m => m.
                GetOAuthToken())
                    .Returns(fakeValidAccessToken);

            mockCfApiClient.Setup(m => m.
                DeleteAppWithGuid(fakeApp.ParentSpace.ParentOrg.ParentCf.ApiAddress, fakeValidAccessToken, fakeApp.AppId))
                    .ReturnsAsync(true);

            DetailedResult result = await cfService.DeleteAppAsync(fakeApp);

            Assert.AreEqual("DELETED", fakeApp.State);
            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.IsNull(result.CmdDetails);
        }

        [TestMethod]
        [TestCategory("DeleteApp")]
        public async Task DeleteAppAsync_ReturnsFailedResult_WhenDeleteAppWithGuidReturnsFalse()
        {
            mockCfCliService.Setup(m => m.
                GetOAuthToken())
                    .Returns(fakeValidAccessToken);

            mockCfApiClient.Setup(m => m.
                DeleteAppWithGuid(fakeApp.ParentSpace.ParentOrg.ParentCf.ApiAddress, fakeValidAccessToken, fakeApp.AppId))
                    .ReturnsAsync(false);

            DetailedResult result = await cfService.DeleteAppAsync(fakeApp);

            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsNull(result.CmdDetails);

            mockLogger.Verify(m => m.Error(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("DeleteApp")]
        public async Task DeleteAppAsync_ReturnsFailedResult_WhenDeleteAppWithGuidThrowsException()
        {
            var fakeExceptionMsg = "junk";

            mockCfCliService.Setup(m => m.
                GetOAuthToken())
                    .Returns(fakeValidAccessToken);

            mockCfApiClient.Setup(m => m.
                DeleteAppWithGuid(fakeApp.ParentSpace.ParentOrg.ParentCf.ApiAddress, fakeValidAccessToken, fakeApp.AppId))
                    .Throws(new Exception(fakeExceptionMsg));

            DetailedResult result = await cfService.DeleteAppAsync(fakeApp);

            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Explanation);
            Assert.IsTrue(result.Explanation.Contains(fakeExceptionMsg));
            Assert.IsNull(result.CmdDetails);

            mockLogger.Verify(m => m.Error(It.IsAny<string>()), Times.Once);
        }


        [TestMethod]
        [TestCategory("DeployApp")]
        public async Task DeployAppAsync_ReturnsFalseResult_WhenCfTargetCommandFails()
        {
            const string fakeFailureExplanation = "cf target failed";
            var fakeCfCmdResponse = new DetailedResult(false, fakeFailureExplanation);

            mockCfCliService.Setup(mock =>
                mock.InvokeCfCliAsync(It.IsAny<string>(), It.IsAny<StdOutDelegate>(), It.IsAny<StdErrDelegate>(), It.IsAny<string>()))
                    .ReturnsAsync(fakeCfCmdResponse);

            mockFileLocatorService.Setup(mock => mock.DirContainsFiles(It.IsAny<string>())).Returns(true);

            DetailedResult result = await cfService.DeployAppAsync(fakeCfInstance, fakeOrg, fakeSpace, fakeApp.AppName, fakeProjectPath, stdOutCallback: null, stdErrCallback: null);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Explanation.Contains(fakeFailureExplanation));

            var cfTargetArgs = $"target -o {fakeOrg.OrgName} -s {fakeSpace.SpaceName}";
            mockCfCliService.Verify(mock =>
                mock.InvokeCfCliAsync(cfTargetArgs, null, null, null),
                    Times.Once);
        }

        [TestMethod]
        [TestCategory("DeployApp")]
        public async Task DeployAppAsync_ReturnsFalseResult_WhenCfPushCommandFails()
        {
            const string fakeFailureExplanation = "cf push failed";
            var cfTargetArgs = $"target -o {fakeOrg.OrgName} -s {fakeSpace.SpaceName}";

            var fakeCfTargetResponse = new DetailedResult(true);
            var fakeCfPushResponse = new DetailedResult(false, fakeFailureExplanation);

            mockCfCliService.Setup(mock =>
                mock.InvokeCfCliAsync(cfTargetArgs, It.IsAny<StdOutDelegate>(), It.IsAny<StdErrDelegate>(), It.IsAny<string>()))
                    .ReturnsAsync(fakeCfTargetResponse);

            mockCfCliService.Setup(mock =>
                mock.PushAppAsync(fakeApp.AppName, It.IsAny<StdOutDelegate>(), It.IsAny<StdErrDelegate>(), It.IsAny<string>()))
                    .ReturnsAsync(fakeCfPushResponse);

            mockFileLocatorService.Setup(mock => mock.DirContainsFiles(It.IsAny<string>())).Returns(true);

            DetailedResult result = await cfService.DeployAppAsync(fakeCfInstance, fakeOrg, fakeSpace, fakeApp.AppName, fakeProjectPath, stdOutCallback: null, stdErrCallback: null);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Explanation.Contains(fakeFailureExplanation));

            mockCfCliService.Verify(mock =>
                mock.InvokeCfCliAsync(cfTargetArgs, null, null, null),
                    Times.Once);

            mockCfCliService.Verify(mock =>
                mock.PushAppAsync(fakeApp.AppName, null, null, fakeProjectPath),
                    Times.Once);
        }

        [TestMethod]
        [TestCategory("DeployApp")]
        public async Task DeployAppAsync_ReturnsTrueResult_WhenCfTargetAndPushCommandsSucceed()
        {
            var cfTargetArgs = $"target -o {fakeOrg.OrgName} -s {fakeSpace.SpaceName}";

            var fakeCfTargetResponse = new DetailedResult(true);
            var fakeCfPushResponse = new DetailedResult(true);

            mockCfCliService.Setup(mock =>
                mock.InvokeCfCliAsync(cfTargetArgs, It.IsAny<StdOutDelegate>(), It.IsAny<StdErrDelegate>(), It.IsAny<string>()))
                    .ReturnsAsync(fakeCfTargetResponse);

            mockCfCliService.Setup(mock =>
                mock.PushAppAsync(fakeApp.AppName, It.IsAny<StdOutDelegate>(), It.IsAny<StdErrDelegate>(), It.IsAny<string>()))
                    .ReturnsAsync(fakeCfPushResponse);

            mockFileLocatorService.Setup(mock => mock.DirContainsFiles(It.IsAny<string>())).Returns(true);

            DetailedResult result = await cfService.DeployAppAsync(fakeCfInstance, fakeOrg, fakeSpace, fakeApp.AppName, fakeProjectPath, stdOutCallback: null, stdErrCallback: null);

            Assert.IsTrue(result.Succeeded);

            mockCfCliService.Verify(mock =>
                mock.InvokeCfCliAsync(cfTargetArgs, null, null, null),
                    Times.Once);

            mockCfCliService.Verify(mock =>
                mock.PushAppAsync(fakeApp.AppName, null, null, fakeProjectPath),
                    Times.Once);
        }

        [TestMethod]
        [TestCategory("DeployApp")]
        public async Task DeployAppAsync_ReturnsFalseResult_WhenProjDirContainsNoFiles()
        {
            mockFileLocatorService.Setup(mock => mock.DirContainsFiles(It.IsAny<string>())).Returns(false);

            var result = await cfService.DeployAppAsync(fakeCfInstance, fakeOrg, fakeSpace, fakeApp.AppName, fakeProjectPath, stdOutCallback: null, stdErrCallback: null);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Explanation.Contains(CloudFoundryService.emptyOutputDirMessage));
            mockFileLocatorService.VerifyAll();
        }
        

        [TestMethod]
        [TestCategory("GetRecentLogs")]
        public async Task GetRecentLogs_ReturnsSuccessResult_WhenWrappedMethodSucceeds()
        {
            mockCfCliService.Setup(m => m.
                TargetOrg(fakeApp.ParentSpace.ParentOrg.OrgName))
                    .Returns(fakeSuccessDetailedResult);
            
            mockCfCliService.Setup(m => m.
                TargetSpace(fakeApp.ParentSpace.SpaceName))
                    .Returns(fakeSuccessDetailedResult);

            var logsStub = "These are fake app logs!\n[12:16:04] App took a nap.";
            var fakeLogsResult = new DetailedResult<string>(logsStub, true, null, fakeSuccessCmdResult);

            mockCfCliService.Setup(m => m
                .GetRecentAppLogs(fakeApp.AppName))
                    .ReturnsAsync(fakeLogsResult);

            var result = await cfService.GetRecentLogs(fakeApp);

            Assert.AreEqual(result.Content, logsStub);
            Assert.AreEqual(result.Succeeded, fakeLogsResult.Succeeded);
            Assert.AreEqual(result.Explanation, fakeLogsResult.Explanation);
            Assert.AreEqual(result.CmdDetails, fakeLogsResult.CmdDetails);
        }

        [TestMethod]
        [TestCategory("GetRecentLogs")]
        public async Task GetRecentLogs_ReturnsFailedResult_WhenWrappedMethodFails()
        {
            mockCfCliService.Setup(m => m.
                TargetOrg(fakeApp.ParentSpace.ParentOrg.OrgName))
                    .Returns(fakeSuccessDetailedResult);
            
            mockCfCliService.Setup(m => m.
                TargetSpace(fakeApp.ParentSpace.SpaceName))
                    .Returns(fakeSuccessDetailedResult);

            string fakeLogs = null;
            var fakeErrorMsg = "something went wrong";
            var fakeLogsResult = new DetailedResult<string>(fakeLogs, false, fakeErrorMsg, fakeFailureCmdResult);

            mockCfCliService.Setup(m => m
                .GetRecentAppLogs(fakeApp.AppName))
                    .ReturnsAsync(fakeLogsResult);

            var result = await cfService.GetRecentLogs(fakeApp);

            Assert.IsNull(result.Content);
            Assert.AreEqual(result.Succeeded, fakeLogsResult.Succeeded);
            Assert.AreEqual(result.Explanation, fakeLogsResult.Explanation);
            Assert.AreEqual(result.CmdDetails, fakeLogsResult.CmdDetails);
        }
        
        [TestMethod]
        [TestCategory("GetRecentLogs")]
        public async Task GetRecentLogs_ReturnsFailedResult_WhenTargetOrgFails()
        {
            mockCfCliService.Setup(m => m.
                TargetOrg(fakeApp.ParentSpace.ParentOrg.OrgName))
                    .Returns(fakeFailureDetailedResult);

            var result = await cfService.GetRecentLogs(fakeApp);

            Assert.IsNull(result.Content);
            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(result.Explanation, fakeFailureDetailedResult.Explanation);
            Assert.AreEqual(result.CmdDetails, fakeFailureDetailedResult.CmdDetails);
        }
        
        [TestMethod]
        [TestCategory("GetRecentLogs")]
        public async Task GetRecentLogs_ReturnsFailedResult_WhenTargetSpaceFails()
        {
            mockCfCliService.Setup(m => m.
                TargetOrg(fakeApp.ParentSpace.ParentOrg.OrgName))
                    .Returns(fakeSuccessDetailedResult);
            
            mockCfCliService.Setup(m => m.
                TargetSpace(fakeApp.ParentSpace.SpaceName))
                    .Returns(fakeFailureDetailedResult);

            var result = await cfService.GetRecentLogs(fakeApp);

            Assert.IsNull(result.Content);
            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(result.Explanation, fakeFailureDetailedResult.Explanation);
            Assert.AreEqual(result.CmdDetails, fakeFailureDetailedResult.CmdDetails);
        }

    }
}
