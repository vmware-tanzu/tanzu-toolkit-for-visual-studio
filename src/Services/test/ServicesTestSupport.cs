using System;
using System.Collections.Generic;
using System.Security;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Serilog;
using Tanzu.Toolkit.CloudFoundryApiClient;
using Tanzu.Toolkit.Models;
using Tanzu.Toolkit.Services.CfCli;
using Tanzu.Toolkit.Services.CfCli.Models.Apps;
using Tanzu.Toolkit.Services.CfCli.Models.Orgs;
using Tanzu.Toolkit.Services.CfCli.Models.Spaces;
using Tanzu.Toolkit.Services.CloudFoundry;
using Tanzu.Toolkit.Services.CmdProcess;
using Tanzu.Toolkit.Services.Dialog;
using Tanzu.Toolkit.Services.FileLocator;
using Tanzu.Toolkit.Services.Logging;

namespace Tanzu.Toolkit.Services.Tests
{
    public abstract class ServicesTestSupport
    {
        protected const string _org1Name = "org1";
        protected const string _org2Name = "org2";
        protected const string _org3Name = "org3";
        protected const string _org4Name = "org4";
        protected const string _org1Guid = "org-1-id";
        protected const string _org2Guid = "org-2-id";
        protected const string _org3Guid = "org-3-id";
        protected const string _org4Guid = "org-4-id";
        protected const string _org1SpacesUrl = "fake spaces url 1";
        protected const string _org2SpacesUrl = "fake spaces url 2";
        protected const string _org3SpacesUrl = "fake spaces url 3";
        protected const string _org4SpacesUrl = "fake spaces url 4";

        protected const string _space1Name = "space1";
        protected const string _space2Name = "space2";
        protected const string _space3Name = "space3";
        protected const string _space4Name = "space4";
        protected const string _space1Guid = "space-1-id";
        protected const string _space2Guid = "space-2-id";
        protected const string _space3Guid = "space-3-id";
        protected const string _space4Guid = "space-4-id";
        protected const string _space1AppsUrl = "fake apps url 1";
        protected const string _space2AppsUrl = "fake apps url 2";
        protected const string _space3AppsUrl = "fake apps url 3";
        protected const string _space4AppsUrl = "fake apps url 4";

        protected const string _app1Name = "app1";
        protected const string _app2Name = "app2";
        protected const string _app3Name = "app3";
        protected const string _app4Name = "app4";
        protected const string _app1Guid = "app-1-id";
        protected const string _app2Guid = "app-2-id";
        protected const string _app3Guid = "app-3-id";
        protected const string _app4Guid = "app-4-id";
        protected const string _app1State = "STARTED";
        protected const string _app2State = "STOPPED";
        protected const string _app3State = "STARTED";
        protected const string _app4State = "STOPPED";

        protected static readonly CloudFoundryInstance FakeCfInstance = new CloudFoundryInstance("fake cf", _fakeValidTarget, _fakeValidAccessToken);
        protected static readonly CloudFoundryOrganization FakeOrg = new CloudFoundryOrganization("fake org", "fake org guid", FakeCfInstance);
        protected static readonly CloudFoundrySpace FakeSpace = new CloudFoundrySpace("fake space", "fake space guid", FakeOrg);
        protected static readonly CloudFoundryApp FakeApp = new CloudFoundryApp("fake app", "fake app guid", FakeSpace, null);

        protected static readonly string _fakeValidTarget = "https://my.fake.target";
        protected static readonly string _fakeValidUsername = "junk";
        protected static readonly SecureString _fakeValidPassword = new SecureString();
        protected static readonly string _fakeHttpProxy = "junk";
        protected static readonly bool _skipSsl = true;
        protected static readonly string _fakeValidAccessToken = "valid token";
        protected static readonly string _fakeProjectPath = "this\\is\\a\\fake\\path";

        protected static readonly CmdResult _fakeSuccessCmdResult = new CmdResult("junk output", "junk error", 0);
        protected static readonly CmdResult _fakeFailureCmdResult = new CmdResult("junk output", "junk error", 1);
        protected static readonly DetailedResult _fakeSuccessDetailedResult = new DetailedResult(true, null, _fakeSuccessCmdResult);
        protected static readonly DetailedResult _fakeFailureDetailedResult = new DetailedResult(false, "junk", _fakeSuccessCmdResult);

