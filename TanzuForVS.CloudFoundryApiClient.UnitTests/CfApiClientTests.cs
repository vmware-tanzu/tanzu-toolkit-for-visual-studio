using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using RichardSzalay.MockHttp;
using System;
using System.Net;
using System.Threading.Tasks;
using TanzuForVS.CloudFoundryApiClient.Models.AppsResponse;
using TanzuForVS.CloudFoundryApiClient.Models.Token;

namespace TanzuForVS.CloudFoundryApiClient.UnitTests
{
    [TestClass()]
    public class CfApiClientTests : CfApiClientTestSupport
    {
        private CfApiClient _sut;
        private Mock<IUaaClient> _mockUaaClient = new Mock<IUaaClient>();
        private static readonly MockHttpMessageHandler _mockHttp = new MockHttpMessageHandler();

        [TestInitialize()]
        public void TestInit()
        {
            _mockHttp.ResetExpectations();
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
               .Respond("application/json", _fakeBasicInfoJsonResponse);

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
               .Respond("application/json", _fakeBasicInfoJsonResponse);

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

            MockedRequest catchallRequest = _mockHttp.When("*")
               .Throw(new Exception("Malformed uri exception should be thrown before httpClient is used"));

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
            Assert.AreEqual(0, _mockHttp.GetMatchCount(catchallRequest));
        }

        [TestMethod()]
        public async Task LoginAsync_ThrowsException_WhenBasicInfoRequestErrors()
        {
            Exception resultException = null;
            var fakeHttpExceptionMessage = "(fake) http request failed";

            MockedRequest cfBasicInfoRequest = _mockHttp.Expect(_fakeCfApiAddress + "/")
               .Throw(new Exception(fakeHttpExceptionMessage));

            _sut = new CfApiClient(_mockUaaClient.Object, _mockHttp.ToHttpClient());

            try
            {
                await _sut.LoginAsync(_fakeCfApiAddress, _fakeCfUsername, _fakeCfPassword);
            }
            catch (Exception e)
            {
                resultException = e;
            }

            Assert.AreEqual(1, _mockHttp.GetMatchCount(cfBasicInfoRequest));
            Assert.IsNotNull(resultException);
            Assert.IsTrue(resultException.Message.Contains(fakeHttpExceptionMessage));
            Assert.IsTrue(
                resultException.Message.Contains(CfApiClient.AuthServerLookupFailureMessage)
                || (resultException.Data.Contains("MessageToDisplay")
                    && resultException.Data["MessageToDisplay"].ToString().Contains(CfApiClient.AuthServerLookupFailureMessage))
            );

            _mockUaaClient.Verify(mock =>
                mock.RequestAccessTokenAsync(
                    It.IsAny<Uri>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()), Times.Never);
        }

        [TestMethod()]
        public async Task ListOrgs_ReturnsNull_WhenStatusCodeIsNotASuccess()
        {
            string expectedPath = CfApiClient.listOrgsPath;

            MockedRequest orgsRequest = _mockHttp.Expect(_fakeCfApiAddress + expectedPath)
                .WithHeaders("Authorization", $"Bearer {_fakeAccessToken}")
                .Respond(HttpStatusCode.Unauthorized);

            _sut = new CfApiClient(_mockUaaClient.Object, _mockHttp.ToHttpClient());

            var result = await _sut.ListOrgs(_fakeCfApiAddress, _fakeAccessToken);

            Assert.IsNull(result);
            Assert.AreEqual(1, _mockHttp.GetMatchCount(orgsRequest));
        }

