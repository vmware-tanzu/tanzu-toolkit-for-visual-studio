using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;
using Tanzu.Toolkit.VisualStudio.Services.CloudFoundry;
using Tanzu.Toolkit.VisualStudio.Services.Dialog;
using Tanzu.Toolkit.VisualStudio.Services.ViewLocator;
using Tanzu.Toolkit.VisualStudio.ViewModels;
using Tanzu.Toolkit.VisualStudio.WpfViews;
using Tanzu.Toolkit.VisualStudio.WpfViews.Services;

namespace Tanzu.Toolkit.WpfApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; }

        //public IConfiguration Configuration { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // var builder = new ConfigurationBuilder()
            // .SetBasePath(Directory.GetCurrentDirectory())
            // .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            // Configuration = builder.Build();

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            ServiceProvider = serviceCollection.BuildServiceProvider();

            var window = ServiceProvider.GetRequiredService<IMainWindowView>() as Window;
            window?.Show();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ICloudFoundryService, CloudFoundryService>();
            services.AddSingleton<IViewLocatorService, WpfViewLocatorService>();
            services.AddSingleton<IDialogService, WpfDialogService>();

            services.AddTransient<IMainWindowViewModel, MainWindowViewModel>();
            services.AddTransient<IMainWindowView, MainWindowView>();

            services.AddTransient<ICloudExplorerViewModel, CloudExplorerViewModel>();
            services.AddTransient<ICloudExplorerView, CloudExplorerView>();

            services.AddTransient<IAddCloudDialogViewModel, AddCloudDialogViewModel>();
            services.AddTransient<IAddCloudDialogView, AddCloudDialogView>();
        }
    }
}
