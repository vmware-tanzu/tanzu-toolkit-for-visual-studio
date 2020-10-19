using TanzuForVS.CloudFoundryApiClient.Models;
using TanzuForVS.CloudFoundryApiClient.Models.BasicInfoResponse;
using TanzuForVS.CloudFoundryApiClient.Models.OrgsResponse;

namespace TanzuForVS.CloudFoundryApiClient.UnitTests
{
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

            pagination = new Pagination
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
                    orgs[i] = new Org($"fakeOrg{i + 1}");
                }
            }
            else
            {
                orgs = new Org[resultsPerPage];

                for (int i = 0; i < resultsPerPage; i++)
                {
                    orgs[i] = new Org($"fakeOrg{i + 1}");
                }
            }

            Orgs = orgs;
        }
    }
}
