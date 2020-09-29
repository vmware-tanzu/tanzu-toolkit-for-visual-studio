using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace TanzuForVS.Services.CloudFoundry
{
    [TestClass()]
    public class CloudFoundryServiceTests
    {
        [TestMethod()]
        public void ConnectToCFAsync_ValidatesParameters()
        {
            var cfService = new CloudFoundryService();

            Assert.ThrowsException<ArgumentException>(() => cfService.ConnectToCFAsync(null, null, null, null, false));
            Assert.ThrowsException<ArgumentException>(() => cfService.ConnectToCFAsync(string.Empty, null, null, null, false));
            Assert.ThrowsException<ArgumentException>(() => cfService.ConnectToCFAsync("Junk", null, null, null, false));
            Assert.ThrowsException<ArgumentException>(() => cfService.ConnectToCFAsync("Junk", string.Empty, null, null, false));
            Assert.ThrowsException<ArgumentNullException>(() => cfService.ConnectToCFAsync("Junk", "Junk", null, null, false));
        }
    }
}
