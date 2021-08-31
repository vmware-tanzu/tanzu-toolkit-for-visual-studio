﻿using System;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Tanzu.Toolkit.Services;
using Tanzu.Toolkit.Services.CloudFoundry;
using Tanzu.Toolkit.Services.Dialog;
using Tanzu.Toolkit.Services.ErrorDialog;
using Tanzu.Toolkit.Services.Logging;
using Tanzu.Toolkit.Services.Threading;
using Tanzu.Toolkit.Services.ViewLocator;
using Tanzu.Toolkit.ViewModels;
using Tanzu.Toolkit.WpfViews.Services;
using Tanzu.Toolkit.WpfViews.ThemeService;

namespace Tanzu.Toolkit.WpfViews.Tests
{
    public abstract class ViewTestSupport
    {
        protected IServiceProvider services;

        // Services
        protected Mock<ICloudFoundryService> mockCloudFoundryService;
        protected Mock<IErrorDialog> mockErrorDialogService;
        protected Mock<IDialogService> mockDialogService;
        protected Mock<IViewLocatorService> mockViewLocatorService;
        protected Mock<ILoggingService> mockLoggingService;
        protected Mock<IUiDispatcherService> mockUiDispatcherService;
        protected Mock<IThreadingService> mockThreadingService;
        protected Mock<IThemeService> mockThemeService;
        protected Mock<IViewService> mockViewService;

        // ViewModels
        protected Mock<ITasExplorerViewModel> mockTasExplorerViewModel;

        protected ViewTestSupport()
        {
            var services = new ServiceCollection();
            mockCloudFoundryService = new Mock<ICloudFoundryService>();
            mockErrorDialogService = new Mock<IErrorDialog>();
            mockDialogService = new Mock<IDialogService>();
            mockViewLocatorService = new Mock<IViewLocatorService>();
            mockLoggingService = new Mock<ILoggingService>();
            mockUiDispatcherService = new Mock<IUiDispatcherService>();
            mockThreadingService = new Mock<IThreadingService>();
            mockThemeService = new Mock<IThemeService>();
            mockViewService = new Mock<IViewService>();
            mockTasExplorerViewModel = new Mock<ITasExplorerViewModel>();

            services.AddSingleton(mockCloudFoundryService.Object);
            services.AddSingleton(mockErrorDialogService.Object);
            services.AddSingleton(mockDialogService.Object);
            services.AddSingleton(mockViewLocatorService.Object);
            services.AddSingleton(mockLoggingService.Object);
            services.AddSingleton(mockUiDispatcherService.Object);
            services.AddSingleton(mockThreadingService.Object);
            services.AddSingleton(mockThemeService.Object);
            services.AddSingleton(mockViewService.Object);
            services.AddSingleton(mockTasExplorerViewModel.Object);

            this.services = services.BuildServiceProvider();
        }
    }
}
