using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Tanzu.Toolkit.Models;
using Tanzu.Toolkit.Services;

namespace Tanzu.Toolkit.ViewModels
{
    public class OutputViewModel : AbstractViewModel, IOutputViewModel
    {
        private string _outputContent;
        private bool _outputPaused;
        private bool _outputIsAppLogs = false;
        private CloudFoundryApp _app;
        private IView _view;
        private string _lastLinePrinted;
        private readonly ITasExplorerViewModel _tasExplorerViewModel;

        public OutputViewModel(ITasExplorerViewModel tasExplorerViewModel, IServiceProvider services) : base(services)
        {
            _tasExplorerViewModel = tasExplorerViewModel;
        }

        public string OutputContent
        {
            get => _outputContent;

            set
            {
                _outputContent = value;
                RaisePropertyChangedEvent("OutputContent");
            }
        }

        public Process ActiveProcess { get; set; }

        public bool OutputIsAppLogs
        {
            get => _outputIsAppLogs;
            set
            {
                _outputIsAppLogs = value;
                RaisePropertyChangedEvent("OutputIsAppLogs");
            }
        }

        public bool OutputPaused
        {
            get => _outputPaused;
            set
            {
                _outputPaused = value;
                RaisePropertyChangedEvent("OutputPaused");
            }
        }

        public void AppendLine(string newContent)
        {
            if (!OutputPaused && !newContent.StartsWith("Retrieving logs for app"))
            {
                var newLine = $"{newContent}\n";
                if (!string.IsNullOrWhiteSpace(newLine))
                {
                    OutputContent += newLine;
                }

                // record last line of logs output to help determine
                // "recent" lines to print when resuming paused logs
                if (!newLine.StartsWith("\n***") && !newLine.StartsWith("***"))
                {
                    _lastLinePrinted = newLine;
                }
            }
        }

        public void CancelActiveProcess(object arg = null)
        {
            ActiveProcess?.Kill();
            ActiveProcess?.Dispose();
        }

        public void ClearContent(object arg = null)
        {
            OutputContent = string.Empty;
        }

        public void PauseOutput(object arg = null)
        {
            OutputPaused = true;
            if (OutputIsAppLogs)
            {
                // stop app logs
                CancelActiveProcess();
                AppendLine("*** App logs paused ***\n");
            }
        }

        public void ResumeOutput(object arg = null)
        {
            OutputPaused = false;
            if (OutputIsAppLogs)
            {
                // start streaming app logs
                var _ = BeginStreamingAppLogsForAppAsync(_app, _view);
            }
        }

        public async Task BeginStreamingAppLogsForAppAsync(CloudFoundryApp cfApp, IView outputView)
        {
            _app = cfApp;
            _view = outputView;
            var recentLogsTask = CfClient.GetRecentLogsAsync(cfApp);
            OutputIsAppLogs = true;
            outputView.DisplayView();
            AppendLine($"\n*** Fetching recent app logs for \"{cfApp.AppName}\" in org \"{cfApp.ParentSpace.ParentOrg.OrgName}\" and space {cfApp.ParentSpace.SpaceName}... ***");

            var recentLogsResult = await recentLogsTask;
            if (recentLogsResult.Succeeded)
            {
                var recentLines = recentLogsResult.Content;
                if (!string.IsNullOrWhiteSpace(_lastLinePrinted) && recentLines.Contains(_lastLinePrinted))
                {
                    var newerLines = recentLines.Substring(recentLines.IndexOf(_lastLinePrinted) + _lastLinePrinted.Length);
                    AppendLine(newerLines);
                }
                else
                {
                    AppendLine(recentLines);
                }
                AppendLine($"\n*** End of recent logs, beginning live log stream for \"{cfApp.AppName}\" in org \"{cfApp.ParentSpace.ParentOrg.OrgName}\" and space {cfApp.ParentSpace.SpaceName}...***");
            }
            else
            {
                if (recentLogsResult.FailureType == FailureType.InvalidRefreshToken)
                {
                    _tasExplorerViewModel.AuthenticationRequired = true;
                    AppendLine("\n*** Unable to fetch recent logs; authentication requied. Please log in to TAS and try again. ***\n");
                    return;
                }
                else
                {
                    Logger.Error($"Unable to retrieve recent logs for {cfApp.AppName}. {recentLogsResult.Explanation}. {recentLogsResult.CmdResult}");
                    AppendLine("\n*** Unable to fetch recent logs, attempting to start live log stream... ***\n");
                }
            }

            var logStreamResult = CfClient.StreamAppLogs(cfApp, stdOutCallback: AppendLine, stdErrCallback: AppendLine);
            if (logStreamResult.Succeeded)
            {
                ActiveProcess = logStreamResult.Content;
            }
            else
            {
                if (logStreamResult.FailureType == FailureType.InvalidRefreshToken)
                {
                    _tasExplorerViewModel.AuthenticationRequired = true;
                }
                ErrorService.DisplayErrorDialog("Error displaying app logs", $"Something went wrong while trying to display logs for {cfApp.AppName}. Please try again -- if this issue persists, contact tas-vs-extension@vmware.com.");
            }
        }
    }
}
