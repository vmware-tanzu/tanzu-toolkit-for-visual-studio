using System;
using System.Collections.Generic;
using System.Text;

namespace Tanzu.Toolkit.Services.ThemeService
{
    public interface IThemeService
    {
        string bgbrushString { get; }
        string txtBrushString { get; }
        uint practiceBrush { get; }
        uint MyColor { get; }
    }
}