        [TestMethod()]
        public async Task ListOrgs_ReturnsListOfAllVisibleOrgs_WhenResponseContainsMultiplePages()
        {
            string expectedPath = CfApiClient.listOrgsPath;
            string page2Identifier = "?page=2&per_page=3";
            string page3Identifier = "?page=3&per_page=3";
            string page4Identifier = "?page=4&per_page=3";

            MockedRequest orgsRequest = _mockHttp.Expect(_fakeCfApiAddress + expectedPath)
                    .WithHeaders("Authorization", $"Bearer {_fakeAccessToken}")
                    .Respond("application/json", _fakeOrgsJsonResponsePage1);

            MockedRequest orgsPage2Request = _mockHttp.Expect(_fakeCfApiAddress + expectedPath + page2Identifier)
                .WithHeaders("Authorization", $"Bearer {_fakeAccessToken}")
                .Respond("application/json", _fakeOrgsJsonResponsePage2);

            MockedRequest orgsPage3Request = _mockHttp.Expect(_fakeCfApiAddress + expectedPath + page3Identifier)
                .WithHeaders("Authorization", $"Bearer {_fakeAccessToken}")
                .Respond("application/json", _fakeOrgsJsonResponsePage3);

            MockedRequest orgsPage4Request = _mockHttp.Expect(_fakeCfApiAddress + expectedPath + page4Identifier)
                .WithHeaders("Authorization", $"Bearer {_fakeAccessToken}")
                .Respond("application/json", _fakeOrgsJsonResponsePage4);

            _sut = new CfApiClient(_mockUaaClient.Object, _mockHttp.ToHttpClient());

            var result = await _sut.ListOrgs(_fakeCfApiAddress, _fakeAccessToken);

            Assert.IsNotNull(result);
            Assert.AreEqual(10, result.Count);
            Assert.AreEqual(1, _mockHttp.GetMatchCount(orgsRequest));
            Assert.AreEqual(1, _mockHttp.GetMatchCount(orgsPage2Request));
            Assert.AreEqual(1, _mockHttp.GetMatchCount(orgsPage3Request));
            Assert.AreEqual(1, _mockHttp.GetMatchCount(orgsPage4Request));
        }

        [TestMethod()]
        public async Task ListSpacesWithGuid_ReturnsNull_WhenStatusCodeIsNotASuccess()
        {
            string expectedPath = CfApiClient.listSpacesPath;

            MockedRequest spacesRequest = _mockHttp.Expect(_fakeCfApiAddress + expectedPath)
                .WithHeaders("Authorization", $"Bearer {_fakeAccessToken}")
                .Respond(HttpStatusCode.Unauthorized);

            _sut = new CfApiClient(_mockUaaClient.Object, _mockHttp.ToHttpClient());

            var result = await _sut.ListSpacesForOrg(_fakeCfApiAddress, _fakeAccessToken, "orgGuid");

            Assert.IsNull(result);
            Assert.AreEqual(1, _mockHttp.GetMatchCount(spacesRequest));
        }

        [TestMethod()]
        public async Task ListSpacesWithGuid_ReturnsListOfAllVisibleSpaces_WhenResponseContainsMultiplePages()
        {
            string expectedPath = CfApiClient.listSpacesPath;
            string page2Identifier = "?page=2&per_page=3";
            string page3Identifier = "?page=3&per_page=3";

            MockedRequest spacesRequest = _mockHttp.Expect(_fakeCfApiAddress + expectedPath)
                    .WithHeaders("Authorization", $"Bearer {_fakeAccessToken}")
                    .Respond("application/json", _fakeSpacesJsonResponsePage1);

            MockedRequest spacesPage2Request = _mockHttp.Expect(_fakeCfApiAddress + expectedPath + page2Identifier)
                .WithHeaders("Authorization", $"Bearer {_fakeAccessToken}")
                .Respond("application/json", _fakeSpacesJsonResponsePage2);

            MockedRequest spacesPage3Request = _mockHttp.Expect(_fakeCfApiAddress + expectedPath + page3Identifier)
                .WithHeaders("Authorization", $"Bearer {_fakeAccessToken}")
                .Respond("application/json", _fakeSpacesJsonResponsePage3);

            _sut = new CfApiClient(_mockUaaClient.Object, _mockHttp.ToHttpClient());

            var result = await _sut.ListSpacesForOrg(_fakeCfApiAddress, _fakeAccessToken, "fake guid");

            Assert.IsNotNull(result);
            Assert.AreEqual(7, result.Count);
            Assert.AreEqual(1, _mockHttp.GetMatchCount(spacesRequest));
            Assert.AreEqual(1, _mockHttp.GetMatchCount(spacesPage2Request));
            Assert.AreEqual(1, _mockHttp.GetMatchCount(spacesPage3Request));
        }

