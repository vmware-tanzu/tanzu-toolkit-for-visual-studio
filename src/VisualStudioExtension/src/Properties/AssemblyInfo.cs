using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;

[assembly: ComVisible(false)]

[assembly: ProvideCodeBase(CodeBase = "$PackageFolder$\\Community.VisualStudio.Toolkit.DependencyInjection.Core.dll")]
[assembly: ProvideCodeBase(CodeBase = "$PackageFolder$\\Community.VisualStudio.Toolkit.DependencyInjection.Microsoft.dll")]
[assembly:
    ProvideBindingRedirection(AssemblyName = "Microsoft.Extensions.DependencyInjection", NewVersion = "8.0.0.1", OldVersionLowerBound = "0.0.0.0", OldVersionUpperBound = "5.0.0.2",
        PublicKeyToken = "adb9793829ddae60")]
[assembly:
    ProvideBindingRedirection(AssemblyName = "Microsoft.Extensions.DependencyInjection.Abstractions", NewVersion = "8.0.0.2", OldVersionLowerBound = "0.0.0.0", OldVersionUpperBound = "5.0.0.2",
        PublicKeyToken = "adb9793829ddae60")]