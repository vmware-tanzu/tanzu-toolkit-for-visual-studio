using Microsoft.VisualStudio.TestTools.UnitTesting;
using RichardSzalay.MockHttp;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Tanzu.Toolkit.CloudFoundryApiClient.Models;
using Tanzu.Toolkit.CloudFoundryApiClient.Models.AppsResponse;

namespace Tanzu.Toolkit.CloudFoundryApiClient.Tests
{
    [TestClass]
    public class CfApiClientTests : CfApiClientTestSupport
    {
        private CfApiClient _sut;
        private MockHttpMessageHandler _mockHttp;
        private readonly Uri _defaultApiAddressConfigValue = new Uri(_fakeCfApiAddress);
        private readonly bool _defaultSkipCertValidationConfigValue = false;

        [TestInitialize]
        public void TestInit()
        {
            var fakeHttpClientFactory = (IFakeHttpClientFactory)_fakeHttpClientFactory;
            _mockHttp = fakeHttpClientFactory.MockHttpMessageHandler;
            
            _sut = new CfApiClient(_fakeHttpClientFactory);
            _sut.Configure(_defaultApiAddressConfigValue, _defaultSkipCertValidationConfigValue);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _mockHttp.VerifyNoOutstandingExpectation();
        }

        [TestMethod]
        public async Task ListOrgs_ThrowsException_WhenStatusCodeIsNotASuccess()
        {
            var expectedPath = CfApiClient.ListOrgsPath;

            var orgsRequest = _mockHttp.Expect(_fakeCfApiAddress + expectedPath)
                .WithHeaders("Authorization", $"Bearer {_fakeAccessToken}")
                .Respond(HttpStatusCode.Unauthorized);

            Exception thrownException = null;
            try
            {
                var result = await _sut.ListOrgs(_fakeCfApiAddress, _fakeAccessToken);
            }
            catch (Exception ex)
            {
                thrownException = ex;
            }

            Assert.IsNotNull(thrownException);
            Assert.IsTrue(thrownException.Message.Contains(CfApiClient.ListOrgsPath));
            Assert.AreEqual(1, _mockHttp.GetMatchCount(orgsRequest));
        }

        [TestMethod]
        public async Task ListOrgs_ReturnsListOfAllVisibleOrgs_WhenResponseContainsMultiplePages()
        {
            var expectedPath = CfApiClient.ListOrgsPath;
            var page2Identifier = "?page=2&per_page=3";
            var page3Identifier = "?page=3&per_page=3";
            var page4Identifier = "?page=4&per_page=3";

            var orgsRequest = _mockHttp.Expect(_fakeCfApiAddress + expectedPath)
                    .WithHeaders("Authorization", $"Bearer {_fakeAccessToken}")
                    .Respond("application/json", _fakeOrgsJsonResponsePage1);

            var orgsPage2Request = _mockHttp.Expect(_fakeCfApiAddress + expectedPath + page2Identifier)
                .WithHeaders("Authorization", $"Bearer {_fakeAccessToken}")
                .Respond("application/json", _fakeOrgsJsonResponsePage2);

            var orgsPage3Request = _mockHttp.Expect(_fakeCfApiAddress + expectedPath + page3Identifier)
                .WithHeaders("Authorization", $"Bearer {_fakeAccessToken}")
                .Respond("application/json", _fakeOrgsJsonResponsePage3);

            var orgsPage4Request = _mockHttp.Expect(_fakeCfApiAddress + expectedPath + page4Identifier)
                .WithHeaders("Authorization", $"Bearer {_fakeAccessToken}")
                .Respond("application/json", _fakeOrgsJsonResponsePage4);

            var result = await _sut.ListOrgs(_fakeCfApiAddress, _fakeAccessToken);

            Assert.IsNotNull(result);
            Assert.AreEqual(10, result.Count);
            Assert.AreEqual(1, _mockHttp.GetMatchCount(orgsRequest));
            Assert.AreEqual(1, _mockHttp.GetMatchCount(orgsPage2Request));
            Assert.AreEqual(1, _mockHttp.GetMatchCount(orgsPage3Request));
            Assert.AreEqual(1, _mockHttp.GetMatchCount(orgsPage4Request));
        }

