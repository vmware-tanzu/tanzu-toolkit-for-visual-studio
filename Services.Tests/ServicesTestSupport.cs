using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using TanzuForVS.CloudFoundryApiClient;

namespace TanzuForVS.Services
{
    public abstract class ServicesTestSupport
    {
        protected IServiceProvider services;

        //protected Mock<ICloudFoundryService> mockCloudFoundryService;
        //protected Mock<IDialogService> mockDialogService;
        //protected Mock<IViewLocatorService> mockViewLocatorService;
        protected Mock<ICfApiClient> mockCfApiClient;

        protected ServicesTestSupport()
        {
            var services = new ServiceCollection();
            mockCfApiClient = new Mock<ICfApiClient>();
            //mockCloudFoundryService = new Mock<ICloudFoundryService>();
            //mockDialogService = new Mock<IDialogService>();
            //mockViewLocatorService = new Mock<IViewLocatorService>();

            services.AddSingleton<ICfApiClient>(mockCfApiClient.Object);

            //services.AddSingleton<ICloudFoundryService>(mockCloudFoundryService.Object);
            //services.AddSingleton<IDialogService>(mockDialogService.Object);
            //services.AddSingleton<IViewLocatorService>(mockViewLocatorService.Object);
            this.services = services.BuildServiceProvider();
        }
    }
}
