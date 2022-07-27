// See https://aka.ms/new-console-template for more information

using System;
using System.Diagnostics;

namespace CfSshWrapper
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 4)
            {
                Environment.Exit(1);
            }

            var pathToCfHome = args[0];
            var pathToCfExe = args[1];
            var appName = args[2];
            var sshCmd = args[3];

            var _cfSshProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = pathToCfExe,
                    Arguments = $"ssh {appName} -c \"{sshCmd}\"",
                }
            };

            _cfSshProcess.StartInfo.EnvironmentVariables["CF_HOME"] = pathToCfHome;

            if (!_cfSshProcess.Start())
            {
                Environment.Exit(2);
            }

            try
            {
                _cfSshProcess.WaitForExit();
            }
            catch
            {
                Environment.Exit(3);
            }
        }
    }
}