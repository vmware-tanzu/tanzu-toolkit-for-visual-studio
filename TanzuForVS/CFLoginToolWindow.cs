namespace TanzuForVS
{
    using System;
    using System.Net.Http;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio.Shell;
    using TanzuForVS.CloudFoundryApiClient;

    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    /// <remarks>
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// <para>
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
    /// implementation of the IVsUIElementPane interface.
    /// </para>
    /// </remarks>
    [Guid("3d369352-09bf-4671-8cdc-21df11ea3ac7")]
    public class TanzuCloudExplorerToolWindow : ToolWindowPane
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TanzuCloudExplorerToolWindow"/> class.
        /// </summary>
        public TanzuCloudExplorerToolWindow() : base(null)
        {
            this.Caption = "Tanzu Cloud Explorer";

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            var globalHttpClient = new HttpClient();
            var globalUaaClient = new UaaClient(globalHttpClient);
            var globalCfApiClient = new CfApiClient(globalUaaClient, globalHttpClient);

            this.Content = new TanzuCloudExplorer(globalCfApiClient);
        }
    }
}
