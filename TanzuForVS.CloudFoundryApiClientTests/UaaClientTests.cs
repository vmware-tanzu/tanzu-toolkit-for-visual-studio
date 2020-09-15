using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Net.Http;

namespace TanzuForVS.CloudFoundryApiClient.UnitTests
{
    [TestClass()]
    public class UaaClientTests
    {
        private static UaaClient _sut;

        [TestInitialize()]
        public void TestInit()
        {
            //var mockHttpClient = ;
            //_sut = new UaaClient(mock)
        }

        [TestMethod()]
        public void RequestAccessTokenTest()
        {
            Assert.Fail();
        }
    }
}
