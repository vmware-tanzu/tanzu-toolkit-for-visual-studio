using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace TanzuForVS.CloudFoundryApiClient
{
    public class UaaClient : IUaaClient
    {
        private static HttpClient _httpClient;
        private static bool skipSsl = true;
        public Token Token { get; private set; }

        public UaaClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<HttpStatusCode> RequestAccessTokenAsync(Uri uaaUri, string uaaClientId, string uaaClientSecret, string cfUsername, string cfPassword)
        {
            try
            {
                if (skipSsl)
                {
                    // trust any certificate
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    ServicePointManager.ServerCertificateValidationCallback +=
                        (sender, cert, chain, sslPolicyErrors) => { return true; };
                }

                string uriString = uaaUri.ToString();
                var uri = new UriBuilder(uriString);
                uri.Path = "/oauth/token";

                var postBody = new List<KeyValuePair<string, string>>();
                postBody.Add(new KeyValuePair<string, string>("grant_type", "password"));
                postBody.Add(new KeyValuePair<string, string>("username", cfUsername));
                postBody.Add(new KeyValuePair<string, string>("password", cfPassword));

                var content = new FormUrlEncodedContent(postBody);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

                string basicAuthStringToEncode = uaaClientId + ":" + uaaClientSecret;
                string encodedBasicAuthString = Base64Encode(basicAuthStringToEncode);

                var request = new HttpRequestMessage(HttpMethod.Post, uri.ToString());
                request.Content = content;
                request.Headers.Add("Accept", "application/json");
                request.Headers.Add("Authorization", "Basic " + encodedBasicAuthString);

                var result = await _httpClient.SendAsync(request);

                if (result.StatusCode == HttpStatusCode.OK)
                {
                    string resultContent = await result.Content.ReadAsStringAsync();
                    Token = JsonConvert.DeserializeObject<Token>(resultContent);
                }

                return result.StatusCode;

            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
                throw new Exception("Authentication error");
            }
        }

        private static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }
    }
}