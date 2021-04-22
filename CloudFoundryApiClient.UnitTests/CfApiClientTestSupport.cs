using Newtonsoft.Json;
using Tanzu.Toolkit.CloudFoundryApiClient.Models;
using Tanzu.Toolkit.CloudFoundryApiClient.Models.AppsResponse;
using Tanzu.Toolkit.CloudFoundryApiClient.Models.BasicInfoResponse;
using Tanzu.Toolkit.CloudFoundryApiClient.Models.OrgsResponse;
using Tanzu.Toolkit.CloudFoundryApiClient.Models.SpacesResponse;

namespace Tanzu.Toolkit.CloudFoundryApiClient.Tests
{
    public class CfApiClientTestSupport
    {
        internal static readonly string _fakeTargetDomain = "myfaketarget.com";
        internal static readonly string _fakeCfApiAddress = $"https://api.{_fakeTargetDomain}";
        internal static readonly string _fakeLoginAddress = $"https://login.{_fakeTargetDomain}";
        internal static readonly string _fakeUaaAddress = $"https://uaa.{_fakeTargetDomain}";
        internal static readonly string _fakeCfUsername = "user";
        internal static readonly string _fakeCfPassword = "pass";
        internal static readonly string _fakeAccessToken = "fakeToken";


        internal static readonly string _fakeBasicInfoJsonResponse = JsonConvert.SerializeObject(new FakeBasicInfoResponse(
                loginHref: _fakeLoginAddress,
                uaaHref: _fakeUaaAddress));

        internal static readonly string _fakeOrgsJsonResponsePage1 = JsonConvert.SerializeObject(new FakeOrgsResponse(
            apiAddress: _fakeCfApiAddress,
            pageNum: 1,
            totalResults: 10,
            totalPages: 4,
            resultsPerPage: 3));

        internal static readonly string _fakeOrgsJsonResponsePage2 = JsonConvert.SerializeObject(new FakeOrgsResponse(
            apiAddress: _fakeCfApiAddress,
            pageNum: 2,
            totalResults: 10,
            totalPages: 4,
            resultsPerPage: 3));

        internal static readonly string _fakeOrgsJsonResponsePage3 = JsonConvert.SerializeObject(new FakeOrgsResponse(
            apiAddress: _fakeCfApiAddress,
            pageNum: 3,
            totalResults: 10,
            totalPages: 4,
            resultsPerPage: 3));

        internal static readonly string _fakeOrgsJsonResponsePage4 = JsonConvert.SerializeObject(new FakeOrgsResponse(
            apiAddress: _fakeCfApiAddress,
            pageNum: 4,
            totalResults: 10,
            totalPages: 4,
            resultsPerPage: 3));

        internal static readonly string _fakeSpacesJsonResponsePage1 = JsonConvert.SerializeObject(new FakeSpacesResponse(
            apiAddress: _fakeCfApiAddress,
            pageNum: 1,
            totalResults: 7,
            totalPages: 3,
            resultsPerPage: 3));

        internal static readonly string _fakeSpacesJsonResponsePage2 = JsonConvert.SerializeObject(new FakeSpacesResponse(
            apiAddress: _fakeCfApiAddress,
            pageNum: 2,
            totalResults: 7,
            totalPages: 3,
            resultsPerPage: 3));

        internal static readonly string _fakeSpacesJsonResponsePage3 = JsonConvert.SerializeObject(new FakeSpacesResponse(
            apiAddress: _fakeCfApiAddress,
            pageNum: 3,
            totalResults: 7,
            totalPages: 3,
            resultsPerPage: 3));

        internal static readonly string _fakeAppsJsonResponsePage1 = JsonConvert.SerializeObject(new FakeAppsResponse(
            apiAddress: _fakeCfApiAddress,
            pageNum: 1,
            totalResults: 125,
            totalPages: 3,
            resultsPerPage: 50));

        internal static readonly string _fakeAppsJsonResponsePage2 = JsonConvert.SerializeObject(new FakeAppsResponse(
            apiAddress: _fakeCfApiAddress,
            pageNum: 2,
            totalResults: 125,
            totalPages: 3,
            resultsPerPage: 50));

        internal static readonly string _fakeAppsJsonResponsePage3 = JsonConvert.SerializeObject(new FakeAppsResponse(
            apiAddress: _fakeCfApiAddress,
            pageNum: 3,
            totalResults: 125,
            totalPages: 3,
            resultsPerPage: 50));
    }

    class FakeBasicInfoResponse : BasicInfoResponse
    {
        public FakeBasicInfoResponse(string loginHref, string uaaHref) : base()
        {
            links = new Models.BasicInfoResponse.Links
            {
                login = new Href
                {
                    href = loginHref
                },
                uaa = new Href
                {
                    href = uaaHref
                }
            };
        }
    }