        [TestMethod()]
        public async Task ListAppsWithGuid_ReturnsNull_WhenStatusCodeIsNotASuccess()
        {
            string expectedPath = CfApiClient.listAppsPath;

            MockedRequest appsRequest = _mockHttp.Expect(_fakeCfApiAddress + expectedPath)
                .WithHeaders("Authorization", $"Bearer {_fakeAccessToken}")
                .Respond(HttpStatusCode.Unauthorized);

            _sut = new CfApiClient(_mockUaaClient.Object, _mockHttp.ToHttpClient());

            var result = await _sut.ListAppsForSpace(_fakeCfApiAddress, _fakeAccessToken, "spaceGuid");

            Assert.IsNull(result);
            Assert.AreEqual(1, _mockHttp.GetMatchCount(appsRequest));
        }

        [TestMethod()]
        public async Task ListAppsWithGuid_ReturnsListOfAllVisibleApps_WhenResponseContainsMultiplePages()
        {
            string expectedPath = CfApiClient.listAppsPath;
            string page2Identifier = "?page=2&per_page=50";
            string page3Identifier = "?page=3&per_page=50";

            MockedRequest appsRequest = _mockHttp.Expect(_fakeCfApiAddress + expectedPath)
                    .WithHeaders("Authorization", $"Bearer {_fakeAccessToken}")
                    .Respond("application/json", _fakeAppsJsonResponsePage1);

            MockedRequest appsPage2Request = _mockHttp.Expect(_fakeCfApiAddress + expectedPath + page2Identifier)
                .WithHeaders("Authorization", $"Bearer {_fakeAccessToken}")
                .Respond("application/json", _fakeAppsJsonResponsePage2);

            MockedRequest appsPage3Request = _mockHttp.Expect(_fakeCfApiAddress + expectedPath + page3Identifier)
                .WithHeaders("Authorization", $"Bearer {_fakeAccessToken}")
                .Respond("application/json", _fakeAppsJsonResponsePage3);

            _sut = new CfApiClient(_mockUaaClient.Object, _mockHttp.ToHttpClient());

            var result = await _sut.ListAppsForSpace(_fakeCfApiAddress, _fakeAccessToken, "fake guid");

            Assert.IsNotNull(result);
            Assert.AreEqual(125, result.Count);
            Assert.AreEqual(1, _mockHttp.GetMatchCount(appsRequest));
            Assert.AreEqual(1, _mockHttp.GetMatchCount(appsPage2Request));
            Assert.AreEqual(1, _mockHttp.GetMatchCount(appsPage3Request));
        }

        [TestMethod]
        public async Task StopAppWithGuid_ReturnsFalse_WhenStatusCodeIsNotASuccess()
        {
            string fakeAppGuid = "1234";
            string expectedPath = _fakeCfApiAddress + CfApiClient.listAppsPath + $"/{fakeAppGuid}/actions/stop";
            var fakeHttpExceptionMessage = "(fake) http request failed";
            Exception resultException = null;

            MockedRequest cfStopAppRequest = _mockHttp.Expect(expectedPath)
               .Throw(new Exception(fakeHttpExceptionMessage));

            _sut = new CfApiClient(_mockUaaClient.Object, _mockHttp.ToHttpClient());

            bool stopResult = true;
            try
            {
                stopResult = await _sut.StopAppWithGuid(_fakeCfApiAddress, _fakeAccessToken, fakeAppGuid);
            }
            catch (Exception e)
            {
                resultException = e;
            }

            Assert.AreEqual(1, _mockHttp.GetMatchCount(cfStopAppRequest));
            Assert.IsNull(resultException);
            Assert.IsFalse(stopResult);
        }

        [TestMethod]
        public async Task StopAppWithGuid_ReturnsTrue_WhenAppStateIsSTOPPED()
        {
            string fakeAppGuid = "1234";
            string expectedPath = _fakeCfApiAddress + CfApiClient.listAppsPath + $"/{fakeAppGuid}/actions/stop";
            Exception resultException = null;

            MockedRequest cfStopAppRequest = _mockHttp.Expect(expectedPath)
               .Respond("application/json", JsonConvert.SerializeObject(new App { state = "STOPPED" }));

            _sut = new CfApiClient(_mockUaaClient.Object, _mockHttp.ToHttpClient());

            bool stopResult = false;
            try
            {
                stopResult = await _sut.StopAppWithGuid(_fakeCfApiAddress, _fakeAccessToken, fakeAppGuid);
            }
            catch (Exception e)
            {
                resultException = e;
            }

            Assert.AreEqual(1, _mockHttp.GetMatchCount(cfStopAppRequest));
            Assert.IsNull(resultException);
            Assert.IsTrue(stopResult);
        }

