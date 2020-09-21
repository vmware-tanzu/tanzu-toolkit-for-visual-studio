using Microsoft.VisualStudio.TestTools.UnitTesting;
using RichardSzalay.MockHttp;
using System;
using System.Net;
using System.Threading.Tasks;

namespace TanzuForVS.CloudFoundryApiClient.UnitTests
{
    [TestClass()]
    public class UaaClientTests
    {
        [TestMethod()]
        public async Task RequestAccessToken_ReturnsResponseCode_WhenResponseCodeIs200()
        {
            string fakeJsonString = "{'token' : 'testToken'}";

            var mockHttp = new MockHttpMessageHandler();
            var request = mockHttp.When("http://some.fake.url/oauth/token")
                    .Respond("application/json", fakeJsonString);

            var mockClient = mockHttp.ToHttpClient();
            var _uaaClient = new UaaClient(mockClient);

            Uri uaaUri = new Uri("http://some.fake.url");
            string uaaClientId = null;
            string uaaClientSecret = null;
            string cfUsername = null;
            string cfPassword = null;

            var expectedResult = HttpStatusCode.OK;
            var actualResult = await _uaaClient.RequestAccessTokenAsync(uaaUri, uaaClientId, uaaClientSecret, cfUsername, cfPassword);

            Assert.AreEqual(expectedResult, actualResult);
            Assert.AreEqual(1, mockHttp.GetMatchCount(request));
        }

        [TestMethod()]
        public async Task RequestAccessToken_ReturnsResponseCode_WhenResponseCodeIsNot200()
        {
            var mockHttp = new MockHttpMessageHandler();
            var request = mockHttp.When("http://some.fake.url/oauth/token")
                    .Respond(HttpStatusCode.Unauthorized);

            var mockClient = mockHttp.ToHttpClient();
            var _uaaClient = new UaaClient(mockClient);

            Uri uaaUri = new Uri("http://some.fake.url");
            string uaaClientId = null;
            string uaaClientSecret = null;
            string cfUsername = null;
            string cfPassword = null;

            var expectedResult = HttpStatusCode.Unauthorized;
            var actualResult = await _uaaClient.RequestAccessTokenAsync(uaaUri, uaaClientId, uaaClientSecret, cfUsername, cfPassword);

            Assert.AreEqual(expectedResult, actualResult);
            Assert.AreEqual(1, mockHttp.GetMatchCount(request));
        }

        [TestMethod()]
        public async Task RequestAccessToken_ThrowsException_WhenRequestErrors()
        {
            string fakeJsonResponse = "this is unparseable json and will cause an error";

            var mockHttp = new MockHttpMessageHandler();
            var request = mockHttp.When("http://some.fake.url/oauth/token")
                    .Respond("application/json", fakeJsonResponse);

            var mockClient = mockHttp.ToHttpClient();
            var _uaaClient = new UaaClient(mockClient);

            Uri uaaUri = new Uri("http://some.fake.url");
            string uaaClientId = null;
            string uaaClientSecret = null;
            string cfUsername = null;
            string cfPassword = null;

            Exception expectedException = null;
            
            try
            {
                await _uaaClient.RequestAccessTokenAsync(uaaUri, uaaClientId, uaaClientSecret, cfUsername, cfPassword);
            }
            catch (Exception e)
            {
                expectedException = e;
            }

            Assert.IsNotNull(expectedException);
            Assert.AreEqual(1, mockHttp.GetMatchCount(request));
        }

    }
}