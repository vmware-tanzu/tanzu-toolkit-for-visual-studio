using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using Tanzu.Toolkit.VisualStudio.Services.CfCli;
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
