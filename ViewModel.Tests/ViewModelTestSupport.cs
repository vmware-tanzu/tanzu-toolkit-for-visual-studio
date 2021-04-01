using Microsoft.Extensions.DependencyInjection;
using Moq;
using Serilog;
using System;
using System.Collections.Generic;
using Tanzu.Toolkit.VisualStudio.Models;
using Tanzu.Toolkit.VisualStudio.Services;
using Tanzu.Toolkit.VisualStudio.Services.CloudFoundry;
using Tanzu.Toolkit.VisualStudio.Services.CmdProcess;
using Tanzu.Toolkit.VisualStudio.Services.Dialog;
using Tanzu.Toolkit.VisualStudio.Services.Logging;
using Tanzu.Toolkit.VisualStudio.Services.ViewLocator;

namespace Tanzu.Toolkit.VisualStudio.ViewModels.Tests
{
    public abstract class ViewModelTestSupport
    {
        protected IServiceProvider services;

        protected Mock<ICloudFoundryService> mockCloudFoundryService;
        protected Mock<IDialogService> mockDialogService;
        protected Mock<IViewLocatorService> mockViewLocatorService;
        protected Mock<ILoggingService> mockLoggingService;

        protected Mock<ILogger> mockLogger;

        protected const string fakeCfName = "fake cf name";
        protected const string fakeCfApiAddress = "http://fake.api.address";
        protected const string fakeAccessToken = "fake.access.token";
        private const string fakeOrgName = "fake org name";
        private const string fakeOrgGuid = "fake-org-guid";
        private const string fakeSpaceName = "fake space name";
        private const string fakeSpaceGuid = "fake-space-guid";

        protected static readonly CloudFoundryInstance fakeCfInstance = new CloudFoundryInstance(fakeCfName, fakeCfApiAddress, fakeAccessToken);
        protected static readonly CloudFoundryOrganization fakeCfOrg = new CloudFoundryOrganization(fakeOrgName, fakeOrgGuid, fakeCfInstance, "fake spaces url");
        protected static readonly CloudFoundrySpace fakeCfSpace = new CloudFoundrySpace(fakeSpaceName, fakeSpaceGuid, fakeCfOrg, "fake apps url");

        protected readonly List<CloudFoundryOrganization> emptyListOfOrgs = new List<CloudFoundryOrganization>();
        protected readonly List<CloudFoundrySpace> emptyListOfSpaces = new List<CloudFoundrySpace>();
        protected readonly List<CloudFoundryApp> emptyListOfApps = new List<CloudFoundryApp>();

        internal static readonly CmdResult fakeSuccessCmdResult = new CmdResult("junk output", "junk error", 0);
        internal static readonly CmdResult fakeFailureCmdResult = new CmdResult("junk output", "junk error", 1);

        internal static readonly DetailedResult fakeSuccessDetailedResult = new DetailedResult(true, null, fakeSuccessCmdResult);
        internal static readonly DetailedResult fakeFailureDetailedResult = new DetailedResult(false, "junk error", fakeFailureCmdResult);

        protected ViewModelTestSupport()
        {
            var services = new ServiceCollection();

            mockCloudFoundryService = new Mock<ICloudFoundryService>();
            mockDialogService = new Mock<IDialogService>();
            mockViewLocatorService = new Mock<IViewLocatorService>();
            mockLoggingService = new Mock<ILoggingService>();

            mockLogger = new Mock<ILogger>();
            mockLoggingService.SetupGet(m => m.Logger).Returns(mockLogger.Object);

            services.AddSingleton(mockCloudFoundryService.Object);
            services.AddSingleton(mockDialogService.Object);
            services.AddSingleton(mockViewLocatorService.Object);
            services.AddSingleton(mockLoggingService.Object);

            this.services = services.BuildServiceProvider();
        }
    }
}
