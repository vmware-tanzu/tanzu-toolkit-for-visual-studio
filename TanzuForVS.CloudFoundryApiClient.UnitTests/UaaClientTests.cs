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

            var expectedResult = (int)HttpStatusCode.OK;
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

            var expectedResult = (int)HttpStatusCode.Unauthorized;
            var actualResult = await _uaaClient.RequestAccessTokenAsync(uaaUri, uaaClientId, uaaClientSecret, cfUsername, cfPassword);

            Assert.AreEqual(expectedResult, actualResult);
            Assert.AreEqual(1, mockHttp.GetMatchCount(request));
        }
        
        [TestMethod()]
        public async Task RequestAccessToken_ReturnsNegative1_WhenAnExceptionOccurs()
        {
            string fakeJsonString = "this is invalid json; we should never realistically see this";

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

            var expectedResult = -1;
            var actualResult = await _uaaClient.RequestAccessTokenAsync(uaaUri, uaaClientId, uaaClientSecret, cfUsername, cfPassword);

            Assert.AreEqual(expectedResult, actualResult);
            Assert.AreEqual(1, mockHttp.GetMatchCount(request));
        }
    }
}