        [TestMethod]
        public async Task ListSpacesWithGuid_ThrowsException_WhenStatusCodeIsNotASuccess()
        {
            var expectedPath = CfApiClient.ListSpacesPath;

            var spacesRequest = _mockHttp.Expect(_fakeCfApiAddress + expectedPath)
                .WithHeaders("Authorization", $"Bearer {_fakeAccessToken}")
                .Respond(HttpStatusCode.Unauthorized);

            Exception thrownException = null;
            try
            {
                var result = await _sut.ListSpacesForOrg(_fakeCfApiAddress, _fakeAccessToken, "orgGuid");
            }
            catch (Exception ex)
            {
                thrownException = ex;
            }

            Assert.IsNotNull(thrownException);
            Assert.IsTrue(thrownException.Message.Contains(CfApiClient.ListSpacesPath));
            Assert.AreEqual(1, _mockHttp.GetMatchCount(spacesRequest));
        }

        [TestMethod]
        public async Task ListSpacesWithGuid_ReturnsListOfAllVisibleSpaces_WhenResponseContainsMultiplePages()
        {
            var expectedPath = CfApiClient.ListSpacesPath;
            var page2Identifier = "?page=2&per_page=3";
            var page3Identifier = "?page=3&per_page=3";

            var spacesRequest = _mockHttp.Expect(_fakeCfApiAddress + expectedPath)
                    .WithHeaders("Authorization", $"Bearer {_fakeAccessToken}")
                    .Respond("application/json", _fakeSpacesJsonResponsePage1);

            var spacesPage2Request = _mockHttp.Expect(_fakeCfApiAddress + expectedPath + page2Identifier)
                .WithHeaders("Authorization", $"Bearer {_fakeAccessToken}")
                .Respond("application/json", _fakeSpacesJsonResponsePage2);

            var spacesPage3Request = _mockHttp.Expect(_fakeCfApiAddress + expectedPath + page3Identifier)
                .WithHeaders("Authorization", $"Bearer {_fakeAccessToken}")
                .Respond("application/json", _fakeSpacesJsonResponsePage3);

            var result = await _sut.ListSpacesForOrg(_fakeCfApiAddress, _fakeAccessToken, "fake guid");

            Assert.IsNotNull(result);
            Assert.AreEqual(7, result.Count);
            Assert.AreEqual(1, _mockHttp.GetMatchCount(spacesRequest));
            Assert.AreEqual(1, _mockHttp.GetMatchCount(spacesPage2Request));
            Assert.AreEqual(1, _mockHttp.GetMatchCount(spacesPage3Request));
        }

        [TestMethod]
        public async Task ListAppsWithGuid_ThrowsException_WhenStatusCodeIsNotASuccess()
        {
            var expectedPath = CfApiClient.ListAppsPath;

            var appsRequest = _mockHttp.Expect(_fakeCfApiAddress + expectedPath)
                .WithHeaders("Authorization", $"Bearer {_fakeAccessToken}")
                .Respond(HttpStatusCode.Unauthorized);

            Exception thrownException = null;
            try
            {
                var result = await _sut.ListAppsForSpace(_fakeCfApiAddress, _fakeAccessToken, "spaceGuid");
            }
            catch (Exception ex)
            {
                thrownException = ex;
            }

            Assert.IsNotNull(thrownException);
            Assert.IsTrue(thrownException.Message.Contains(CfApiClient.ListAppsPath));
            Assert.AreEqual(1, _mockHttp.GetMatchCount(appsRequest));
        }

