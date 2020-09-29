using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using TanzuForVS.Services.CloudFoundry;
using TanzuForVS.Services.Dialog;
using TanzuForVS.Services.Locator;

namespace TanzuForVS.WpfViews.Tests
{
    public abstract class ViewTestSupport
    {
        protected IServiceProvider services;

        protected Mock<ICloudFoundryService> mockCloudFoundryService;
        protected Mock<IDialogService> mockDialogService;
        protected Mock<IViewLocatorService> mockViewLocatorService;

        protected ViewTestSupport()
        {
            var services = new ServiceCollection();
            mockCloudFoundryService = new Mock<ICloudFoundryService>();
            mockDialogService = new Mock<IDialogService>();
            mockViewLocatorService = new Mock<IViewLocatorService>();

            services.AddSingleton<ICloudFoundryService>(mockCloudFoundryService.Object);
            services.AddSingleton<IDialogService>(mockDialogService.Object);
            services.AddSingleton<IViewLocatorService>(mockViewLocatorService.Object);
            this.services = services.BuildServiceProvider();
        }
    }
}
