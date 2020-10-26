using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;
using TanzuForVS.CloudFoundryApiClient.Models.OrgsResponse;
using TanzuForVS.Models;

namespace TanzuForVS.Services.CloudFoundry
{
    [TestClass()]
    public class CloudFoundryServiceTests : ServicesTestSupport
    {
        ICloudFoundryService cfService;
        string fakeValidTarget = "https://my.fake.target";
        string fakeValidUsername = "junk";
        SecureString fakeValidPassword = new SecureString();
        string fakeHttpProxy = "junk";
        bool skipSsl = true;
        string fakeValidAccessToken = "valid token";
        CloudFoundryInstance fakeCfInstance;

        [TestInitialize()]
        public void TestInit()
        {
            cfService = new CloudFoundryService(services);
            fakeCfInstance = new CloudFoundryInstance("fake cf", fakeValidTarget, fakeValidAccessToken);
        }

        [TestMethod()]
        public async Task ConnectToCFAsync_ThrowsExceptions_WhenParametersAreInvalid()
        {
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => cfService.ConnectToCFAsync(null, null, null, null, false));
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => cfService.ConnectToCFAsync(string.Empty, null, null, null, false));
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => cfService.ConnectToCFAsync("Junk", null, null, null, false));
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => cfService.ConnectToCFAsync("Junk", string.Empty, null, null, false));
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => cfService.ConnectToCFAsync("Junk", "Junk", null, null, false));
        }

        [TestMethod()]
        public async Task ConnectToCFAsync_ReturnsConnectResult_WhenLoginSucceeds()
        {
            mockCfApiClient.Setup(mock => mock.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(fakeValidAccessToken);

            ConnectResult result = await cfService.ConnectToCFAsync(fakeValidTarget, fakeValidUsername, fakeValidPassword, fakeHttpProxy, skipSsl);

            Assert.IsTrue(result.IsLoggedIn);
            Assert.IsNull(result.ErrorMessage);
            Assert.AreEqual(fakeValidAccessToken, result.Token);
            mockCfApiClient.Verify(mock => mock.LoginAsync(fakeValidTarget, fakeValidUsername, It.IsAny<string>()), Times.Once);
        }

        [TestMethod()]
        public async Task ConnectToCFAsync_ReturnsConnectResult_WhenLoginFails()
        {
            mockCfApiClient.Setup(mock => mock.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync((string)null);

            ConnectResult result = await cfService.ConnectToCFAsync(fakeValidTarget, fakeValidUsername, fakeValidPassword, fakeHttpProxy, skipSsl);

            Assert.IsFalse(result.IsLoggedIn);
            Assert.IsTrue(result.ErrorMessage.Contains(cfService.LoginFailureMessage));
            Assert.IsNull(result.Token);
            mockCfApiClient.Verify(mock => mock.LoginAsync(fakeValidTarget, fakeValidUsername, It.IsAny<string>()), Times.Once);
        }

        [TestMethod()]
        public async Task ConnectToCfAsync_IncludesNestedExceptionMessages_WhenExceptionIsThrown()
        {
            string baseMessage = "base exception message";
            string innerMessage = "inner exception message";
            string outerMessage = "outer exception message";
            Exception multilayeredException = new Exception(outerMessage, new Exception(innerMessage, new Exception(baseMessage)));

            mockCfApiClient.Setup(mock => mock.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ThrowsAsync(multilayeredException);

            ConnectResult result = await cfService.ConnectToCFAsync(fakeValidTarget, fakeValidUsername, fakeValidPassword, fakeHttpProxy, skipSsl);

            Assert.IsNull(result.Token);
            Assert.IsTrue(result.ErrorMessage.Contains(baseMessage));
            Assert.IsTrue(result.ErrorMessage.Contains(innerMessage));
            Assert.IsTrue(result.ErrorMessage.Contains(outerMessage));
        }

        [TestMethod()]
        public async Task GetOrgsAsync_ReturnsEmptyList_WhenListOrgsFails()
        {
            var expectedResult = new List<CloudFoundryOrganization>();

            mockCfApiClient.Setup(mock => mock.ListOrgs(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync((List<Org>)null);

            var result = await cfService.GetOrgsForCfInstanceAsync(fakeCfInstance);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
            CollectionAssert.AreEqual(expectedResult, result);
            Assert.AreEqual(typeof(List<CloudFoundryOrganization>), result.GetType());
            mockCfApiClient.Verify(mock => mock.ListOrgs(fakeValidTarget, fakeValidAccessToken), Times.Once);
        }

        [TestMethod()]
        public async Task GetOrgsAsync_ReturnsListOfStrings_WhenListOrgsSuceeds()
        {
            const string org1Name = "org1";
            const string org2Name = "org2";
            const string org3Name = "org3";
            const string org4Name = "org4";
            const string org1Guid = "org-1-id";
            const string org2Guid = "org-2-id";
            const string org3Guid = "org-3-id";
            const string org4Guid = "org-4-id";

            var mockOrgsResponse = new List<Org>
            {
                new Org
                {
                    name = org1Name,
                    guid = org1Guid
                },
                new Org
                {
                    name = org2Name,
                    guid = org2Guid
                },
                new Org
                {
                    name = org3Name,
                    guid = org3Guid
                },
                new Org
                {
                    name = org4Name,
                    guid = org4Guid
                }
            };

            var expectedResult = new List<CloudFoundryOrganization>
            {
                new CloudFoundryOrganization(org1Name, org1Guid, fakeCfInstance),
                new CloudFoundryOrganization(org2Name, org2Guid, fakeCfInstance),
                new CloudFoundryOrganization(org3Name, org3Guid, fakeCfInstance),
                new CloudFoundryOrganization(org4Name, org4Guid, fakeCfInstance)
            };

            mockCfApiClient.Setup(mock => mock.ListOrgs(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(mockOrgsResponse);

            var result = await cfService.GetOrgsForCfInstanceAsync(fakeCfInstance);

            Assert.IsNotNull(result);
            Assert.AreEqual(expectedResult.Count, result.Count);

            for (int i = 0; i < expectedResult.Count; i++)
            {
                Assert.AreEqual(expectedResult[i].OrgId, result[i].OrgId);
                Assert.AreEqual(expectedResult[i].OrgName, result[i].OrgName);
                Assert.AreEqual(expectedResult[i].ParentCf, result[i].ParentCf);
            }

            mockCfApiClient.Verify(mock => mock.ListOrgs(fakeValidTarget, fakeValidAccessToken), Times.Once);
        }

        [TestMethod()]
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
        public async Task StopAppAsync_ReturnsFalseAndLogsError_WhenRequestInfoIsMissing()
        {
            var fakeApp = new CloudFoundryApp("fakeApp",null, parentSpace: null);

            var result = await cfService.StopAppAsync(fakeApp);
            Assert.IsFalse(result);
        }
    }
}
