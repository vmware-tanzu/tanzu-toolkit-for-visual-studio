using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace Tanzu.Toolkit.Services.ThemeService
{
    public interface IThemeService
    {
        void SetTheme(FrameworkElement element);
        //string bgbrushString { get; }
        //string txtBrushString { get; }
        //uint MyColor { get; }
    }
}
