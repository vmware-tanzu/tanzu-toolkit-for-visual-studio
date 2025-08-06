using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using System.ComponentModel;
using System.Drawing.Design;
using System.Runtime.InteropServices;

namespace Tanzu.Toolkit.VisualStudio.Options
{
    internal partial class OptionsProvider
    {
        [ComVisible(true)]
        public class General1Options : BaseOptionPage<GeneralOptionsModel> { }
    }

    public class GeneralOptionsModel : BaseOptionModel<GeneralOptionsModel>
    {
        [Category("RemoteDebug")]
        [DisplayName("Path to vsdbg")]
        [Description("For airgapped installations, specify the path to a local directory containing vsdbg.")]
        [Editor(typeof(FolderEditor), typeof(UITypeEditor))]
        public string VsdbgPath { get; set; } = string.Empty;
    }
}