        [TestMethod]
        public async Task StopAppWithGuid_ReturnsFalse_WhenAppStateIsNotSTOPPED()
        {
            string fakeAppGuid = "1234";
            string expectedPath = _fakeCfApiAddress + CfApiClient.listAppsPath + $"/{fakeAppGuid}/actions/stop";
            Exception resultException = null;

            MockedRequest cfStopAppRequest = _mockHttp.Expect(expectedPath)
               .Respond("application/json", JsonConvert.SerializeObject(new App { state = "fake state != STOPPED" }));

            _sut = new CfApiClient(_mockUaaClient.Object, _mockHttp.ToHttpClient());

            bool stopResult = true;
            try
            {
                stopResult = await _sut.StopAppWithGuid(_fakeCfApiAddress, _fakeAccessToken, fakeAppGuid);
            }
            catch (Exception e)
            {
                resultException = e;
            }

            Assert.AreEqual(1, _mockHttp.GetMatchCount(cfStopAppRequest));
            Assert.IsNull(resultException);
            Assert.IsFalse(stopResult);
        }

        [TestMethod]
        public async Task StartAppWithGuid_ReturnsTrue_WhenAppStateIsSTARTED()
        {
            string fakeAppGuid = "1234";
            string expectedPath = _fakeCfApiAddress + CfApiClient.listAppsPath + $"/{fakeAppGuid}/actions/start";
            Exception resultException = null;

            MockedRequest cfStartAppRequest = _mockHttp.Expect(expectedPath)
               .Respond("application/json", JsonConvert.SerializeObject(new App { state = "STARTED" }));

            _sut = new CfApiClient(_mockUaaClient.Object, _mockHttp.ToHttpClient());

            bool startResult = false;
            try
            {
                startResult = await _sut.StartAppWithGuid(_fakeCfApiAddress, _fakeAccessToken, fakeAppGuid);
            }
            catch (Exception e)
            {
                resultException = e;
            }

            Assert.AreEqual(1, _mockHttp.GetMatchCount(cfStartAppRequest));
            Assert.IsNull(resultException);
            Assert.IsTrue(startResult);
        }

        [TestMethod]
        public async Task StartAppWithGuid_ReturnsFalse_WhenAppStateIsNotSTARTED()
        {
            string fakeAppGuid = "1234";
            string expectedPath = _fakeCfApiAddress + CfApiClient.listAppsPath + $"/{fakeAppGuid}/actions/start";
            Exception resultException = null;

            MockedRequest cfStartAppRequest = _mockHttp.Expect(expectedPath)
               .Respond("application/json", JsonConvert.SerializeObject(new App { state = "fake state != STARTED" }));

            _sut = new CfApiClient(_mockUaaClient.Object, _mockHttp.ToHttpClient());

            bool startResult = true;
            try
            {
                startResult = await _sut.StartAppWithGuid(_fakeCfApiAddress, _fakeAccessToken, fakeAppGuid);
            }
            catch (Exception e)
            {
                resultException = e;
            }

            Assert.AreEqual(1, _mockHttp.GetMatchCount(cfStartAppRequest));
            Assert.IsNull(resultException);
            Assert.IsFalse(startResult);
        }

        [TestMethod]
        public async Task StartAppWithGuid_ReturnsFalse_WhenStatusCodeIsNotASuccess()
        {
            string fakeAppGuid = "1234";
            string expectedPath = _fakeCfApiAddress + CfApiClient.listAppsPath + $"/{fakeAppGuid}/actions/start";
            var fakeHttpExceptionMessage = "(fake) http request failed";
            Exception resultException = null;

            MockedRequest cfStartAppRequest = _mockHttp.Expect(expectedPath)
               .Throw(new Exception(fakeHttpExceptionMessage));

            _sut = new CfApiClient(_mockUaaClient.Object, _mockHttp.ToHttpClient());

            bool startResult = true;
            try
            {
                startResult = await _sut.StartAppWithGuid(_fakeCfApiAddress, _fakeAccessToken, fakeAppGuid);
            }
            catch (Exception e)
            {
                resultException = e;
            }

            Assert.AreEqual(1, _mockHttp.GetMatchCount(cfStartAppRequest));
            Assert.IsNull(resultException);
            Assert.IsFalse(startResult);
        }
    }
}