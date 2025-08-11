using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Tanzu.Toolkit.VisualStudio.Views.Converters
{
    public class VisibilityConverter : IValueConverter
    {
        public bool Reversed { get; set; }
        public bool ReserveSpace { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value as bool? != Reversed)
            {
                return Visibility.Visible;
            }
            else
            {
                return ReserveSpace ? Visibility.Hidden : (object)Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new InvalidOperationException("Visibility Converter can only be used OneWay.");
        }
    }
}