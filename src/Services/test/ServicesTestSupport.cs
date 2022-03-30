using System;
using System.Collections.Generic;
using System.Security;
using Tanzu.Toolkit.Models;
using Tanzu.Toolkit.Services.CommandProcess;

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

        protected const string _onlySupportedAppLifecycleType = "buildpack";

        protected const string _stack1Name = "stack1";
        protected const string _stack2Name = "stack2";
        protected const string _stack3Name = "stack3";
        protected const string _stack4Name = "stack4";
        protected const string _stack1Guid = "stack-1-id";
        protected const string _stack2Guid = "stack-2-id";
        protected const string _stack3Guid = "stack-3-id";
        protected const string _stack4Guid = "stack-4-id";

        protected const string _buildpack1Name = "buildpack1";
        protected const string _buildpack2Name = "buildpack2";
        protected const string _buildpack3Name = "buildpack3";
        protected const string _buildpack4Name = "buildpack4";

        protected static readonly string _fakeValidTarget = "https://my.fake.target";
        protected static readonly string _fakeValidUsername = "junk";
        protected static readonly SecureString _fakeValidPassword = new SecureString();
        protected static readonly string _fakeHttpProxy = "junk";
        protected static readonly bool _skipSsl = true;
        protected static readonly string _fakeValidAccessToken = "valid token";
        protected static readonly string _fakeProjectPath = "this\\is\\a\\fake\\path";
        protected static readonly string _fakeManifestPath = "this\\is\\a\\fake\\path"; 
        protected static readonly Action<string> _fakeOutCallback = content => { };
        protected static readonly Action<string> _fakeErrCallback = content => { };

        protected static readonly CloudFoundryInstance FakeCfInstance = new CloudFoundryInstance("fake cf", _fakeValidTarget, false);
        protected static readonly CloudFoundryOrganization FakeOrg = new CloudFoundryOrganization("fake org", "fake org guid", FakeCfInstance);
        protected static readonly CloudFoundrySpace FakeSpace = new CloudFoundrySpace("fake space", "fake space guid", FakeOrg);
        protected static readonly CloudFoundryApp FakeApp = new CloudFoundryApp("fake app", "fake app guid", FakeSpace, null);

        protected static readonly CommandResult _fakeSuccessCmdResult = new CommandResult("junk output", "junk error", 0);
        protected static readonly CommandResult _fakeFailureCmdResult = new CommandResult("junk output", "junk error", 1);
        protected static readonly DetailedResult _fakeSuccessDetailedResult = new DetailedResult(true, null, _fakeSuccessCmdResult);
        protected static readonly DetailedResult _fakeFailureDetailedResult = new DetailedResult(false, "junk", _fakeSuccessCmdResult);

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

        internal readonly AppManifest exampleManifest = new AppManifest
        {
            Version = 1,
            Applications = new List<AppConfig>
                {
                    new AppConfig
                    {
                        Name = "app1",
                        Buildpacks = new List<string>
                        {
                            "ruby_buildpack",
                            "java_buildpack",
                        },
                        Env = new Dictionary<string, string>
                        {
                            {"my_snake_case_var_key", "my_snake_case_var_val" },
                            {"my-kebab-case-var-key", "my-kebab-case-var-val" },
                            {"myCamelCaseVarKey", "myCamelCaseVarVal" },
                            {"MyPascalCaseVarKey", "MyPascalCaseVarVal" },
                            {"MY_UPPER_CASE_VAR_KEY", "MY_UPPER_CASE_VAR_VAL" },
                        },
                        Routes = new List<RouteConfig>
                        {
                            new RouteConfig
                            {
                                Route = "route.example.com",
                            },
                            new RouteConfig
                            {
                                Route = "another-route.example.com",
                                Protocol = "http2"
                            },
                        },
                        Services = new List<string> {
                            "my-service1",
                            "my-service2",
                        },
                        Stack = "cflinuxfs3",
                        Metadata = new MetadataConfig
                        {
                            Annotations = new Dictionary<string, string>
                            {
                                { "contact", "bob@example.com jane@example.com" },
                            },
                            Labels = new Dictionary<string, string>
                            {
                                { "sensitive", "true" },
                            },
                        },
                        Processes = new List<ProcessConfig>
                        {
                            new ProcessConfig
                            {
                                Type = "web",
                                Command = "start-web.sh",
                                DiskQuota = "512M",
                                HealthCheckHttpEndpoint = "/healthcheck",
                                HealthCheckType = "http",
                                HealthCheckInvocationTimeout = 10,
                                Instances = 3,
                                Memory = "500M",
                                Timeout = 10,
                            },
                            new ProcessConfig
                            {
                                Type = "worker",
                                Command = "start-worker.sh",
                                DiskQuota = "1G",
                                HealthCheckType = "process",
                                Instances = 2,
                                Memory = "256M",
                                Timeout = 15,
                            },
                        }
                    },
                    new AppConfig
                    {
                        Name = "app2",
                        Env = new Dictionary<string, string>
                        {
                            { "VAR1", "value1" },
                        },
                        Processes = new List<ProcessConfig>
                        {
                            new ProcessConfig
                            {
                                Type = "web",
                                Instances = 1,
                                Memory = "256M",
                            },
                        },
                        Sidecars = new List<SidecarConfig>
                        {
                            new SidecarConfig
                            {
                                Name = "authenticator",
                                ProcessTypes = new List<string>
                                {
                                    "web",
                                    "worker",
                                },
                                Command = "bundle exec run-authenticator",
                                Memory = "800M",
                            },
                            new SidecarConfig
                            {
                                Name = "upcaser",
                                ProcessTypes = new List<string>
                                {
                                    "worker",
                                },
                                Command = "./tr-server",
                                Memory = "2G",
                            }
                        }
                    }
                }
        };

        internal readonly string exampleManifestYaml = System.IO.File.ReadAllText("TestFakes/SampleManifest.yml");
    }
}
