using System;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Tanzu.Toolkit.Services;
using Tanzu.Toolkit.Services.CloudFoundry;
using Tanzu.Toolkit.Services.Dialog;
using Tanzu.Toolkit.Services.Logging;
using Tanzu.Toolkit.Services.Threading;
using Tanzu.Toolkit.Services.ViewLocator;

namespace Tanzu.Toolkit.WpfViews.Tests
{
    public abstract class ViewTestSupport
    {
        protected IServiceProvider services;

        protected Mock<ICloudFoundryService> mockCloudFoundryService;
        protected Mock<IDialogService> mockDialogService;
        protected Mock<IViewLocatorService> mockViewLocatorService;
        protected Mock<ILoggingService> mockLoggingService;
        protected Mock<IUiDispatcherService> mockUiDispatcherService;
        protected Mock<IThreadingService> mockThreadingService;

        protected ViewTestSupport()
        {
            var services = new ServiceCollection();
            mockCloudFoundryService = new Mock<ICloudFoundryService>();
            mockDialogService = new Mock<IDialogService>();
            mockViewLocatorService = new Mock<IViewLocatorService>();
            mockLoggingService = new Mock<ILoggingService>();
            mockUiDispatcherService = new Mock<IUiDispatcherService>();
            mockThreadingService = new Mock<IThreadingService>();

            services.AddSingleton(mockCloudFoundryService.Object);
            services.AddSingleton(mockDialogService.Object);
            services.AddSingleton(mockViewLocatorService.Object);
            services.AddSingleton(mockLoggingService.Object);
            services.AddSingleton(mockUiDispatcherService.Object);
            services.AddSingleton(mockThreadingService.Object);
            this.services = services.BuildServiceProvider();
        }
    }
}
