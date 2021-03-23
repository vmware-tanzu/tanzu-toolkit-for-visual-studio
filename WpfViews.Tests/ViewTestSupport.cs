using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using Tanzu.Toolkit.VisualStudio.Services.CloudFoundry;
using Tanzu.Toolkit.VisualStudio.Services.Dialog;
using Tanzu.Toolkit.VisualStudio.Services.Logging;
using Tanzu.Toolkit.VisualStudio.Services.ViewLocator;

namespace Tanzu.Toolkit.VisualStudio.WpfViews.Tests
{
    public abstract class ViewTestSupport
    {
        protected IServiceProvider services;

        protected Mock<ICloudFoundryService> mockCloudFoundryService;
        protected Mock<IDialogService> mockDialogService;
        protected Mock<IViewLocatorService> mockViewLocatorService;
        protected Mock<ILoggingService> mockLoggingService;

        protected ViewTestSupport()
        {
            var services = new ServiceCollection();
            mockCloudFoundryService = new Mock<ICloudFoundryService>();
            mockDialogService = new Mock<IDialogService>();
            mockViewLocatorService = new Mock<IViewLocatorService>();
            mockLoggingService = new Mock<ILoggingService>();

            services.AddSingleton(mockCloudFoundryService.Object);
            services.AddSingleton(mockDialogService.Object);
            services.AddSingleton(mockViewLocatorService.Object);
            services.AddSingleton(mockLoggingService.Object);
            this.services = services.BuildServiceProvider();
        }
    }
}
