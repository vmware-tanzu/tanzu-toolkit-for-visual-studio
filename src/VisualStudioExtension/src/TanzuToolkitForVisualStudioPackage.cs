using System;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using EnvDTE;
using EnvDTE80;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Shell;
using Tanzu.Toolkit.CloudFoundryApiClient;
using Tanzu.Toolkit.Services;
using Tanzu.Toolkit.Services.CfCli;
using Tanzu.Toolkit.Services.CloudFoundry;
using Tanzu.Toolkit.Services.CmdProcess;
using Tanzu.Toolkit.Services.Dialog;
using Tanzu.Toolkit.Services.ErrorDialog;
using Tanzu.Toolkit.Services.FileLocator;
using Tanzu.Toolkit.Services.Logging;
using Tanzu.Toolkit.Services.Threading;
using Tanzu.Toolkit.Services.ViewLocator;
using Tanzu.Toolkit.ViewModels;
using Tanzu.Toolkit.VisualStudio.Commands;
using Tanzu.Toolkit.VisualStudio.WpfViews.Services;
using Tanzu.Toolkit.WpfViews;
using Tanzu.Toolkit.WpfViews.Services;
using Task = System.Threading.Tasks.Task;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft;
using Tanzu.Toolkit.WpfViews.ThemeService;

namespace Tanzu.Toolkit.VisualStudio
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(TanzuTasExplorerToolWindow))]
    [ProvideToolWindow(typeof(OutputToolWindow))]
    public sealed class TanzuToolkitForVisualStudioPackage : AsyncPackage
    {
        /// <summary>
        /// TanzuToolkitPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "9419e55b-9e82-4d87-8ee5-70871b01b7cc";

        private IServiceProvider _serviceProvider;
   
       
        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            await TanzuTasExplorerCommand.InitializeAsync(this);
            await PushToCloudFoundryCommand.InitializeAsync(this, _serviceProvider);
            await OutputWindowCommand.InitializeAsync(this);
            await OpenLogsCommand.InitializeAsync(this, _serviceProvider);

        }

        protected override object GetService(Type serviceType)
        {
            if (_serviceProvider == null)
            {
                var collection = new ServiceCollection();
                ConfigureServices(collection);
                _serviceProvider = collection.BuildServiceProvider();
            }

            var result = _serviceProvider.GetService(serviceType);
            if (result != null)
            {
                return result;
            }

            return base.GetService(serviceType);
        }

        protected override WindowPane InstantiateToolWindow(Type toolWindowType)
        {
            return GetService(toolWindowType) as WindowPane;
        }

        private void ConfigureServices(IServiceCollection services)
        {
            string assemblyBasePath = Path.GetDirectoryName(GetType().Assembly.Location);

            /* VSIX package */
            services.AddSingleton<AsyncPackage>(this);

            /* Cloud Foundry API */
            HttpClient httpClient = new HttpClient();
            IUaaClient uaaClient = new UaaClient(httpClient);
            services.AddSingleton(_ => uaaClient);
            services.AddSingleton<ICfApiClient>(_ => new CfApiClient(uaaClient, httpClient));

            /* Services */
            services.AddSingleton<ICloudFoundryService, CloudFoundryService>();
            services.AddSingleton<IViewLocatorService, WpfViewLocatorService>();
            services.AddSingleton<IDialogService, WpfDialogService>();
            services.AddSingleton<ICfCliService>(provider => new CfCliService(assemblyBasePath, provider));
            services.AddSingleton<IFileLocatorService>(new FileLocatorService(assemblyBasePath));
            services.AddSingleton<ILoggingService, LoggingService>();
            services.AddSingleton<IViewService, VsToolWindowService>();
            services.AddSingleton<IThreadingService, ThreadingService>();
            services.AddSingleton<IErrorDialog>(new ErrorDialogWindowService(this));
            services.AddSingleton<IUiDispatcherService, UiDispatcherService>();

            services.AddSingleton<IThemeService>(new ThemeService());

            services.AddTransient<ICmdProcessService, CmdProcessService>();

            /* Tool Windows */
            services.AddTransient<TanzuTasExplorerToolWindow>();
            services.AddTransient<OutputToolWindow>();

            /* View Models */
            services.AddSingleton<IOutputViewModel, OutputViewModel>();
            services.AddSingleton<ITasExplorerViewModel, TasExplorerViewModel>();

            services.AddTransient<IDeploymentDialogViewModel, DeploymentDialogViewModel>();
            services.AddTransient<IAddCloudDialogViewModel, AddCloudDialogViewModel>();

            /* Views */
            services.AddSingleton<IOutputView, OutputView>();

            services.AddTransient<ITasExplorerView, TasExplorerView>();
            services.AddTransient<IDeploymentDialogView, DeploymentDialogView>();
            services.AddTransient<IAddCloudDialogView, LoginView>();
            
        }
    }
}