        [TestMethod]
        public async Task ListAppsWithGuid_ReturnsListOfAllVisibleApps_WhenResponseContainsMultiplePages()
        {
            var expectedPath = CfApiClient.ListAppsPath;
            var page2Identifier = "?page=2&per_page=50";
            var page3Identifier = "?page=3&per_page=50";

            var appsRequest = _mockHttp.Expect(_fakeCfApiAddress + expectedPath)
                    .WithHeaders("Authorization", $"Bearer {_fakeAccessToken}")
                    .Respond("application/json", _fakeAppsJsonResponsePage1);

            var appsPage2Request = _mockHttp.Expect(_fakeCfApiAddress + expectedPath + page2Identifier)
                .WithHeaders("Authorization", $"Bearer {_fakeAccessToken}")
                .Respond("application/json", _fakeAppsJsonResponsePage2);

            var appsPage3Request = _mockHttp.Expect(_fakeCfApiAddress + expectedPath + page3Identifier)
                .WithHeaders("Authorization", $"Bearer {_fakeAccessToken}")
                .Respond("application/json", _fakeAppsJsonResponsePage3);

            var result = await _sut.ListAppsForSpace(_fakeCfApiAddress, _fakeAccessToken, "fake guid");

            Assert.IsNotNull(result);
            Assert.AreEqual(125, result.Count);
            Assert.AreEqual(1, _mockHttp.GetMatchCount(appsRequest));
            Assert.AreEqual(1, _mockHttp.GetMatchCount(appsPage2Request));
            Assert.AreEqual(1, _mockHttp.GetMatchCount(appsPage3Request));
        }

        [TestMethod]
        public async Task ListRoutesForApp_ThrowsException_WhenStatusCodeIsNotASuccess()
        {
            var expectedPath = CfApiClient.ListRoutesPath;

            var routesRequest = _mockHttp.Expect(_fakeCfApiAddress + expectedPath)
                .WithHeaders("Authorization", $"Bearer {_fakeAccessToken}")
                .Respond(HttpStatusCode.Unauthorized);

            Exception expectedException = null;
            try
            {
                var result = await _sut.ListRoutesForApp(_fakeCfApiAddress, _fakeAccessToken, "appGuid");
            }
            catch (Exception ex)
            {
                expectedException = ex;
            }

            Assert.IsNotNull(expectedException);
            Assert.AreEqual(1, _mockHttp.GetMatchCount(routesRequest));
        }

        [TestMethod]
        public async Task ListRoutesForApp_ReturnsListOfAllVisibleRoutes_WhenResponseContainsMultiplePages()
        {
            var expectedPath = CfApiClient.ListRoutesPath;
            var page2Identifier = "?page=2&per_page=50";
            var page3Identifier = "?page=3&per_page=50";

            var routesRequest = _mockHttp.Expect(_fakeCfApiAddress + expectedPath)
                    .WithHeaders("Authorization", $"Bearer {_fakeAccessToken}")
                    .Respond("application/json", _fakeRoutesJsonResponsePage1);

            var routesPage2Request = _mockHttp.Expect(_fakeCfApiAddress + expectedPath + page2Identifier)
                .WithHeaders("Authorization", $"Bearer {_fakeAccessToken}")
                .Respond("application/json", _fakeRoutesJsonResponsePage2);

            var routesPage3Request = _mockHttp.Expect(_fakeCfApiAddress + expectedPath + page3Identifier)
                .WithHeaders("Authorization", $"Bearer {_fakeAccessToken}")
                .Respond("application/json", _fakeRoutesJsonResponsePage3);

            var result = await _sut.ListRoutesForApp(_fakeCfApiAddress, _fakeAccessToken, "fake guid");

            Assert.IsNotNull(result);
            Assert.AreEqual(125, result.Count);
            Assert.AreEqual(1, _mockHttp.GetMatchCount(routesRequest));
            Assert.AreEqual(1, _mockHttp.GetMatchCount(routesPage2Request));
            Assert.AreEqual(1, _mockHttp.GetMatchCount(routesPage3Request));
        }

        [TestMethod]
        public async Task StopAppWithGuid_ThrowsException_WhenStatusCodeIsNotASuccess()
        {
            var fakeAppGuid = "1234";
            var expectedPath = _fakeCfApiAddress + CfApiClient.ListAppsPath + $"/{fakeAppGuid}/actions/stop";
            Exception thrownException = null;

            var appsRequest = _mockHttp.Expect(expectedPath)
               .WithHeaders("Authorization", $"Bearer {_fakeAccessToken}")
               .Respond(HttpStatusCode.Unauthorized);

            try
            {
                await _sut.StopAppWithGuid(_fakeCfApiAddress, _fakeAccessToken, fakeAppGuid);
            }
            catch (Exception e)
            {
                thrownException = e;
            }

            Assert.IsNotNull(thrownException);
            Assert.IsTrue(thrownException.Message.Contains(CfApiClient.ListAppsPath));
            Assert.AreEqual(1, _mockHttp.GetMatchCount(appsRequest));
        }

