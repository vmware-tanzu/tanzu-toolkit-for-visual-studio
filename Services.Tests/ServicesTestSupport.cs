using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using TanzuForVS.CloudFoundryApiClient;
using TanzuForVS.Services.CfCli;
using TanzuForVS.Services.CmdProcess;
using TanzuForVS.Services.FileLocator;

namespace TanzuForVS.Services
{
    public abstract class ServicesTestSupport
    {
        protected IServiceProvider services;
        protected Mock<ICfApiClient> mockCfApiClient;
        protected Mock<ICfCliService> mockCfCliService;
        protected Mock<ICmdProcessService> mockCmdProcessService;
        protected Mock<IFileLocatorService> mockFileLocatorService;

        protected ServicesTestSupport()
        {
            var services = new ServiceCollection();
            mockCfApiClient = new Mock<ICfApiClient>();
            mockCfCliService = new Mock<ICfCliService>();
            mockCmdProcessService = new Mock<ICmdProcessService>();
            mockFileLocatorService = new Mock<IFileLocatorService>();

            services.AddSingleton(mockCfApiClient.Object);
            services.AddSingleton(mockCfCliService.Object);
            services.AddSingleton(mockCmdProcessService.Object);
            services.AddSingleton(mockFileLocatorService.Object);

            this.services = services.BuildServiceProvider();
        }
    }
}
