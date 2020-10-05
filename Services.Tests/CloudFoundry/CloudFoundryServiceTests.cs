using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace TanzuForVS.Services.CloudFoundry
{
    [TestClass()]
    public class CloudFoundryServiceTests
    {
        [TestMethod()]
        public async Task ConnectToCFAsync_ThrowsExceptions_WhenParametersAreInvalid()
        {
            var cfService = new CloudFoundryService();

            await Assert.ThrowsExceptionAsync<ArgumentException>(() => cfService.ConnectToCFAsync(null, null, null, null, false));
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => cfService.ConnectToCFAsync(string.Empty, null, null, null, false));
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => cfService.ConnectToCFAsync("Junk", null, null, null, false));
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => cfService.ConnectToCFAsync("Junk", string.Empty, null, null, false));
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => cfService.ConnectToCFAsync("Junk", "Junk", null, null, false));
        }

        [TestMethod()]
        public async Task ConnectToCFAsync_ReturnsConnectResult_WhenParametersAreValid()
        {
            var cfService = new CloudFoundryService();

            // TODO: mock a "success" response from CfApiClient.LoginAsync

            ConnectResult result = await cfService.ConnectToCFAsync(null, null, null, null, false);

            Assert.IsTrue(result.IsLoggedIn);
            Assert.IsNull(result.ErrorMessage);
        }
    }
}
