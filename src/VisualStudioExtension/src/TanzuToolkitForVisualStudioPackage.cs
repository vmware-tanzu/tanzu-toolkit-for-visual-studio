using Community.VisualStudio.Toolkit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using Tanzu.Toolkit.CloudFoundryApiClient;
using Tanzu.Toolkit.Services;
using Tanzu.Toolkit.Services.CfCli;
using Tanzu.Toolkit.Services.CloudFoundry;
using Tanzu.Toolkit.Services.CommandProcess;
using Tanzu.Toolkit.Services.DataPersistence;
using Tanzu.Toolkit.Services.DebugAgentProvider;
using Tanzu.Toolkit.Services.Dialog;
using Tanzu.Toolkit.Services.DotnetCli;
using Tanzu.Toolkit.Services.ErrorDialog;
using Tanzu.Toolkit.Services.File;
using Tanzu.Toolkit.Services.Logging;
using Tanzu.Toolkit.Services.Project;
using Tanzu.Toolkit.Services.Threading;
using Tanzu.Toolkit.Services.ViewLocator;
using Tanzu.Toolkit.ViewModels;
using Tanzu.Toolkit.ViewModels.AppDeletionConfirmation;
using Tanzu.Toolkit.ViewModels.RemoteDebug;
using Tanzu.Toolkit.VisualStudio.Commands;
using Tanzu.Toolkit.VisualStudio.Services;
using Tanzu.Toolkit.VisualStudio.Views;
using Tanzu.Toolkit.VisualStudio.VSToolWindows;
using Task = System.Threading.Tasks.Task;

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
    [Guid(_packageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(TanzuTasExplorerToolWindow))]
    [ProvideToolWindow(typeof(OutputToolWindow), MultiInstances = true, Transient = true)]
    public sealed class TanzuToolkitForVisualStudioPackage : ToolkitPackage
    {
        /// <summary>
        /// TanzuToolkitPackage GUID string.
        /// </summary>
        public const string _packageGuidString = "9419e55b-9e82-4d87-8ee5-70871b01b7cc";

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

            var commandInitializations = new List<Task>
            {
                Task.Run(() => TanzuTasExplorerCommand.InitializeAsync(this), cancellationToken),
                Task.Run(() => PushToCloudFoundryCommand.InitializeAsync(this, _serviceProvider), cancellationToken),
                Task.Run(() => OpenLogsCommand.InitializeAsync(this, _serviceProvider), cancellationToken),
                Task.Run(() => RequestFeedbackCommand.InitializeAsync(this), cancellationToken),
                Task.Run(() => RemoteDebugCommand.InitializeAsync(this, _serviceProvider), cancellationToken),
            };

            await Task.WhenAll(commandInitializations);
        }

        protected override object GetService(Type serviceType)
        {
            try
            {
                if (_serviceProvider == null)
                {
                    var collection = new ServiceCollection();
                    ConfigureServices(collection);
                    _serviceProvider = collection.BuildServiceProvider();
                }

                var result = _serviceProvider.GetService(serviceType);
                return result ?? base.GetService(serviceType);
            }
            catch (Exception)
            {
                return null;
            }
        }

        protected override WindowPane InstantiateToolWindow(Type toolWindowType)
        {
            return GetService(toolWindowType) as WindowPane;
        }

        private void ConfigureServices(IServiceCollection services)
        {
            var assemblyBasePath = Path.GetDirectoryName(GetType().Assembly.Location);

            /* VSIX package */
            services.AddSingleton<AsyncPackage>(this);

            /* Cloud Foundry API */
            services.AddHttpClient();
            services.AddHttpClient("SslCertTruster", c => { }).ConfigurePrimaryHttpMessageHandler(() =>
            {
                return new HttpClientHandler
                {
                    // trust all certs
                    ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => { return true; }
                };
            });
            services.AddTransient<ICfApiClient, CfApiClient>();

            /* Services */
            services.AddTransient<ICloudFoundryService, CloudFoundryService>();
            services.AddSingleton<IViewLocatorService, VsViewLocatorService>();
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<ICfCliService>(provider => new CfCliService(assemblyBasePath, provider));
            services.AddSingleton<IFileService>(new FileService(assemblyBasePath));
            services.AddSingleton<ILoggingService, LoggingService>();
            services.AddSingleton<IToolWindowService, VsToolWindowService>();
            services.AddSingleton<IThreadingService, ThreadingService>();
            services.AddSingleton<IErrorDialog>(new ErrorDialogService(this));
            services.AddSingleton<IUiDispatcherService, UiDispatcherService>();
            services.AddSingleton<IThemeService>(new ThemeService());
            services.AddTransient<ICommandProcessService, CommandProcessService>();
            services.AddSingleton<ISerializationService, SerializationService>();
            services.AddSingleton<IDataPersistenceService>(provider => new DataPersistenceService(this, provider));
            services.AddSingleton<IDotnetCliService, DotnetCliService>();
            services.AddSingleton<IProjectService, ProjectService>();
            services.AddSingleton<IDebugAgentProvider, VsdbgInstaller>();

            /* Tool Windows */
            services.AddTransient<TanzuTasExplorerToolWindow>();
            services.AddTransient<OutputToolWindow>();

            /* View Models */
            services.AddTransient<IOutputViewModel, OutputViewModel>();
            services.AddSingleton<ITasExplorerViewModel, TasExplorerViewModel>();
            services.AddSingleton<ILoginViewModel, LoginViewModel>();
            services.AddTransient<IDeploymentDialogViewModel, DeploymentDialogViewModel>();
            services.AddTransient<IRemoteDebugViewModel, RemoteDebugViewModel>();
            services.AddTransient<ILoginViewModel, LoginViewModel>();
            services.AddSingleton<IAppDeletionConfirmationViewModel, AppDeletionConfirmationViewModel>();

            /* Views */
            services.AddTransient<IOutputView, OutputView>();
            services.AddSingleton<ILoginView, LoginView>();
            services.AddTransient<ITasExplorerView, TasExplorerView>();
            services.AddTransient<IDeploymentDialogView, DeploymentDialogView>();
            services.AddTransient<IRemoteDebugView, RemoteDebugView>();
            services.AddTransient<IAppDeletionConfirmationView, AppDeletionConfirmationView>();
        }
    }
}