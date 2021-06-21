using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
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

        readonly string bgbrushString = backgroundBrush.Name;
        readonly string txtBrushString = textBrush.Name;
    }
}
