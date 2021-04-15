using Microsoft.Extensions.DependencyInjection;
using Moq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Security;
using Tanzu.Toolkit.VisualStudio.Models;
using Tanzu.Toolkit.VisualStudio.Services.CfCli;
using Tanzu.Toolkit.VisualStudio.Services.CfCli.Models.Apps;
using Tanzu.Toolkit.VisualStudio.Services.CfCli.Models.Orgs;
using Tanzu.Toolkit.VisualStudio.Services.CfCli.Models.Spaces;
using Tanzu.Toolkit.VisualStudio.Services.CloudFoundry;
using Tanzu.Toolkit.VisualStudio.Services.CmdProcess;
using Tanzu.Toolkit.VisualStudio.Services.FileLocator;
using Tanzu.Toolkit.VisualStudio.Services.Logging;

namespace Tanzu.Toolkit.VisualStudio.Services.Tests
{
    public abstract class ServicesTestSupport
    {
        protected IServiceProvider services;
        protected Mock<ICfCliService> mockCfCliService;
        protected Mock<ICmdProcessService> mockCmdProcessService;
        protected Mock<IFileLocatorService> mockFileLocatorService;
        protected Mock<ILoggingService> mockLoggingService;
        protected Mock<ILogger> mockLogger;

        internal static ICloudFoundryService cfService;
        internal static CloudFoundryInstance fakeCfInstance;
        internal static CloudFoundryOrganization fakeOrg;
        internal static CloudFoundrySpace fakeSpace;
        internal static CloudFoundryApp fakeApp;

        internal static readonly string fakeValidTarget = "https://my.fake.target";
        internal static readonly string fakeValidUsername = "junk";
        internal static readonly SecureString fakeValidPassword = new SecureString();
        internal static readonly string fakeHttpProxy = "junk";
        internal static readonly bool skipSsl = true;
        internal static readonly string fakeValidAccessToken = "valid token";
        internal static readonly string fakeProjectPath = "this\\is\\a\\fake\\path";

        internal static readonly CmdResult fakeSuccessCmdResult = new CmdResult("junk output", "junk error", 0);
        internal static readonly CmdResult fakeFailureCmdResult = new CmdResult("junk output", "junk error", 1);
        internal static readonly DetailedResult fakeSuccessDetailedResult = new DetailedResult(true, null, fakeSuccessCmdResult);
        internal static readonly DetailedResult fakeFailureDetailedResult = new DetailedResult(false, "junk", fakeSuccessCmdResult);

        internal static readonly string org1Name = "org1";
        internal static readonly string org2Name = "org2";
        internal static readonly string org3Name = "org3";
        internal static readonly string org4Name = "org4";
        internal static readonly string org1Guid = "org-1-id";
        internal static readonly string org2Guid = "org-2-id";
        internal static readonly string org3Guid = "org-3-id";
        internal static readonly string org4Guid = "org-4-id";
        internal static readonly string org1SpacesUrl = "fake spaces url 1";
        internal static readonly string org2SpacesUrl = "fake spaces url 2";
        internal static readonly string org3SpacesUrl = "fake spaces url 3";
        internal static readonly string org4SpacesUrl = "fake spaces url 4";

        internal static readonly string space1Name = "space1";
        internal static readonly string space2Name = "space2";
        internal static readonly string space3Name = "space3";
        internal static readonly string space4Name = "space4";
        internal static readonly string space1Guid = "space-1-id";
        internal static readonly string space2Guid = "space-2-id";
        internal static readonly string space3Guid = "space-3-id";
        internal static readonly string space4Guid = "space-4-id";
        internal static readonly string space1AppsUrl = "fake apps url 1";
        internal static readonly string space2AppsUrl = "fake apps url 2";
        internal static readonly string space3AppsUrl = "fake apps url 3";
        internal static readonly string space4AppsUrl = "fake apps url 4";

        internal static readonly string app1Name = "app1";
        internal static readonly string app2Name = "app2";
        internal static readonly string app3Name = "app3";
        internal static readonly string app4Name = "app4";
        internal static readonly string app1Guid = "app-1-id";
        internal static readonly string app2Guid = "app-2-id";
        internal static readonly string app3Guid = "app-3-id";
        internal static readonly string app4Guid = "app-4-id";

        internal static readonly List<Org> mockOrgsResponse = new List<Org>
        {
            new Org
            {
                entity = new Services.CfCli.Models.Orgs.Entity{ name = org1Name, spaces_url = org1SpacesUrl },
                metadata = new Services.CfCli.Models.Orgs.Metadata{ guid = org1Guid }
            },
            new Org
            {
                entity = new Services.CfCli.Models.Orgs.Entity{ name = org2Name, spaces_url = org2SpacesUrl },
                metadata = new Services.CfCli.Models.Orgs.Metadata{ guid = org2Guid }
            },
            new Org
            {
                entity = new Services.CfCli.Models.Orgs.Entity{ name = org3Name, spaces_url = org3SpacesUrl },
                metadata = new Services.CfCli.Models.Orgs.Metadata{ guid = org3Guid }
            },
            new Org
            {
                entity = new Services.CfCli.Models.Orgs.Entity{ name = org4Name, spaces_url = org4SpacesUrl },
                metadata = new Services.CfCli.Models.Orgs.Metadata{ guid = org4Guid }
            }
            };

        internal static readonly List<Space> mockSpacesResponse = new List<Space>
        {
            new Space
            {
                entity = new Services.CfCli.Models.Spaces.Entity{ name = space1Name, apps_url = space1AppsUrl },
                metadata = new Services.CfCli.Models.Spaces.Metadata{ guid = space1Guid }
            },
            new Space
            {
                entity = new Services.CfCli.Models.Spaces.Entity{ name = space2Name, apps_url = space2AppsUrl },
                metadata = new Services.CfCli.Models.Spaces.Metadata{ guid = space2Guid }
            },
            new Space
            {
                entity = new Services.CfCli.Models.Spaces.Entity{ name = space3Name, apps_url = space3AppsUrl },
                metadata = new Services.CfCli.Models.Spaces.Metadata{ guid = space3Guid }
            },
            new Space
            {
                entity = new Services.CfCli.Models.Spaces.Entity{ name = space4Name, apps_url = space4AppsUrl },
                metadata = new Services.CfCli.Models.Spaces.Metadata{ guid = space4Guid }
            }
        };

        internal static readonly List<App> mockAppsResponse = new List<App>
            {
                new App
                {
                    name = app1Name,
                    guid = app1Guid
                },
                new App
                {
                    name = app2Name,
                    guid = app2Guid
                },
                new App
                {
                    name = app3Name,
                    guid = app3Guid
                },
                new App
                {
                    name = app4Name,
                    guid = app4Guid
                }
            };

        protected ServicesTestSupport()
        {
            var services = new ServiceCollection();
            mockCfCliService = new Mock<ICfCliService>();
            mockCmdProcessService = new Mock<ICmdProcessService>();
            mockFileLocatorService = new Mock<IFileLocatorService>();
            mockLoggingService = new Mock<ILoggingService>();

            mockLogger = new Mock<ILogger>();
            mockLoggingService.SetupGet(m => m.Logger).Returns(mockLogger.Object);

            services.AddSingleton(mockCfCliService.Object);
            services.AddSingleton(mockCmdProcessService.Object);
            services.AddSingleton(mockFileLocatorService.Object);
            services.AddSingleton(mockLoggingService.Object);

            this.services = services.BuildServiceProvider();
        }
    }
}
