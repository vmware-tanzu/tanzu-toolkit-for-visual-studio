using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Security;
using System.Threading.Tasks;

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
        string fakeLoginSuccessResponse = "login success!";

        [TestInitialize()]
        public void TestInit()
        {
            cfService = new CloudFoundryService(services);
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
            mockCfApiClient.Setup(mock => mock.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(fakeLoginSuccessResponse);

            ConnectResult result = await cfService.ConnectToCFAsync(fakeValidTarget, fakeValidUsername, fakeValidPassword, fakeHttpProxy, skipSsl);

            Assert.IsTrue(result.IsLoggedIn);
            Assert.IsNull(result.ErrorMessage);
            Assert.IsTrue(cfService.IsLoggedIn);
            mockCfApiClient.Verify(mock => mock.LoginAsync(fakeValidTarget, fakeValidUsername, It.IsAny<string>()), Times.Once);
        }

        [TestMethod()]
        public async Task ConnectToCFAsync_ReturnsConnectResult_WhenLoginFails()
        {
            mockCfApiClient.Setup(mock => mock.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync((string)null);

            ConnectResult result = await cfService.ConnectToCFAsync(fakeValidTarget, fakeValidUsername, fakeValidPassword, fakeHttpProxy, skipSsl);

            Assert.IsFalse(result.IsLoggedIn);
            Assert.IsTrue(result.ErrorMessage.Contains(cfService.LoginFailureMessage));
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

            Assert.IsTrue(result.ErrorMessage.Contains(baseMessage));
            Assert.IsTrue(result.ErrorMessage.Contains(innerMessage));
            Assert.IsTrue(result.ErrorMessage.Contains(outerMessage));
        }

    }
}