        protected static readonly List<Org> _mockOrgsResponse = new List<Org>
        {
            new Org
            {
                Entity = new Services.CfCli.Models.Orgs.Entity { Name = _org1Name, SpacesUrl = _org1SpacesUrl },
                Metadata = new Services.CfCli.Models.Orgs.Metadata { Guid = _org1Guid },
            },
            new Org
            {
                Entity = new Services.CfCli.Models.Orgs.Entity { Name = _org2Name, SpacesUrl = _org2SpacesUrl },
                Metadata = new Services.CfCli.Models.Orgs.Metadata { Guid = _org2Guid },
            },
            new Org
            {
                Entity = new Services.CfCli.Models.Orgs.Entity { Name = _org3Name, SpacesUrl = _org3SpacesUrl },
                Metadata = new Services.CfCli.Models.Orgs.Metadata { Guid = _org3Guid },
            },
            new Org
            {
                Entity = new Services.CfCli.Models.Orgs.Entity { Name = _org4Name, SpacesUrl = _org4SpacesUrl },
                Metadata = new Services.CfCli.Models.Orgs.Metadata { Guid = _org4Guid },
            },
        };

        protected static readonly List<Space> _mockSpacesResponse = new List<Space>
        {
            new Space
            {
                Entity = new Services.CfCli.Models.Spaces.Entity { Name = _space1Name, AppsUrl = _space1AppsUrl },
                Metadata = new Services.CfCli.Models.Spaces.Metadata { Guid = _space1Guid },
            },
            new Space
            {
                Entity = new Services.CfCli.Models.Spaces.Entity { Name = _space2Name, AppsUrl = _space2AppsUrl },
                Metadata = new Services.CfCli.Models.Spaces.Metadata { Guid = _space2Guid },
            },
            new Space
            {
                Entity = new Services.CfCli.Models.Spaces.Entity { Name = _space3Name, AppsUrl = _space3AppsUrl },
                Metadata = new Services.CfCli.Models.Spaces.Metadata { Guid = _space3Guid },
            },
            new Space
            {
                Entity = new Services.CfCli.Models.Spaces.Entity { Name = _space4Name, AppsUrl = _space4AppsUrl },
                Metadata = new Services.CfCli.Models.Spaces.Metadata { Guid = _space4Guid },
            },
        };

        protected static readonly List<App> _mockAppsResponse = new List<App>
            {
                new App
                {
                    Name = _app1Name,
                    Guid = _app1Guid,
                },
                new App
                {
                    Name = _app2Name,
                    Guid = _app2Guid,
                },
                new App
                {
                    Name = _app3Name,
                    Guid = _app3Guid,
                },
                new App
                {
                    Name = _app4Name,
                    Guid = _app4Guid,
                },
            };

        /** this fake JWT was created using these values:
         * HEADER:
         * {
         * "typ": "JWT",
         * "alg": "HS256"
         * }
         * 
         * PAYLOAD:
         * {
         * "iss": "junk",
         * "iat": 1623163938,
         * "exp": 253370818338, // year 9999
         * "aud": "www.example.com",
         * "sub": "jrocket@example.com",
         * "GivenName": "Johnny",
         * "Surname": "Rocket",
         * "Email": "jrocket@example.com",
         * "Role": [
         * "Manager",
         * "Project Administrator"
         * ]
         * }
         * 
         * SIGNING KEY:
         * "qwertyuiopasdfghjklzxcvbnm123456"
         */
        internal static readonly string _fakeAccessToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJpc3MiOiJqdW5rIiwiaWF0IjoxNjIzMTYzOTM4LCJleHAiOjI1MzM3MDgxODMzOCwiYXVkIjoid3d3LmV4YW1wbGUuY29tIiwic3ViIjoianJvY2tldEBleGFtcGxlLmNvbSIsIkdpdmVuTmFtZSI6IkpvaG5ueSIsIlN1cm5hbWUiOiJSb2NrZXQiLCJFbWFpbCI6Impyb2NrZXRAZXhhbXBsZS5jb20iLCJSb2xlIjpbIk1hbmFnZXIiLCJQcm9qZWN0IEFkbWluaXN0cmF0b3IiXX0.9ulEPk_mjvBivguELvlojZAUnrwkqUMnunFF6zlmKqc";

