using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Tanzu.Toolkit.Services.CloudFoundry;
using Tanzu.Toolkit.Services.Dialog;
using Tanzu.Toolkit.Services.ViewLocator;
using Tanzu.Toolkit.ViewModels;
using Tanzu.Toolkit.WpfViews;
using Tanzu.Toolkit.WpfViews.Services;
using ServiceCollection = Microsoft.Extensions.DependencyInjection.ServiceCollection;

namespace Tanzu.Toolkit.WpfApp
{
    /// <summary>
    /// Interaction logic for App.xaml.
    /// </summary>
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; }

        // public IConfiguration Configuration { get; private set; }

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

            services.AddTransient<ITasExplorerViewModel, TasExplorerViewModel>();
            services.AddTransient<ITasExplorerView, TasExplorerView>();

            services.AddTransient<ILoginViewModel, LoginViewModel>();
            services.AddTransient<ILoginView, LoginView>();
        }
    }
}
