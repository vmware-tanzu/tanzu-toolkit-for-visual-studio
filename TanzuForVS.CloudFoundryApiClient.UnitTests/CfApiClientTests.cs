using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using RichardSzalay.MockHttp;
using System;
using System.Net;
using System.Threading.Tasks;

namespace TanzuForVS.CloudFoundryApiClient.UnitTests
{
    [TestClass()]
    public class CfApiClientTests
    {
        private CfApiClient _sut;
        private Mock<IUaaClient> _mockUaaClient = new Mock<IUaaClient>();
        private static readonly MockHttpMessageHandler _mockHttp = new MockHttpMessageHandler();

        private static readonly string _fakeTargetDomain = "myfaketarget.com";
        private static readonly string _fakeCfApiAddress = $"https://api.{_fakeTargetDomain}";
        private static readonly string _fakeLoginAddress = $"https://login.{_fakeTargetDomain}";
        private static readonly string _fakeUaaAddress = $"https://uaa.{_fakeTargetDomain}";
        private static readonly string _fakeCfUsername = "user";
        private static readonly string _fakeCfPassword = "pass";

        private static readonly BasicInfoResponse _fakeResponse = new BasicInfoResponse
        {
            links = new Links
            {
                login = new Login
                {
                    href = _fakeLoginAddress
                },
                uaa = new Uaa
                {
                    href = _fakeUaaAddress
                }
            }
        };
        private static readonly string _fakeJsonResponse = JsonConvert.SerializeObject(_fakeResponse);

        [TestInitialize()]
        public void TestInit()
        {
            _mockHttp.Fallback.Throw(new InvalidOperationException("No matching mock handler"));
        }

        [TestCleanup()]
        public void TestCleanup()
        {
            _mockHttp.VerifyNoOutstandingExpectation();
        }

        [TestMethod()]
        public async Task LoginAsync_UpdatesAndReturnsAccessToken_WhenLoginSucceeds()
        {
            var expectedUri = new Uri(_fakeLoginAddress);
            var fakeAccessTokenContent = "fake access token";

            MockedRequest cfBasicInfoRequest = _mockHttp.Expect("https://api." + _fakeTargetDomain + "/")
               .Respond("application/json", _fakeJsonResponse);

            _mockUaaClient.Setup(mock => mock.RequestAccessTokenAsync(
                expectedUri,
                CfApiClient.defaultAuthClientId,
                CfApiClient.defaultAuthClientSecret,
                _fakeCfUsername,
                _fakeCfPassword))
                .ReturnsAsync(HttpStatusCode.OK);

            _mockUaaClient.SetupGet(mock => mock.Token)
                .Returns(new Token() { access_token = fakeAccessTokenContent });

            _sut = new CfApiClient(_mockUaaClient.Object, _mockHttp.ToHttpClient());

            Assert.AreNotEqual(fakeAccessTokenContent, _sut.AccessToken);

            var result = await _sut.LoginAsync(_fakeCfApiAddress, _fakeCfUsername, _fakeCfPassword);

            Assert.AreEqual(fakeAccessTokenContent, _sut.AccessToken);
            Assert.AreEqual(fakeAccessTokenContent, result);
            _mockUaaClient.VerifyAll();
            Assert.AreEqual(1, _mockHttp.GetMatchCount(cfBasicInfoRequest));
        }

        [TestMethod()]
        public async Task LoginAsync_ReturnsNull_AndDoesNotUpdateAccessToken_WhenLoginFails()
        {
            var expectedUri = new Uri(_fakeLoginAddress);

            MockedRequest cfBasicInfoRequest = _mockHttp.Expect("https://api." + _fakeTargetDomain + "/")
               .Respond("application/json", _fakeJsonResponse);

            _mockUaaClient.Setup(mock => mock.RequestAccessTokenAsync(
                expectedUri,
                CfApiClient.defaultAuthClientId,
                CfApiClient.defaultAuthClientSecret,
                _fakeCfUsername,
                _fakeCfPassword))
                .ReturnsAsync(HttpStatusCode.Unauthorized);

            _sut = new CfApiClient(_mockUaaClient.Object, _mockHttp.ToHttpClient());

            var initialAccessToken = _sut.AccessToken;

            var result = await _sut.LoginAsync(_fakeCfApiAddress, _fakeCfUsername, _fakeCfPassword);

            Assert.IsNull(result);
            Assert.AreEqual(initialAccessToken, _sut.AccessToken);
            _mockUaaClient.VerifyAll();
            Assert.AreEqual(1, _mockHttp.GetMatchCount(cfBasicInfoRequest));
        }

        [TestMethod()]
        public async Task LoginAsync_ThrowsException_WhenCfTargetIsMalformed()
        {
            Exception expectedException = null;

            MockedRequest cfBasicInfoRequest = _mockHttp.When("*")
               .Respond("application/json", _fakeJsonResponse);


            _sut = new CfApiClient(_mockUaaClient.Object, _mockHttp.ToHttpClient());

            try
            {
                var malformedCfTargetString = "what-a-mess";
                await _sut.LoginAsync(malformedCfTargetString, _fakeCfUsername, _fakeCfPassword);
            }
            catch (Exception e)
            {
                expectedException = e;
            }

            Assert.IsNotNull(expectedException);
            _mockUaaClient.Verify(mock =>
                mock.RequestAccessTokenAsync(
                    It.IsAny<Uri>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()), Times.Never);
            Assert.AreEqual(0, _mockHttp.GetMatchCount(cfBasicInfoRequest));
        }
    }
}