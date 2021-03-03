using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.Collections.Generic;
using System.Security;
using Tanzu.Toolkit.VisualStudio.Models;
using Tanzu.Toolkit.VisualStudio.Services.CfCli;
using Tanzu.Toolkit.VisualStudio.Services.CfCli.Models.Orgs;
using Tanzu.Toolkit.VisualStudio.Services.CloudFoundry;
using Tanzu.Toolkit.VisualStudio.Services.CmdProcess;
using Tanzu.Toolkit.VisualStudio.Services.FileLocator;

namespace Tanzu.Toolkit.VisualStudio.Services.Tests
{
    public abstract class ServicesTestSupport
    {
        protected IServiceProvider services;
        protected Mock<ICfCliService> mockCfCliService;
        protected Mock<ICmdProcessService> mockCmdProcessService;
        protected Mock<IFileLocatorService> mockFileLocatorService;

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

        internal static readonly List<Org> mockOrgsResponse = new List<Org>
            {
                new Org
                {
                    entity = new Services.CfCli.Models.Orgs.Entity{ name = org1Name },
                    metadata = new Metadata{ guid = org1Guid }
                },
                new Org
                {
                   entity = new Services.CfCli.Models.Orgs.Entity{ name = org2Name },
                   metadata = new Metadata{ guid = org2Guid }
                },
                new Org
                {
                    entity = new Services.CfCli.Models.Orgs.Entity{ name = org3Name },
                    metadata = new Metadata{ guid = org3Guid }
                },
                new Org
                {
                    entity = new Services.CfCli.Models.Orgs.Entity{ name = org4Name },
                    metadata = new Metadata{ guid = org4Guid }
                }
            };

        protected ServicesTestSupport()
        {
            var services = new ServiceCollection();
            mockCfCliService = new Mock<ICfCliService>();
            mockCmdProcessService = new Mock<ICmdProcessService>();
            mockFileLocatorService = new Mock<IFileLocatorService>();

            services.AddSingleton(mockCfCliService.Object);
            services.AddSingleton(mockCmdProcessService.Object);
            services.AddSingleton(mockFileLocatorService.Object);

            this.services = services.BuildServiceProvider();
        }
    }
}
