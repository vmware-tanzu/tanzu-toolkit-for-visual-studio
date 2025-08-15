using Community.VisualStudio.Toolkit;
using Community.VisualStudio.Toolkit.DependencyInjection.Microsoft;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Shell;
using System;
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
using Tanzu.Toolkit.Services.Serialization;
using Tanzu.Toolkit.Services.Threading;
using Tanzu.Toolkit.Services.ViewLocator;
using Tanzu.Toolkit.ViewModels;
using Tanzu.Toolkit.ViewModels.AppDeletionConfirmation;
using Tanzu.Toolkit.ViewModels.RemoteDebug;
using Tanzu.Toolkit.VisualStudio.Commands;
using Tanzu.Toolkit.VisualStudio.Options;
using Tanzu.Toolkit.VisualStudio.Services;
using Tanzu.Toolkit.VisualStudio.Views;
using Tanzu.Toolkit.VisualStudio.VSToolWindows;
using static Tanzu.Toolkit.VisualStudio.Options.OptionsProvider;
using Task = System.Threading.Tasks.Task;

[assembly: ProvideBindingRedirection(AssemblyName = "Microsoft.Extensions.DependencyInjection", NewVersion = "8.0.0.1", OldVersionLowerBound = "0.0.0.0", OldVersionUpperBound = "5.0.0.2", PublicKeyToken = "adb9793829ddae60")]
[assembly: ProvideBindingRedirection(AssemblyName = "Microsoft.Extensions.DependencyInjection.Abstractions", NewVersion = "8.0.0.2", OldVersionLowerBound = "0.0.0.0", OldVersionUpperBound = "5.0.0.2", PublicKeyToken = "adb9793829ddae60")]
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
    [ProvideToolWindow(typeof(TanzuExplorerToolWindow.Pane))]
    [ProvideToolWindow(typeof(OutputToolWindow), MultiInstances = true, Transient = true)]
    [ProvideOptionPage(typeof(General1Options), "Tanzu Toolkit", "General", 0, 0, true)]
    public sealed class TanzuToolkitForVisualStudioPackage : MicrosoftDIToolkitPackage<TanzuToolkitForVisualStudioPackage>
    {
        /// <summary>
        /// TanzuToolkitPackage GUID string.
        /// </summary>
        public const string _packageGuidString = "9419e55b-9e82-4d87-8ee5-70871b01b7cc";

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);
            TanzuExplorerToolWindow.Initialize(this);
            //OutputToolWindow.Initialize(this);
            //await PushToCloudFoundryCommand.InitializeAsync(this, _serviceProvider);
            //await OpenLogsCommand.InitializeAsync(this, _serviceProvider);
            //await RemoteDebugCommand.InitializeAsync(this, _serviceProvider);
            GeneralOptionsModel.Saved += OnSettingsSaved;
        }

        private void OnSettingsSaved(GeneralOptionsModel obj)
        {
            var dataService = ServiceProvider.GetService<IDataPersistenceService>();
            dataService.WriteStringData(nameof(obj.VsdbgLinuxPath), obj.VsdbgLinuxPath);
            dataService.WriteStringData(nameof(obj.VsdbgWindowsPath), obj.VsdbgWindowsPath);
        }

        protected override void InitializeServices(IServiceCollection services)
        {
            var assemblyBasePath = Path.GetDirectoryName(GetType().Assembly.Location);

            // Cloud Foundry API
            services.AddHttpClient();
            services.AddHttpClient("SslCertTruster", c => { }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                // trust all certs
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            });
            services.AddTransient<ICfApiClient, CfApiClient>();

            //Services
            services.AddTransient<ICloudFoundryService, CloudFoundryService>();
            services.AddSingleton<IViewLocatorService, VsViewLocatorService>();
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<ICfCliService>(provider => new CfCliService(assemblyBasePath, provider));
            services.AddSingleton<IFileService>(new FileService(assemblyBasePath));
            services.AddSingleton<ILoggingService, LoggingService>();
            services.AddSingleton<IToolWindowService, VsToolWindowService>();
            services.AddSingleton<IThreadingService, ThreadingService>();
            services.AddSingleton<IErrorDialog, ErrorDialogService>();
            services.AddSingleton<IUIDispatcherService, VisualStudioUIDispatcherService>();
            services.AddSingleton<IThemeService>(new ThemeService());
            services.AddTransient<ICommandProcessService, CommandProcessService>();
            services.AddSingleton<ISerializationService, SerializationService>();
            services.AddSingleton<IDataPersistenceService>(provider => new DataPersistenceService(this, provider));
            services.AddSingleton<IDotnetCliService, DotnetCliService>();
            services.AddSingleton<IProjectService, ProjectService>();
            services.AddSingleton<IDebugAgentProvider, VsdbgInstaller>();

            // Tool Windows
            services.AddTransient<TanzuExplorerToolWindow>();
            services.AddTransient<OutputToolWindow>();

            // View Models
            services.AddTransient<IOutputViewModel, OutputViewModel>();
            services.AddSingleton<ITanzuExplorerViewModel, TanzuExplorerViewModel>();
            services.AddSingleton<ILoginViewModel, LoginViewModel>();
            services.AddTransient<IDeploymentDialogViewModel, DeploymentDialogViewModel>();
            services.AddTransient<IRemoteDebugViewModel, RemoteDebugViewModel>();
            services.AddTransient<ILoginViewModel, LoginViewModel>();
            services.AddSingleton<IAppDeletionConfirmationViewModel, AppDeletionConfirmationViewModel>();

            // Views
            services.AddTransient<IOutputView, OutputView>();
            services.AddSingleton<ILoginView, LoginView>();
            services.AddTransient<ITanzuExplorerView, TanzuExplorerView>();
            services.AddTransient<IDeploymentDialogView, DeploymentDialogView>();
            services.AddTransient<IRemoteDebugView, RemoteDebugView>();
            services.AddTransient<IAppDeletionConfirmationView, AppDeletionConfirmationView>();

            // Commands
            services.RegisterCommands(ServiceLifetime.Singleton);
        }
    }
}