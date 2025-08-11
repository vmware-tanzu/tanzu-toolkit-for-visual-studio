using Community.VisualStudio.Toolkit;
using System.ComponentModel;
using System.Drawing.Design;
using System.Runtime.InteropServices;

namespace Tanzu.Toolkit.VisualStudio.Options
{
    internal partial class OptionsProvider
    {
        [ComVisible(true)]
        public class General1Options : BaseOptionPage<GeneralOptionsModel>
        {
        }
    }

    public class GeneralOptionsModel : BaseOptionModel<GeneralOptionsModel>
    {
        [Category("Remote Debugging")]
        [DisplayName("Path to vsdbg (apps on Linux)")]
        [Description(
            "For airgapped installations, specify the path to a local directory containing vsdbg for applications running on Linux.")]
        [Editor(typeof(FolderEditor), typeof(UITypeEditor))]
        public string VsdbgLinuxPath { get; set; } = string.Empty;

        [Category("Remote Debugging")]
        [DisplayName("Path to vsdbg (apps on Windows)")]
        [Description(
            "For airgapped installations, specify the path to a local directory containing vsdbg for applications running on Windows.")]
        [Editor(typeof(FolderEditor), typeof(UITypeEditor))]
        public string VsdbgWindowsPath { get; set; } = string.Empty;
    }
}