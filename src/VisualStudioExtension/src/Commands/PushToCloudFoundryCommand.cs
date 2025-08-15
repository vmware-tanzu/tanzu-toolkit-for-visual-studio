using Community.VisualStudio.Toolkit;
using Community.VisualStudio.Toolkit.DependencyInjection;
using Community.VisualStudio.Toolkit.DependencyInjection.Core;
using EnvDTE;
using EnvDTE80;
using Microsoft;
using Microsoft.VisualStudio.Shell;
using Serilog;
using System;
using System.IO;
using Tanzu.Toolkit.Services.ErrorDialog;
using Tanzu.Toolkit.Services.Logging;
using Tanzu.Toolkit.Services.Project;
using Tanzu.Toolkit.ViewModels;
using Tanzu.Toolkit.VisualStudio.Services;
using Tanzu.Toolkit.VisualStudio.Views;
using Project = EnvDTE.Project;
using Task = System.Threading.Tasks.Task;

namespace Tanzu.Toolkit.VisualStudio.Commands
{
    [Command(PackageGuids.guidTanzuToolkitPackageCmdSetString, PackageIds.PushToCloudFoundryCommandId)]
    internal sealed class PushToCloudFoundryCommand : BaseDICommand
    {
        private readonly IErrorDialog _dialogService;
        private readonly IProjectService _projectService;
        private readonly ILogger _logger;

        public PushToCloudFoundryCommand(DIToolkitPackage package, IErrorDialog errorDialog, ILoggingService loggingService, IProjectService projectService)
            : base(package)
        {
            _dialogService = errorDialog;
            _logger = loggingService.Logger;
            _projectService = projectService;
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(Package.DisposalToken);

            try
            {
                var dte = await ServiceProvider.GetGlobalServiceAsync(typeof(DTE)) as DTE2;
                Assumes.Present(dte);

                var activeProjects = (Array)dte.ActiveSolutionProjects;

                foreach (Project project in activeProjects)
                {
                    var projectDirectory = Path.GetDirectoryName(project.FullName) ?? throw new Exception($"Unable to locate project directory from '{project.FullName}'");

                    string frameworkMoniker = null;
                    try
                    {
                        frameworkMoniker = project.Properties.Item("FriendlyTargetFramework").Value.ToString();
                    }
                    catch (ArgumentException)
                    {
                        frameworkMoniker = project.Properties.Item("TargetFrameworkMoniker").Value.ToString();
                    }
                    finally
                    {
                        if (string.IsNullOrEmpty(frameworkMoniker))
                        {
                            _logger.Warning("Unable to identify target framework");
                        }

                        if (frameworkMoniker != null && frameworkMoniker.StartsWith(".NETFramework") && !File.Exists(Path.Combine(projectDirectory, "Web.config")))
                        {
                            _dialogService.DisplayErrorDialog("Unable to push to Tanzu Platform",
                                $"This project appears to target .NET Framework; pushing it to Tanzu Platform requires a 'Web.config' file at it's base directory, but none was found in {projectDirectory}");
                        }
                        else
                        {
                            _projectService.ProjectName = project.Name;
                            _projectService.PathToProjectDirectory = projectDirectory;
                            _projectService.TargetFrameworkMoniker = frameworkMoniker;

                            var serviceProvider = await
                                VS.GetServiceAsync<SToolkitServiceProvider<TanzuToolkitForVisualStudioPackage>, IToolkitServiceProvider<TanzuToolkitForVisualStudioPackage>>();
                            IDeploymentDialogViewModel remoteDebugViewModel = new DeploymentDialogViewModel(serviceProvider);
                            var view = new DeploymentDialogView(remoteDebugViewModel, new ThemeService());
                            view.DisplayView();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                _logger.Error("{ClassName} caught exception in {MethodName}: {PushException}",
                    nameof(PushToCloudFoundryCommand), nameof(Execute), ex);
                _dialogService.DisplayErrorDialog("Unable to push to Tanzu Platform", $"Internal error: \"{ex.Message}\"");
            }
        }
    }
}