        /**
         * This is a real JWT from the Jamestown CF environment which expired on June 8th 2021
         */
        internal static readonly string expiredAccessToken = "eyJhbGciOiJSUzI1NiIsImprdSI6Imh0dHBzOi8vdWFhLnN5cy5qYW1lc3Rvd24uY2YtYXBwLmNvbS90b2tlbl9rZXlzIiwia2lkIjoia2V5LTEiLCJ0eXAiOiJKV1QifQ.eyJqdGkiOiIyNmY3ZGFhNTI5MDM0OGVmOWQyZTRkYTg4MmZiMWMyZiIsInN1YiI6IjY0NzljMjY0LWE3YTQtNGRiYS05MGFmLTk1MGE4YTMyODE5ZSIsInNjb3BlIjpbIm9wZW5pZCIsInJvdXRpbmcucm91dGVyX2dyb3Vwcy53cml0ZSIsIm5ldHdvcmsud3JpdGUiLCJzY2ltLnJlYWQiLCJjbG91ZF9jb250cm9sbGVyLmFkbWluIiwidWFhLnVzZXIiLCJyb3V0aW5nLnJvdXRlcl9ncm91cHMucmVhZCIsImNsb3VkX2NvbnRyb2xsZXIucmVhZCIsInBhc3N3b3JkLndyaXRlIiwiY2xvdWRfY29udHJvbGxlci53cml0ZSIsIm5ldHdvcmsuYWRtaW4iLCJkb3BwbGVyLmZpcmVob3NlIiwic2NpbS53cml0ZSJdLCJjbGllbnRfaWQiOiJjZiIsImNpZCI6ImNmIiwiYXpwIjoiY2YiLCJncmFudF90eXBlIjoicGFzc3dvcmQiLCJ1c2VyX2lkIjoiNjQ3OWMyNjQtYTdhNC00ZGJhLTkwYWYtOTUwYThhMzI4MTllIiwib3JpZ2luIjoidWFhIiwidXNlcl9uYW1lIjoiYWRtaW4iLCJlbWFpbCI6ImFkbWluIiwiYXV0aF90aW1lIjoxNjIzMTY0ODE2LCJyZXZfc2lnIjoiNDFkOTJiYiIsImlhdCI6MTYyMzE4MTEzNSwiZXhwIjoxNjIzMTg4MzM1LCJpc3MiOiJodHRwczovL3VhYS5zeXMuamFtZXN0b3duLmNmLWFwcC5jb20vb2F1dGgvdG9rZW4iLCJ6aWQiOiJ1YWEiLCJhdWQiOlsiY2xvdWRfY29udHJvbGxlciIsInNjaW0iLCJwYXNzd29yZCIsImNmIiwidWFhIiwib3BlbmlkIiwiZG9wcGxlciIsIm5ldHdvcmsiLCJyb3V0aW5nLnJvdXRlcl9ncm91cHMiXX0.qCfIxuJb2Xv21pq9idUO44PY50n4FY1cTwpmoWbjAmVs2Cu1smeD2L8gJFSZtg04MlKEJLspfSwfsAfu4YTbUB_iWyBmrZybnZFNrU335z8jReAnHTD5Nq5wVvPLNKdwVy3VyyhHTpD7BQ-oTPLDFaVTysoqR8C13ln0Sbr8jctOHVGRS8sOxJVedtRrLAhQUtZJUPpxbq4msFa0YWLQfXRwWTUc4boYOqtHx1jXg5T2qOJDcUF8MvvLE5ROnfsRciMEtCjCqJsteIEG2lfHcE7JwH3XXSJoiz1pIBoDw1DEUplHmNgQ1saK7tNQu-gg4RVWHFKvqhnGwT94buwHkA";
    }
}
