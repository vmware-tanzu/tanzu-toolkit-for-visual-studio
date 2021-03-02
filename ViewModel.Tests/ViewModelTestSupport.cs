using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.Collections.Generic;
using Tanzu.Toolkit.VisualStudio.Models;
using Tanzu.Toolkit.VisualStudio.Services.CloudFoundry;
using Tanzu.Toolkit.VisualStudio.Services.Dialog;
using Tanzu.Toolkit.VisualStudio.Services.ViewLocator;

namespace Tanzu.Toolkit.VisualStudio.ViewModels.Tests
{
    public abstract class ViewModelTestSupport
    {
        protected IServiceProvider services;

        protected Mock<ICloudFoundryService> mockCloudFoundryService;
        protected Mock<IDialogService> mockDialogService;
        protected Mock<IViewLocatorService> mockViewLocatorService;

        protected const string fakeCfName = "fake cf name";
        protected const string fakeCfApiAddress = "http://fake.api.address";
        protected const string fakeAccessToken = "fake.access.token";
        private const string fakeOrgName = "fake org name";
        private const string fakeOrgGuid = "fake-org-guid";
        private const string fakeSpaceName = "fake space name";
        private const string fakeSpaceGuid = "fake-space-guid";

        protected static readonly CloudFoundryInstance fakeCfInstance = new CloudFoundryInstance(fakeCfName, fakeCfApiAddress, fakeAccessToken);
        protected static readonly CloudFoundryOrganization fakeCfOrg = new CloudFoundryOrganization(fakeOrgName, fakeOrgGuid, fakeCfInstance);
        protected static readonly CloudFoundrySpace fakeCfSpace = new CloudFoundrySpace(fakeSpaceName, fakeSpaceGuid, fakeCfOrg);

        protected readonly List<CloudFoundryOrganization> emptyListOfOrgs = new List<CloudFoundryOrganization>();
        protected readonly List<CloudFoundrySpace> emptyListOfSpaces = new List<CloudFoundrySpace>();
        protected readonly List<CloudFoundryApp> emptyListOfApps = new List<CloudFoundryApp>();

        protected ViewModelTestSupport()
        {
            var services = new ServiceCollection();

            mockCloudFoundryService = new Mock<ICloudFoundryService>();
            mockDialogService = new Mock<IDialogService>();
            mockViewLocatorService = new Mock<IViewLocatorService>();

            services.AddSingleton(mockCloudFoundryService.Object);
            services.AddSingleton(mockDialogService.Object);
            services.AddSingleton(mockViewLocatorService.Object);

            this.services = services.BuildServiceProvider();
        }
    }
}
