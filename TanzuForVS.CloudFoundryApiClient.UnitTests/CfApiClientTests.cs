using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Net;
using System.Threading.Tasks;

namespace TanzuForVS.CloudFoundryApiClient.UnitTests
{
    [TestClass()]
    public class CfApiClientTests
    {
        private Mock<IUaaClient> _mockUaaClient = new Mock<IUaaClient>();
        private CfApiClient _sut;

        [TestInitialize()]
        public void TestInit()
        {
            _sut = new CfApiClient(_mockUaaClient.Object);
        }

        [TestMethod()]
        public async Task LoginAsync_UpdatesAndReturnsAccessToken_WhenLoginSucceeds()
        {
            var expectedUri = new Uri("http://uaa.myfaketarget.com");
            var testUriString = "http://api.myfaketarget.com";

            var fakeUaaClientId = "cf";
            var fakeUaaClientSecret = "";
            var fakeCfUsername = "user";
            var fakeCfPassword = "pass";
            var fakeAccessTokenContent = "fake access token";

            _mockUaaClient.Setup(mock => mock.RequestAccessTokenAsync(
                expectedUri,
                fakeUaaClientId,
                fakeUaaClientSecret,
                fakeCfUsername,
                fakeCfPassword))
                .ReturnsAsync(HttpStatusCode.OK);

            _mockUaaClient.SetupGet(mock => mock.Token)
                .Returns(new Token() { access_token = fakeAccessTokenContent });

            var initialAccessToken = _sut.AccessToken;

            var result = await _sut.LoginAsync(testUriString, fakeCfUsername, fakeCfPassword);

            Assert.AreNotEqual(initialAccessToken, _sut.AccessToken);
            Assert.AreEqual(fakeAccessTokenContent, _sut.AccessToken);

            Assert.AreEqual(fakeAccessTokenContent, result);
            _mockUaaClient.VerifyAll();
        }

        [TestMethod()]
        public async Task LoginAsync_ReturnsNull_AndDoesNotUpdateAccessToken_WhenLoginFails()
        {
            var expectedUri = new Uri("http://uaa.myfaketarget.com");
            var testUriString = "http://api.myfaketarget.com";

            var fakeUaaClientId = "cf";
            var fakeUaaClientSecret = "";
            var fakeCfUsername = "user";
            var fakeCfPassword = "pass";

            _mockUaaClient.Setup(mock => mock.RequestAccessTokenAsync(
                expectedUri,
                fakeUaaClientId,
                fakeUaaClientSecret,
                fakeCfUsername,
                fakeCfPassword))
                .ReturnsAsync(HttpStatusCode.Unauthorized);

            var initialAccessToken = _sut.AccessToken;

            var result = await _sut.LoginAsync(testUriString, fakeCfUsername, fakeCfPassword);

            Assert.IsNull(result);

            Assert.AreEqual(initialAccessToken, _sut.AccessToken);
            _mockUaaClient.VerifyAll();
        }

        [TestMethod()]
        public async Task LoginAsync_ThrowsException_WhenCfTargetIsMalformed()
        {
            Exception expectedException = null;

            try
            {
                var malformedCfTargetString = "what-a-mess";
                await _sut.LoginAsync(malformedCfTargetString, "user", "pass");
            }
            catch (Exception e)
            {
                expectedException = e;
            }

            Assert.IsNotNull(expectedException);
        }
    }
}