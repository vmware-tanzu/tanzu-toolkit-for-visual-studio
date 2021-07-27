﻿using System;
using System.Diagnostics;
using System.IO;
using Tanzu.Toolkit.ViewModels;

namespace Tanzu.Toolkit.WpfApp
{
    public class MainWindowViewModel : AbstractViewModel, IMainWindowViewModel
    {
        private string _commandInput;

        private string _commandOutput = "Initial value (delete me)";

        private string _commandStdOut;

        private string _commandStdErr;

        public MainWindowViewModel(IServiceProvider services)
            : base(services)
        {
        }

        public string CommandInput
        {
            get
            {
                return _commandInput;
            }
            set
            {
                _commandInput = value;
                RaisePropertyChangedEvent("CommandInput");
            }
        }

        public string CommandOutput
        {
            get
            {
                return _commandOutput;
            }
            set
            {
                _commandOutput = value;
                RaisePropertyChangedEvent("CommandOutput");
            }
        }

        public string CommandStdOut
        {
            get
            {
                return _commandStdOut;
            }

            set
            {
                _commandStdOut = value;
                RaisePropertyChangedEvent("CommandStdOut");
            }
        }

        public string CommandStdErr
        {
            get
            {
                return _commandStdErr;
            }
            set
            {
                _commandStdErr = value;
                RaisePropertyChangedEvent("CommandStdErr");
            }
        }

        public bool CanOpenTasExplorer(object arg)
        {
            return true;
        }

        public void OpenTasExplorer(object arg)
        {
            ActiveView = ViewLocatorService.NavigateTo(typeof(TasExplorerViewModel).Name);
        }

        public bool CanInvokeCommandPrompt(object arg)
        {
            return true;
        }

        public bool CanInvokeCfCli(object arg)
        {
            return true;
        }

        public void InvokeCommandPrompt(object arg)
        {
            CommandOutput = "";
            CommandStdOut = "";
            CommandStdErr = "";

            ExecuteWindowlessCommand(arguments: CommandInput, workingDir: @"C:\Users\awoosnam\source\repos\SampleNETCoreWebApp");
        }

        public void InvokeCfCli(object arg)
        {
            CommandOutput = "";
            CommandStdOut = "";
            CommandStdErr = "";

            ExecuteCfCliCommand(arguments: CommandInput, workingDir: @"C:\Users\awoosnam\source\repos\SampleNETCoreWebApp", cliVersion: 6);
        }

        private void ExecuteCfCliCommand(string arguments, string workingDir, int cliVersion)
        {
            const string pathToCfCliV6 = @"C:\Program Files\Cloud Foundry\v6";
            const string pathToCfCliV7 = @"C:\Program Files\Cloud Foundry\v7";
            string cfCliCommandName;

            if (cliVersion == 6)
            {
                cfCliCommandName = Path.GetFullPath(pathToCfCliV6 + @"\cf.exe");
            }
            else if (cliVersion == 7)
            {
                cfCliCommandName = Path.GetFullPath(pathToCfCliV7 + @"\cf.exe");
            }
            else
            {
                return;
            }

            if (arguments.StartsWith("cf "))
            {
                arguments = arguments.Remove(0, 3);
            }

            string commandStr = '"' + cfCliCommandName + '"' + ' ' + arguments;
            ExecuteWindowlessCommand(commandStr, workingDir);
        }

        private void ExecuteWindowlessCommand(string arguments, string workingDir)
        {
            // * Create your Process
            Process process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = "/c " + arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.WorkingDirectory = workingDir;

            // * Set your output and error (asynchronous) handlers
            process.OutputDataReceived += new DataReceivedEventHandler(OutputHandler);
            process.ErrorDataReceived += new DataReceivedEventHandler(ErrorHandler);

            // * Start process and handlers
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }

        private void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            // * Do your stuff with the output (write to console/log/StringBuilder)
            if (outLine.Data != null)
            {
                CommandOutput += "> " + outLine.Data + '\n';
            }
        }

        private void ErrorHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            // * Do your stuff with the output (write to console/log/StringBuilder)
            if (outLine.Data != null)
            {
                CommandOutput += "###ERROR###: " + outLine.Data + '\n';
            }
        }
    }
}
