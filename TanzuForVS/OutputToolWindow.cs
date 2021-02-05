using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;
using Tanzu.Toolkit.VisualStudio.WpfViews;

namespace Tanzu.Toolkit.VisualStudio
{
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
    [Guid("1c563078-79b7-4b16-842f-d85ba441e92e")]
    public class OutputToolWindow : ToolWindowPane
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OutputToolWindow"/> class.
        /// </summary>
        public OutputToolWindow(IOutputView view) : base(null)
        {
            Caption = "Tanzu Output";

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            Content = view;
        }
    }
}
