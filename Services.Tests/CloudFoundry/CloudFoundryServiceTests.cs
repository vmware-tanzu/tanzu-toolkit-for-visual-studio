using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;
using Tanzu.Toolkit.VisualStudio.Models;
using Tanzu.Toolkit.VisualStudio.Services.CfCli.Models.Orgs;
using Tanzu.Toolkit.VisualStudio.Services.CfCli.Models.Spaces;
using Tanzu.Toolkit.VisualStudio.Services.CfCli.Models.Apps;
using Tanzu.Toolkit.VisualStudio.Services.CloudFoundry;
using Tanzu.Toolkit.VisualStudio.Services.CmdProcess;
using static Tanzu.Toolkit.VisualStudio.Services.OutputHandler.OutputHandler;
using Metadata = Tanzu.Toolkit.VisualStudio.Services.CfCli.Models.Orgs.Metadata;

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
            fakeOrg = new CloudFoundryOrganization("fake org", "fake org guid", fakeCfInstance, "fake spaces url");
            fakeSpace = new CloudFoundrySpace("fake space", "fake space guid", fakeOrg);
            fakeApp = new CloudFoundryApp("fake app", "fake app guid", fakeSpace);
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
        [TestCategory("GetOrgs")]
        public async Task GetOrgsForCfInstanceAsync_ReturnsFailedResult_WhenListOrgsFails()
        {
            var fakeFailedOrgsResult = new DetailedResult<List<Org>>(
                content: new List<Org>(), // this is unrealistic if succeeded == false; testing just in case
                succeeded: false,
                explanation: "junk",
                cmdDetails: fakeFailureCmdResult);

            mockCfCliService.Setup(mock => mock.
                TargetApi(fakeCfInstance.ApiAddress, true))
                    .Returns(fakeSuccessDetailedResult);

            mockCfCliService.Setup(mock => mock.
                GetOrgsAsync())
                    .ReturnsAsync(fakeFailedOrgsResult);

            DetailedResult<List<CloudFoundryOrganization>> result = await cfService.GetOrgsForCfInstanceAsync(fakeCfInstance);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(fakeFailedOrgsResult.Explanation, result.Explanation);
            Assert.AreEqual(fakeFailedOrgsResult.CmdDetails, result.CmdDetails);

            mockCfCliService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("GetOrgs")]
        public async Task GetOrgsForCfInstanceAsync_ReturnsFailedResult_WhenListOrgsReturnsNoContent()
        {
            var fakeFailedOrgsResult = new DetailedResult<List<Org>>(
                content: null,
                succeeded: true, // this is unrealistic if content == null; testing just in case
                explanation: "junk",
                cmdDetails: fakeFailureCmdResult);

            mockCfCliService.Setup(mock => mock.
                TargetApi(fakeCfInstance.ApiAddress, true))
                    .Returns(fakeSuccessDetailedResult);

            mockCfCliService.Setup(mock => mock.
                GetOrgsAsync())
                    .ReturnsAsync(fakeFailedOrgsResult);

            DetailedResult<List<CloudFoundryOrganization>> result = await cfService.GetOrgsForCfInstanceAsync(fakeCfInstance);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(fakeFailedOrgsResult.Explanation, result.Explanation);
            Assert.AreEqual(fakeFailedOrgsResult.CmdDetails, result.CmdDetails);

            mockCfCliService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("GetOrgs")]
        public async Task GetOrgsForCfInstanceAsync_ReturnsSuccessfulResult_WhenGetOrgsAsyncSucceeds()
        {
            var fakeOrgsDetailedResult = new DetailedResult<List<Org>>(content: mockOrgsResponse, succeeded: true, cmdDetails: fakeSuccessCmdResult);

            var expectedResultContent = new List<CloudFoundryOrganization>
            {
                new CloudFoundryOrganization(org1Name, org1Guid, fakeCfInstance, org1SpacesUrl),
                new CloudFoundryOrganization(org2Name, org2Guid, fakeCfInstance, org2SpacesUrl),
                new CloudFoundryOrganization(org3Name, org3Guid, fakeCfInstance, org3SpacesUrl),
                new CloudFoundryOrganization(org4Name, org4Guid, fakeCfInstance, org4SpacesUrl)
            };

            mockCfCliService.Setup(mock => mock.
                TargetApi(fakeCfInstance.ApiAddress, true))
                    .Returns(fakeSuccessDetailedResult);

            mockCfCliService.Setup(mock => mock.
                GetOrgsAsync())
                    .ReturnsAsync(fakeOrgsDetailedResult);

            DetailedResult<List<CloudFoundryOrganization>> result = await cfService.GetOrgsForCfInstanceAsync(fakeCfInstance);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.AreEqual(fakeOrgsDetailedResult.CmdDetails, result.CmdDetails);
            Assert.AreEqual(expectedResultContent.Count, result.Content.Count);

            for (int i = 0; i < expectedResultContent.Count; i++)
            {
                Assert.AreEqual(expectedResultContent[i].OrgId, result.Content[i].OrgId);
                Assert.AreEqual(expectedResultContent[i].OrgName, result.Content[i].OrgName);
                Assert.AreEqual(expectedResultContent[i].ParentCf, result.Content[i].ParentCf);
                Assert.AreEqual(expectedResultContent[i].SpacesUrl, result.Content[i].SpacesUrl);
            }

            mockCfCliService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("GetOrgs")]
        public async Task GetOrgsForCfInstanceAsync_ReturnsFailedResult_WhenTargetApiFails()
        {
            mockCfCliService.Setup(mock => mock.
                TargetApi(fakeCfInstance.ApiAddress, true))
                    .Returns(fakeFailureDetailedResult);

            DetailedResult<List<CloudFoundryOrganization>> result = await cfService.GetOrgsForCfInstanceAsync(fakeCfInstance);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNull(result.Content);
            Assert.AreEqual(fakeFailureDetailedResult.Explanation, result.Explanation);
            Assert.AreEqual(fakeFailureDetailedResult.CmdDetails, result.CmdDetails);

            mockCfCliService.VerifyAll();
        }



        [TestMethod]
        [TestCategory("GetSpaces")]
        public async Task GetSpacesForOrgAsync_ReturnsFailedResult_WhenSpacesResultReportsFailure()
        {
            var fakeFailedSpacesResult = new DetailedResult<List<Space>>(
                content: new List<Space>(), // this is unrealistic if succeeded == false; testing just in case
                succeeded: false,
                explanation: "junk",
                cmdDetails: fakeFailureCmdResult);

            mockCfCliService.Setup(mock => mock.
                TargetApi(fakeOrg.ParentCf.ApiAddress, true))
                    .Returns(fakeSuccessDetailedResult);

            mockCfCliService.Setup(mock => mock.
                GetSpacesAsync(fakeOrg.SpacesUrl))
                    .ReturnsAsync(fakeFailedSpacesResult);

            DetailedResult<List<CloudFoundrySpace>> result = await cfService.GetSpacesForOrgAsync(fakeOrg);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(fakeFailedSpacesResult.Explanation, result.Explanation);
            Assert.AreEqual(fakeFailedSpacesResult.CmdDetails, result.CmdDetails);

            mockCfCliService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("GetSpaces")]
        public async Task GetSpacesForOrgAsync_ReturnsFailedResult_WhenSpacesResultHasNoContent()
        {
            var fakeFailedSpacesResult = new DetailedResult<List<Space>>(
                content: null,
                succeeded: true, // this is unrealistic if content == null; testing just in case
                explanation: "junk",
                cmdDetails: fakeFailureCmdResult);

            mockCfCliService.Setup(mock => mock.
                TargetApi(fakeOrg.ParentCf.ApiAddress, true))
                    .Returns(fakeSuccessDetailedResult);

            mockCfCliService.Setup(mock => mock.
                GetSpacesAsync(fakeOrg.SpacesUrl))
                    .ReturnsAsync(fakeFailedSpacesResult);

            DetailedResult<List<CloudFoundrySpace>> result = await cfService.GetSpacesForOrgAsync(fakeOrg);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(fakeFailedSpacesResult.Explanation, result.Explanation);
            Assert.AreEqual(fakeFailedSpacesResult.CmdDetails, result.CmdDetails);

            mockCfCliService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("GetSpaces")]
        public async Task GetSpacesForOrgAsync_ReturnsSuccessfulResult_WhenGetSpacesAsyncSucceeds()
        {
            var fakeSpacesDetailedResult = new DetailedResult<List<Space>>(content: mockSpacesResponse, succeeded: true, cmdDetails: fakeSuccessCmdResult);

            var expectedResultContent = new List<CloudFoundrySpace>
            {
                new CloudFoundrySpace(space1Name, space1Guid, fakeOrg),
                new CloudFoundrySpace(space2Name, space2Guid, fakeOrg),
                new CloudFoundrySpace(space3Name, space3Guid, fakeOrg),
                new CloudFoundrySpace(space4Name, space4Guid, fakeOrg)
            };

            mockCfCliService.Setup(mock => mock.
                TargetApi(fakeOrg.ParentCf.ApiAddress, true))
                    .Returns(fakeSuccessDetailedResult);

            mockCfCliService.Setup(mock => mock.
                GetSpacesAsync(fakeOrg.SpacesUrl))
                    .ReturnsAsync(fakeSpacesDetailedResult);

            DetailedResult<List<CloudFoundrySpace>> result = await cfService.GetSpacesForOrgAsync(fakeOrg);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.AreEqual(fakeSpacesDetailedResult.CmdDetails, result.CmdDetails);
            Assert.AreEqual(expectedResultContent.Count, result.Content.Count);

            for (int i = 0; i < expectedResultContent.Count; i++)
            {
                Assert.AreEqual(expectedResultContent[i].SpaceId, result.Content[i].SpaceId);
                Assert.AreEqual(expectedResultContent[i].SpaceName, result.Content[i].SpaceName);
                Assert.AreEqual(expectedResultContent[i].ParentOrg, result.Content[i].ParentOrg);
            }

            mockCfCliService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("GetSpaces")]
        public async Task GetSpacesForOrgAsync_ReturnsFailedResult_WhenTargetApiFails()
        {
            mockCfCliService.Setup(mock => mock.
                TargetApi(fakeOrg.ParentCf.ApiAddress, true))
                    .Returns(fakeFailureDetailedResult);

            DetailedResult<List<CloudFoundrySpace>> result = await cfService.GetSpacesForOrgAsync(fakeOrg);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNull(result.Content);
            Assert.AreEqual(fakeFailureDetailedResult.Explanation, result.Explanation);
            Assert.AreEqual(fakeFailureDetailedResult.CmdDetails, result.CmdDetails);

            mockCfCliService.VerifyAll();
        }


        [TestMethod]
        [TestCategory("GetApps")]
        public async Task GetAppsForSpaceAsync_ReturnsFailedResult_WhenGetAppsFails()
        {
            var fakeFailureResult = new DetailedResult<List<App>>(
                succeeded: false,
                content: null,
                explanation: "junk",
                cmdDetails: fakeFailureCmdResult
            );

            mockCfCliService.Setup(mock => mock.
                TargetApi(fakeCfInstance.ApiAddress, true))
                    .Returns(fakeSuccessDetailedResult);

            mockCfCliService.Setup(mock => mock.
                TargetOrg(fakeOrg.OrgName))
                    .Returns(fakeSuccessDetailedResult);

            mockCfCliService.Setup(mock => mock.
                TargetSpace(fakeSpace.SpaceName))
                    .Returns(fakeSuccessDetailedResult);

            mockCfCliService.Setup(mock => mock.
                GetAppsAsync())
                    .ReturnsAsync(fakeFailureResult);

            var result = await cfService.GetAppsForSpaceAsync(fakeSpace);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(fakeFailureResult.Explanation, result.Explanation);
            Assert.AreEqual(fakeFailureResult.CmdDetails, result.CmdDetails);

            mockCfCliService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("GetApps")]
        public async Task GetAppsForSpaceAsync_ReturnsSuccessfulResult_WhenListAppsSuceeds()
        {
            var expectedResult = new List<CloudFoundryApp>
            {
                new CloudFoundryApp(app1Name, app1Guid, fakeSpace),
                new CloudFoundryApp(app2Name, app2Guid, fakeSpace),
                new CloudFoundryApp(app3Name, app3Guid, fakeSpace),
                new CloudFoundryApp(app4Name, app4Guid, fakeSpace)
            };

            var fakeAppsResult = new DetailedResult<List<App>>(
                succeeded: true,
                content: mockAppsResponse,
                explanation: null,
                cmdDetails: fakeSuccessCmdResult);

            mockCfCliService.Setup(mock => mock.
                TargetApi(fakeCfInstance.ApiAddress, true))
                    .Returns(new DetailedResult(true, null, fakeSuccessCmdResult));

            mockCfCliService.Setup(mock => mock.
                TargetOrg(fakeOrg.OrgName))
                    .Returns(new DetailedResult(true, null, fakeSuccessCmdResult));

            mockCfCliService.Setup(mock => mock.
                TargetSpace(fakeSpace.SpaceName))
                    .Returns(new DetailedResult(true, null, fakeSuccessCmdResult));

            mockCfCliService.Setup(mock => mock.
                GetAppsAsync())
                    .ReturnsAsync(fakeAppsResult);

            var result = await cfService.GetAppsForSpaceAsync(fakeSpace);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.AreEqual(fakeAppsResult.CmdDetails, result.CmdDetails);
            Assert.AreEqual(expectedResult.Count, result.Content.Count);

            for (int i = 0; i < expectedResult.Count; i++)
            {
                Assert.AreEqual(expectedResult[i].AppId, result.Content[i].AppId);
                Assert.AreEqual(expectedResult[i].AppName, result.Content[i].AppName);
                Assert.AreEqual(expectedResult[i].ParentSpace, result.Content[i].ParentSpace);
            }

            mockCfCliService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("GetApps")]
        public async Task GetAppsForSpaceAsync_ReturnsFailedResult_WhenTargetApiFails()
        {
            mockCfCliService.Setup(mock => mock.
                TargetApi(fakeCfInstance.ApiAddress, true))
                    .Returns(fakeFailureDetailedResult);

            var result = await cfService.GetAppsForSpaceAsync(fakeSpace);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNull(result.Content);
            Assert.AreEqual(fakeFailureDetailedResult.Explanation, result.Explanation);
            Assert.AreEqual(fakeFailureDetailedResult.CmdDetails, result.CmdDetails);

            mockCfCliService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("GetApps")]
        public async Task GetAppsForSpaceAsync_ReturnsFailedResult_WhenTargetOrgFails()
        {
            mockCfCliService.Setup(mock => mock.
                TargetApi(fakeCfInstance.ApiAddress, true))
                    .Returns(fakeSuccessDetailedResult);

            mockCfCliService.Setup(mock => mock.
                TargetOrg(fakeOrg.OrgName))
                    .Returns(fakeFailureDetailedResult);

            var result = await cfService.GetAppsForSpaceAsync(fakeSpace);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNull(result.Content);
            Assert.AreEqual(fakeFailureDetailedResult.Explanation, result.Explanation);
            Assert.AreEqual(fakeFailureDetailedResult.CmdDetails, result.CmdDetails);

            mockCfCliService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("GetApps")]
        public async Task GetAppsForSpaceAsync_ReturnsFailedResult_WhenTargetSpaceFails()
        {
            mockCfCliService.Setup(mock => mock.
                TargetApi(fakeCfInstance.ApiAddress, true))
                    .Returns(fakeSuccessDetailedResult);

            mockCfCliService.Setup(mock => mock.
                TargetOrg(fakeOrg.OrgName))
                    .Returns(fakeSuccessDetailedResult);

            mockCfCliService.Setup(mock => mock.
                TargetSpace(fakeSpace.SpaceName))
                    .Returns(fakeFailureDetailedResult);

            var result = await cfService.GetAppsForSpaceAsync(fakeSpace);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNull(result.Content);
            Assert.AreEqual(fakeFailureDetailedResult.Explanation, result.Explanation);
            Assert.AreEqual(fakeFailureDetailedResult.CmdDetails, result.CmdDetails);

            mockCfCliService.VerifyAll();
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
        [TestCategory("StopApp")]
        public async Task StopAppAsync_ReturnsSuccessfulResult_AndUpdatesAppState_WhenStopCmdSucceeds()
        {
            fakeApp.State = "STARTED";

            mockCfCliService.Setup(mock => mock.
                TargetApi(fakeCfInstance.ApiAddress, true))
                    .Returns(new DetailedResult(true, null, fakeSuccessCmdResult));

            mockCfCliService.Setup(mock => mock.
                TargetOrg(fakeOrg.OrgName))
                    .Returns(new DetailedResult(true, null, fakeSuccessCmdResult));

            mockCfCliService.Setup(mock => mock.
                TargetSpace(fakeSpace.SpaceName))
                    .Returns(new DetailedResult(true, null, fakeSuccessCmdResult));

            mockCfCliService.Setup(mock => mock.
                StopAppByNameAsync(fakeApp.AppName))
                    .ReturnsAsync(new DetailedResult(true, null, fakeSuccessCmdResult));

            DetailedResult result = await cfService.StopAppAsync(fakeApp);

            Assert.AreEqual("STOPPED", fakeApp.State);
            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.AreEqual(fakeSuccessCmdResult, result.CmdDetails);
        }

        [TestMethod]
        [TestCategory("StopApp")]
        public async Task StopAppAsync_ReturnsFailedResult_WhenStopCmdReportsFailure()
        {
            var fakeExplanation = "junk";

            mockCfCliService.Setup(mock => mock.
                TargetApi(fakeCfInstance.ApiAddress, true))
                    .Returns(new DetailedResult(true, null, fakeSuccessCmdResult));

            mockCfCliService.Setup(mock => mock.
                TargetOrg(fakeOrg.OrgName))
                    .Returns(new DetailedResult(true, null, fakeSuccessCmdResult));

            mockCfCliService.Setup(mock => mock.
                TargetSpace(fakeSpace.SpaceName))
                    .Returns(new DetailedResult(true, null, fakeSuccessCmdResult));

            mockCfCliService.Setup(mock => mock.
                StopAppByNameAsync(fakeApp.AppName))
                    .ReturnsAsync(new DetailedResult(false, fakeExplanation, fakeFailureCmdResult));

            DetailedResult result = await cfService.StopAppAsync(fakeApp);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(fakeExplanation, result.Explanation);
            Assert.AreEqual(fakeFailureCmdResult, result.CmdDetails);
        }



        [TestMethod]
        [TestCategory("StartApp")]
        public async Task StartAppAsync_ReturnsTrue_AndUpdatesAppState_WhenStartCmdSucceeds()
        {
            fakeApp.State = "STOPPED";

            mockCfCliService.Setup(mock => mock.
                TargetApi(fakeCfInstance.ApiAddress, true))
                    .Returns(new DetailedResult(true, null, fakeSuccessCmdResult));

            mockCfCliService.Setup(mock => mock.
               TargetOrg(fakeOrg.OrgName)).Returns(new DetailedResult(true, null, fakeSuccessCmdResult));

            mockCfCliService.Setup(mock => mock.
               TargetSpace(fakeSpace.SpaceName)).Returns(new DetailedResult(true, null, fakeSuccessCmdResult));

            mockCfCliService.Setup(mock => mock.
                StartAppByNameAsync(fakeApp.AppName))
                    .ReturnsAsync(new DetailedResult(true, null, fakeSuccessCmdResult));

            DetailedResult result = await cfService.StartAppAsync(fakeApp);

            Assert.AreEqual("STARTED", fakeApp.State);
            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.AreEqual(fakeSuccessCmdResult, result.CmdDetails);
        }

        [TestMethod]
        [TestCategory("StartApp")]
        public async Task StartAppAsync_ReturnsFailedResult_WhenStartCmdReportsFailure()
        {
            var fakeExplanation = "junk";

            mockCfCliService.Setup(mock => mock.
                TargetApi(fakeCfInstance.ApiAddress, true))
                    .Returns(new DetailedResult(true, null, fakeSuccessCmdResult));

            mockCfCliService.Setup(mock => mock.
               TargetOrg(fakeOrg.OrgName)).Returns(new DetailedResult(true, null, fakeSuccessCmdResult));

            mockCfCliService.Setup(mock => mock.
               TargetSpace(fakeSpace.SpaceName)).Returns(new DetailedResult(true, null, fakeSuccessCmdResult));

            mockCfCliService.Setup(mock => mock.
                StartAppByNameAsync(fakeApp.AppName))
                    .ReturnsAsync(new DetailedResult(false, fakeExplanation, fakeFailureCmdResult));

            DetailedResult result = await cfService.StartAppAsync(fakeApp);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(fakeExplanation, result.Explanation);
            Assert.AreEqual(fakeFailureCmdResult, result.CmdDetails);
        }




        [TestMethod]
        [TestCategory("DeleteApp")]
        public async Task DeleteAppAsync_ReturnsSuccessfulResult_AndUpdatesAppState_WhenDeleteCmdSucceeds()
        {
            fakeApp.State = "STOPPED";

            mockCfCliService.Setup(mock => mock.
                TargetApi(fakeCfInstance.ApiAddress, true))
                    .Returns(new DetailedResult(true, null, fakeSuccessCmdResult));

            mockCfCliService.Setup(mock => mock.
               TargetOrg(fakeOrg.OrgName)).Returns(new DetailedResult(true, null, fakeSuccessCmdResult));

            mockCfCliService.Setup(mock => mock.
               TargetSpace(fakeSpace.SpaceName)).Returns(new DetailedResult(true, null, fakeSuccessCmdResult));

            mockCfCliService.Setup(mock => mock.
                DeleteAppByNameAsync(fakeApp.AppName, true))
                    .ReturnsAsync(new DetailedResult(true, null, fakeSuccessCmdResult));

            var result = await cfService.DeleteAppAsync(fakeApp);

            Assert.AreEqual("DELETED", fakeApp.State);
            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.AreEqual(fakeSuccessCmdResult, result.CmdDetails);
        }

        [TestMethod]
        [TestCategory("DeleteApp")]
        public async Task DeleteAppAsync_ReturnsFailedResult_WhenDeleteCmdReportsFailure()
        {
            var fakeExplanation = "junk";

            mockCfCliService.Setup(mock => mock.
                TargetApi(fakeCfInstance.ApiAddress, true))
                    .Returns(new DetailedResult(true, null, fakeSuccessCmdResult));

            mockCfCliService.Setup(mock => mock.
               TargetOrg(fakeOrg.OrgName)).Returns(new DetailedResult(true, null, fakeSuccessCmdResult));

            mockCfCliService.Setup(mock => mock.
               TargetSpace(fakeSpace.SpaceName)).Returns(new DetailedResult(true, null, fakeSuccessCmdResult));

            mockCfCliService.Setup(mock => mock.
                DeleteAppByNameAsync(fakeApp.AppName, true))
                    .ReturnsAsync(new DetailedResult(false, fakeExplanation, fakeFailureCmdResult));

            var result = await cfService.DeleteAppAsync(fakeApp);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(fakeExplanation, result.Explanation);
            Assert.AreEqual(fakeFailureCmdResult, result.CmdDetails);
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
            var cfPushArgs = $"push {fakeApp.AppName}";
            var fakeCfTargetResponse = new DetailedResult(true);
            var fakeCfPushResponse = new DetailedResult(false, fakeFailureExplanation);

            mockCfCliService.Setup(mock =>
                mock.InvokeCfCliAsync(cfTargetArgs, It.IsAny<StdOutDelegate>(), It.IsAny<StdErrDelegate>(), It.IsAny<string>()))
                    .ReturnsAsync(fakeCfTargetResponse);

            mockCfCliService.Setup(mock =>
                mock.InvokeCfCliAsync(cfPushArgs, It.IsAny<StdOutDelegate>(), It.IsAny<StdErrDelegate>(), It.IsAny<string>()))
                    .ReturnsAsync(fakeCfPushResponse);

            mockFileLocatorService.Setup(mock => mock.DirContainsFiles(It.IsAny<string>())).Returns(true);

            DetailedResult result = await cfService.DeployAppAsync(fakeCfInstance, fakeOrg, fakeSpace, fakeApp.AppName, fakeProjectPath, stdOutCallback: null, stdErrCallback: null);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Explanation.Contains(fakeFailureExplanation));

            mockCfCliService.Verify(mock =>
                mock.InvokeCfCliAsync(cfTargetArgs, null, null, null),
                    Times.Once);

            mockCfCliService.Verify(mock =>
                mock.InvokeCfCliAsync(cfPushArgs, null, null, fakeProjectPath),
                    Times.Once);
        }

        [TestMethod]
        [TestCategory("DeployApp")]
        public async Task DeployAppAsync_ReturnsTrueResult_WhenCfTargetAndPushCommandsSucceed()
        {
            var cfTargetArgs = $"target -o {fakeOrg.OrgName} -s {fakeSpace.SpaceName}";
            var cfPushArgs = $"push {fakeApp.AppName}";
            var fakeCfTargetResponse = new DetailedResult(true);
            var fakeCfPushResponse = new DetailedResult(true);

            mockCfCliService.Setup(mock =>
                mock.InvokeCfCliAsync(cfTargetArgs, It.IsAny<StdOutDelegate>(), It.IsAny<StdErrDelegate>(), It.IsAny<string>()))
                    .ReturnsAsync(fakeCfTargetResponse);

            mockCfCliService.Setup(mock =>
                mock.InvokeCfCliAsync(cfPushArgs, It.IsAny<StdOutDelegate>(), It.IsAny<StdErrDelegate>(), It.IsAny<string>()))
                    .ReturnsAsync(fakeCfPushResponse);

            mockFileLocatorService.Setup(mock => mock.DirContainsFiles(It.IsAny<string>())).Returns(true);

            DetailedResult result = await cfService.DeployAppAsync(fakeCfInstance, fakeOrg, fakeSpace, fakeApp.AppName, fakeProjectPath, stdOutCallback: null, stdErrCallback: null);

            Assert.IsTrue(result.Succeeded);

            mockCfCliService.Verify(mock =>
                mock.InvokeCfCliAsync(cfTargetArgs, null, null, null),
                    Times.Once);

            mockCfCliService.Verify(mock =>
                mock.InvokeCfCliAsync(cfPushArgs, null, null, fakeProjectPath),
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

    }
}
