using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Windows;
using TanzuForVS.CloudFoundryApiClient;
using TanzuForVS.Services.CloudFoundry;
using TanzuForVS.Services.Dialog;
using TanzuForVS.Services.Locator;
using TanzuForVS.ViewModels;
using TanzuForVS.WpfViews;
using TanzuForVS.WpfViews.Services;

namespace TanzuForWpf
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

            services.AddTransient<ILoginDialogViewModel, LoginDialogViewModel>();
            services.AddTransient<ILoginDialogView, LoginDialogView>();

            HttpClient concreteHttpClient = new HttpClient();
            IUaaClient concreteUaaClient = new UaaClient(concreteHttpClient);
            services.AddSingleton<IUaaClient>(_ => concreteUaaClient);
            services.AddSingleton<ICfApiClient>(_ => new CfApiClient(concreteUaaClient, concreteHttpClient));
        }
    }
}
