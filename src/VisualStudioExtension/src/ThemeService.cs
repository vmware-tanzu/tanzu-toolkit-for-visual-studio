using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzu.Toolkit.Services.ThemeService;

namespace Tanzu.Toolkit.VisualStudio
{
    public class ThemeService : TanzuToolkitForVisualStudioPackage, IThemeService
    {
        static public ThemeResourceKey backgroundBrush { get; private set; } = EnvironmentColors.ToolWindowBackgroundBrushKey;
        static public ThemeResourceKey textBrush { get; private set; } = EnvironmentColors.ToolWindowTextBrushKey;


        Guid bgGuid = Guid.Parse("624ed9c3-bdfd-41fa-96c3-7c824ea32e3d");
        string bgName = "ToolWindowBackground";
        uint bgType = (uint)backgroundBrush.KeyType;

        public string bgbrushString { get; } = backgroundBrush.Name;
        public string txtBrushString { get; } = textBrush.Name;

        public uint practiceBrush { get 
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                var service = GetService(typeof(SVsUIShell)) as IVsUIShell6;
                uint colorVal = service.GetThemedColor(bgGuid, bgName, bgType);
                return colorVal;
            } }

        public uint MyColor { get; }

        public ThemeService(uint bgColor)
        {
            MyColor = bgColor;
        }
    }
}