        [TestMethod]
        public async Task StopAppWithGuid_ReturnsTrue_WhenAppStateIsSTOPPED()
        {
            var fakeAppGuid = "1234";
            var expectedPath = _fakeCfApiAddress + CfApiClient.ListAppsPath + $"/{fakeAppGuid}/actions/stop";
            Exception resultException = null;

            var cfStopAppRequest = _mockHttp.Expect(expectedPath)
               .Respond("application/json", JsonSerializer.Serialize(new App { State = "STOPPED" }));

            var stopResult = false;
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
            var fakeAppGuid = "1234";
            var expectedPath = _fakeCfApiAddress + CfApiClient.ListAppsPath + $"/{fakeAppGuid}/actions/stop";
            Exception resultException = null;

            var cfStopAppRequest = _mockHttp.Expect(expectedPath)
               .Respond("application/json", JsonSerializer.Serialize(new App { State = "fake state != STOPPED" }));

            var stopResult = true;
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
            var fakeAppGuid = "1234";
            var expectedPath = _fakeCfApiAddress + CfApiClient.ListAppsPath + $"/{fakeAppGuid}/actions/start";
            Exception resultException = null;

            var cfStartAppRequest = _mockHttp.Expect(expectedPath)
               .Respond("application/json", JsonSerializer.Serialize(new App { State = "STARTED" }));            var startResult = false;

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
            var fakeAppGuid = "1234";
            var expectedPath = _fakeCfApiAddress + CfApiClient.ListAppsPath + $"/{fakeAppGuid}/actions/start";
            Exception resultException = null;

            var cfStartAppRequest = _mockHttp.Expect(expectedPath)
               .Respond("application/json", JsonSerializer.Serialize(new App { State = "fake state != STARTED" }));

            var startResult = true;
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
        public async Task StartAppWithGuid_ThrowsException_WhenStatusCodeIsNotASuccess()
        {
            var fakeAppGuid = "1234";
            var expectedPath = _fakeCfApiAddress + CfApiClient.ListAppsPath + $"/{fakeAppGuid}/actions/start";
            Exception thrownException = null;

            var appsRequest = _mockHttp.Expect(expectedPath)
               .WithHeaders("Authorization", $"Bearer {_fakeAccessToken}")
               .Respond(HttpStatusCode.Unauthorized);

            try
            {
                await _sut.StartAppWithGuid(_fakeCfApiAddress, _fakeAccessToken, fakeAppGuid);
            }
            catch (Exception e)
            {
                thrownException = e;
            }

            Assert.IsNotNull(thrownException);
            Assert.IsTrue(thrownException.Message.Contains(CfApiClient.ListAppsPath));
            Assert.AreEqual(1, _mockHttp.GetMatchCount(appsRequest));
        }

        [TestMethod]
        public async Task DeleteAppWithGuid_ReturnsTrue_WhenStatusCodeIs202()
        {
            var fakeAppGuid = "my fake guid";
            var expectedPath = _fakeCfApiAddress + CfApiClient.DeleteAppsPath + $"/{fakeAppGuid}";

            var cfDeleteAppRequest = _mockHttp.Expect(expectedPath)
               .Respond(HttpStatusCode.Accepted);

            Exception resultException = null;
            var appWasDeleted = false;
            try
            {
                appWasDeleted = await _sut.DeleteAppWithGuid(_fakeCfApiAddress, _fakeAccessToken, fakeAppGuid);
            }
            catch (Exception e)
            {
                resultException = e;
            }

            Assert.AreEqual(1, _mockHttp.GetMatchCount(cfDeleteAppRequest));
            Assert.IsNull(resultException);
            Assert.IsTrue(appWasDeleted);
        }

        [TestMethod]
        public async Task DeleteAppWithGuid_ThrowsException_WhenStatusCodeIsNot202()
        {
            Exception thrownException = null;
            var fakeAppGuid = "my fake guid";
            var expectedPath = _fakeCfApiAddress + CfApiClient.DeleteAppsPath + $"/{fakeAppGuid}";

            var cfDeleteAppRequest = _mockHttp.Expect(expectedPath)
               .Respond(HttpStatusCode.BadRequest);

            try
            {
                await _sut.DeleteAppWithGuid(_fakeCfApiAddress, _fakeAccessToken, fakeAppGuid);
            }
            catch (Exception e)
            {
                thrownException = e;
            }

            Assert.AreEqual(1, _mockHttp.GetMatchCount(cfDeleteAppRequest));
            Assert.IsTrue(thrownException.Message.Contains(CfApiClient.DeleteAppsPath));
            Assert.IsNotNull(thrownException);
        }

        [TestMethod]
        public async Task ListStacks_ThrowsException_WhenStatusCodeIsNotASuccess()
        {
            var expectedPath = CfApiClient.ListStacksPath;

            var stacksRequest = _mockHttp.Expect(_fakeCfApiAddress + expectedPath)
                .WithHeaders("Authorization", $"Bearer {_fakeAccessToken}")
                .Respond(HttpStatusCode.Unauthorized);

            Exception thrownException = null;
            try
            {
                var result = await _sut.ListStacks(_fakeCfApiAddress, _fakeAccessToken);
            }
            catch (Exception ex)
            {
                thrownException = ex;
            }

            Assert.IsNotNull(thrownException);
            Assert.IsTrue(thrownException.Message.Contains(CfApiClient.ListStacksPath));
            Assert.AreEqual(1, _mockHttp.GetMatchCount(stacksRequest));
        }

        [TestMethod]
        public async Task ListStacks_ReturnsListOfAllVisibleStacks_WhenResponseContainsMultiplePages()
        {
            var expectedPath = CfApiClient.ListStacksPath;
            var page2Identifier = "?page=2&per_page=3";
            var page3Identifier = "?page=3&per_page=3";
            var page4Identifier = "?page=4&per_page=3";

            var stacksRequest = _mockHttp.Expect(_fakeCfApiAddress + expectedPath)
                    .WithHeaders("Authorization", $"Bearer {_fakeAccessToken}")
                    .Respond("application/json", _fakeStacksJsonResponsePage1);

            var stacksPage2Request = _mockHttp.Expect(_fakeCfApiAddress + expectedPath + page2Identifier)
                .WithHeaders("Authorization", $"Bearer {_fakeAccessToken}")
                .Respond("application/json", _fakeStacksJsonResponsePage2);

            var stacksPage3Request = _mockHttp.Expect(_fakeCfApiAddress + expectedPath + page3Identifier)
                .WithHeaders("Authorization", $"Bearer {_fakeAccessToken}")
                .Respond("application/json", _fakeStacksJsonResponsePage3);

            var stacksPage4Request = _mockHttp.Expect(_fakeCfApiAddress + expectedPath + page4Identifier)
                .WithHeaders("Authorization", $"Bearer {_fakeAccessToken}")
                .Respond("application/json", _fakeStacksJsonResponsePage4);

            var result = await _sut.ListStacks(_fakeCfApiAddress, _fakeAccessToken);

            Assert.IsNotNull(result);
            Assert.AreEqual(10, result.Count);
            Assert.AreEqual(1, _mockHttp.GetMatchCount(stacksRequest));
            Assert.AreEqual(1, _mockHttp.GetMatchCount(stacksPage2Request));
            Assert.AreEqual(1, _mockHttp.GetMatchCount(stacksPage3Request));
            Assert.AreEqual(1, _mockHttp.GetMatchCount(stacksPage4Request));
        }

        [TestMethod]
        [TestCategory("ListBuildpacks")]
        public async Task ListBuildpacks_ReturnsListOfBuildpacks_WhenResponseContainsMultiplePages()
        {
            var expectedPath = CfApiClient.ListBuildpacksPath;
            var page2Identifier = "?page=2&per_page=50";
            var page3Identifier = "?page=3&per_page=50";

            var buildpacksRequest = _mockHttp.Expect(_fakeCfApiAddress + expectedPath)
                    .WithHeaders("Authorization", $"Bearer {_fakeAccessToken}")
                    .Respond("application/json", _fakeBuildpacksJsonResponsePage1);

            var buildpacksPage2Request = _mockHttp.Expect(_fakeCfApiAddress + expectedPath + page2Identifier)
                .WithHeaders("Authorization", $"Bearer {_fakeAccessToken}")
                .Respond("application/json", _fakeBuildpacksJsonResponsePage2);

            var buildpacksPage3Request = _mockHttp.Expect(_fakeCfApiAddress + expectedPath + page3Identifier)
                .WithHeaders("Authorization", $"Bearer {_fakeAccessToken}")
                .Respond("application/json", _fakeBuildpacksJsonResponsePage3);

            var result = await _sut.ListBuildpacks(_fakeCfApiAddress, _fakeAccessToken);

            Assert.IsNotNull(result);
            Assert.AreEqual(125, result.Count);
            Assert.AreEqual(1, _mockHttp.GetMatchCount(buildpacksRequest));
            Assert.AreEqual(1, _mockHttp.GetMatchCount(buildpacksPage2Request));
            Assert.AreEqual(1, _mockHttp.GetMatchCount(buildpacksPage3Request));
        }

        [TestMethod]
        [TestCategory("ListBuildpacks")]
        public async Task ListBuildpacks_ThrowsException_WhenStatusCodeIsNotASuccess()
        {
            var expectedPath = CfApiClient.ListBuildpacksPath;

            var buildpacksRequest = _mockHttp.Expect(_fakeCfApiAddress + expectedPath)
                .WithHeaders("Authorization", $"Bearer {_fakeAccessToken}")
                .Respond(HttpStatusCode.Unauthorized);

            Exception thrownException = null;
            try
            {
                var result = await _sut.ListBuildpacks(_fakeCfApiAddress, _fakeAccessToken);
            }
            catch (Exception ex)
            {
                thrownException = ex;
            }

            Assert.IsNotNull(thrownException);
            Assert.IsTrue(thrownException.Message.Contains(CfApiClient.ListBuildpacksPath));
            Assert.AreEqual(1, _mockHttp.GetMatchCount(buildpacksRequest));
        }

        [TestMethod]
        [TestCategory("GetLoginServerInformation")]
        public async Task GetLoginServerInformation_ReturnsLoginInfoResponse_WhenRequestsSucceed()
        {
            Exception thrownException = null;

            Assert.IsTrue(_fakeBasicInfoJsonResponse.Contains(_fakeLoginAddress));

            var fakeLoginServerInfo = new LoginInfoResponse
            {
                Prompts = new System.Collections.Generic.Dictionary<string, string[]>
                {
                    { "username", new[] {"text", "Email" } },
                    { "password", new[] {"password", "Password" } },
                }
            };

            var cfBasicInfoRequest = _mockHttp.Expect(_fakeCfApiAddress + "/")
               .Respond("application/json", _fakeBasicInfoJsonResponse);

            var loginServerInfoRequest = _mockHttp.Expect(_fakeLoginAddress + "/login")
               .Respond("application/json", JsonSerializer.Serialize(fakeLoginServerInfo));

            try
            {
                var result = await _sut.GetLoginServerInformation(_fakeCfApiAddress);
            }
            catch (Exception ex)
            {
                thrownException = ex;
            }

            Assert.IsNull(thrownException);

            Assert.AreEqual(1, _mockHttp.GetMatchCount(cfBasicInfoRequest));
            Assert.AreEqual(1, _mockHttp.GetMatchCount(loginServerInfoRequest));
        }

        [TestMethod]
        [TestCategory("GetLoginServerInformation")]
        public async Task GetLoginServerInformation_ThrowsException_WhenAuthServerAddressLookupFails()
        {
            Exception thrownException = null;

            var expectedException = new Exception("Pretending auth server could not be identified");

            var cfBasicInfoRequest = _mockHttp.Expect(_fakeCfApiAddress + "/")
               .Throw(expectedException);

            try
            {
                var result = await _sut.GetLoginServerInformation(_fakeCfApiAddress);
            }
            catch (Exception ex)
            {
                thrownException = ex;
            }

            Assert.IsNotNull(thrownException);
            Assert.AreEqual(expectedException, thrownException);
            Assert.AreEqual(1, _mockHttp.GetMatchCount(cfBasicInfoRequest));
        }

        [TestMethod]
        [TestCategory("GetLoginServerInformation")]
        public async Task GetLoginServerInformation_ThrowsException_WhenJsonResponseParsingFails()
        {
            Exception thrownException = null;

            var cfBasicInfoRequest = _mockHttp.Expect(_fakeCfApiAddress + "/")
               .Respond("application/json", _fakeBasicInfoJsonResponse);

            var loginServerInfoRequest = _mockHttp.Expect(_fakeLoginAddress + "/login")
               .Respond("application/json", "this is fake response content that cannot be parsed as JSON! :)");

            try
            {
                var result = await _sut.GetLoginServerInformation(_fakeCfApiAddress);
            }
            catch (Exception ex)
            {
                thrownException = ex;
            }

            Assert.IsNotNull(thrownException);

            Assert.AreEqual(1, _mockHttp.GetMatchCount(cfBasicInfoRequest));
            Assert.AreEqual(1, _mockHttp.GetMatchCount(loginServerInfoRequest));
        }

        [TestMethod]
        [TestCategory("GetLoginServerInformation")]
        public async Task GetLoginServerInformation_ThrowsException_WhenInfoRequestStatusCodeIsNotASuccess()
        {
            Exception thrownException = null;

            var cfBasicInfoRequest = _mockHttp.Expect(_fakeCfApiAddress + "/")
               .Respond("application/json", _fakeBasicInfoJsonResponse);

            var loginServerInfoRequest = _mockHttp.Expect(_fakeLoginAddress + "/login")
                .Respond(HttpStatusCode.Unauthorized);

            try
            {
                var result = await _sut.GetLoginServerInformation(_fakeCfApiAddress);
            }
            catch (Exception ex)
            {
                thrownException = ex;
            }

            Assert.IsNotNull(thrownException);

            Assert.AreEqual(1, _mockHttp.GetMatchCount(cfBasicInfoRequest));
            Assert.AreEqual(1, _mockHttp.GetMatchCount(loginServerInfoRequest));
        }


        [TestMethod]
        [TestCategory("DeleteRouteWithGuid")]
        public async Task DeleteRouteWithGuid_ReturnsTrue_WhenStatusCodeIs202()
        {
            var fakeAppGuid = "my fake guid";
            var expectedPath = _fakeCfApiAddress + CfApiClient.DeleteRoutesPath + $"/{fakeAppGuid}";

            var cfDeleteRouteRequest = _mockHttp.Expect(expectedPath)
               .Respond(HttpStatusCode.Accepted);

            Exception resultException = null;
            var routeWasDeleted = false;
            try
            {
                routeWasDeleted = await _sut.DeleteRouteWithGuid(_fakeCfApiAddress, _fakeAccessToken, fakeAppGuid);
            }
            catch (Exception e)
            {
                resultException = e;
            }

            Assert.AreEqual(1, _mockHttp.GetMatchCount(cfDeleteRouteRequest));
            Assert.IsNull(resultException);
            Assert.IsTrue(routeWasDeleted);
        }

        [TestMethod]
        [TestCategory("DeleteRouteWithGuid")]
        public async Task DeleteRouteWithGuid_ThrowsException_WhenStatusCodeIsNot202()
        {
            Exception expectedException = null;
            var fakeAppGuid = "my fake guid";
            var expectedPath = _fakeCfApiAddress + CfApiClient.DeleteRoutesPath + $"/{fakeAppGuid}";

            var cfDeleteRouteRequest = _mockHttp.Expect(expectedPath)
               .Respond(HttpStatusCode.BadRequest);

            try
            {
                await _sut.DeleteRouteWithGuid(_fakeCfApiAddress, _fakeAccessToken, fakeAppGuid);
            }
            catch (Exception e)
            {
                expectedException = e;
            }

            Assert.AreEqual(1, _mockHttp.GetMatchCount(cfDeleteRouteRequest));
            Assert.IsNotNull(expectedException);
        }
    }
}