    class FakeOrgsResponse : OrgsResponse
    {
        public FakeOrgsResponse(string apiAddress, int pageNum, int totalResults, int totalPages, int resultsPerPage) : base()
        {
            bool isFirstPage = pageNum == 1;
            bool isLastPage = pageNum == totalPages;

            var firstHref = new Href() { href = $"{apiAddress}{CfApiClient.listOrgsPath}?page=1&per_page={resultsPerPage}" };
            var lastHref = new Href() { href = $"{apiAddress}{CfApiClient.listOrgsPath}?page={totalPages}&per_page={resultsPerPage}" };
            var nextHref = isLastPage ? null : new Href() { href = $"{apiAddress}{CfApiClient.listOrgsPath}?page={pageNum + 1}&per_page={resultsPerPage}" };
            var previousHref = isFirstPage ? null : new Href() { href = $"{apiAddress}{CfApiClient.listOrgsPath}?page={pageNum - 1}&per_page={resultsPerPage}" };

            Pagination = new Pagination
            {
                total_results = totalResults,
                total_pages = totalPages,
                first = firstHref,
                last = lastHref,
                next = nextHref,
                previous = previousHref
            };

            Org[] orgs;
            if (isLastPage)
            {
                int numResourcesInLastPage = totalResults % resultsPerPage;
                orgs = new Org[numResourcesInLastPage];

                for (int i = 0; i < numResourcesInLastPage; i++)
                {
                    orgs[i] = new Org
                    {
                        Name = $"fakeOrg{i + 1}",
                        Guid = $"fakeOrgId-{i + 1}"
                    };
                }
            }
            else
            {
                orgs = new Org[resultsPerPage];

                for (int i = 0; i < resultsPerPage; i++)
                {
                    orgs[i] = new Org
                    {
                        Name = $"fakeOrg{i + 1}",
                        Guid = $"fakeOrgId-{i + 1}"
                    };
                }
            }

            Orgs = orgs;
        }
    }

    class FakeSpacesResponse : SpacesResponse
    {
        public FakeSpacesResponse(string apiAddress, int pageNum, int totalResults, int totalPages, int resultsPerPage) : base()
        {
            bool isFirstPage = pageNum == 1;
            bool isLastPage = pageNum == totalPages;

            var firstHref = new Href() { href = $"{apiAddress}{CfApiClient.listSpacesPath}?page=1&per_page={resultsPerPage}" };
            var lastHref = new Href() { href = $"{apiAddress}{CfApiClient.listSpacesPath}?page={totalPages}&per_page={resultsPerPage}" };
            var nextHref = isLastPage ? null : new Href() { href = $"{apiAddress}{CfApiClient.listSpacesPath}?page={pageNum + 1}&per_page={resultsPerPage}" };
            var previousHref = isFirstPage ? null : new Href() { href = $"{apiAddress}{CfApiClient.listSpacesPath}?page={pageNum - 1}&per_page={resultsPerPage}" };

            Pagination = new Pagination
            {
                total_results = totalResults,
                total_pages = totalPages,
                first = firstHref,
                last = lastHref,
                next = nextHref,
                previous = previousHref
            };

            Space[] spaces;
            if (isLastPage)
            {
                int numResourcesInLastPage = totalResults % resultsPerPage;
                spaces = new Space[numResourcesInLastPage];

                for (int i = 0; i < numResourcesInLastPage; i++)
                {
                    spaces[i] = new Space
                    {
                        Name = $"fakeSpace{i + 1}",
                        Guid = $"fakeSpaceId-{i + 1}"
                    };
                }
            }
            else
            {
                spaces = new Space[resultsPerPage];

                for (int i = 0; i < resultsPerPage; i++)
                {
                    spaces[i] = new Space
                    {
                        Name = $"fakeSpace{i + 1}",
                        Guid = $"fakeSpaceId-{i + 1}"
                    };
                }
            }

            Spaces = spaces;
        }
    }

    class FakeAppsResponse : AppsResponse
    {
        public FakeAppsResponse(string apiAddress, int pageNum, int totalResults, int totalPages, int resultsPerPage) : base()
        {
            bool isFirstPage = pageNum == 1;
            bool isLastPage = pageNum == totalPages;

            var firstHref = new Href() { href = $"{apiAddress}{CfApiClient.listAppsPath}?page=1&per_page={resultsPerPage}" };
            var lastHref = new Href() { href = $"{apiAddress}{CfApiClient.listAppsPath}?page={totalPages}&per_page={resultsPerPage}" };
            var nextHref = isLastPage ? null : new Href() { href = $"{apiAddress}{CfApiClient.listAppsPath}?page={pageNum + 1}&per_page={resultsPerPage}" };
            var previousHref = isFirstPage ? null : new Href() { href = $"{apiAddress}{CfApiClient.listAppsPath}?page={pageNum - 1}&per_page={resultsPerPage}" };

            Pagination = new Pagination
            {
                total_results = totalResults,
                total_pages = totalPages,
                first = firstHref,
                last = lastHref,
                next = nextHref,
                previous = previousHref
            };

            App[] apps;
            if (isLastPage)
            {
                int numResourcesInLastPage = totalResults % resultsPerPage;
                apps = new App[numResourcesInLastPage];

                for (int i = 0; i < numResourcesInLastPage; i++)
                {
                    apps[i] = new App
                    {
                        Name = $"fakeApp{i + 1}"
                    };
                }
            }
            else
            {
                apps = new App[resultsPerPage];

                for (int i = 0; i < resultsPerPage; i++)
                {
                    apps[i] = new App
                    {
                        Name = $"fakeApp{i + 1}"
                    };
                }
            }

            Apps = apps;
        }
    }

}
