using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
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
