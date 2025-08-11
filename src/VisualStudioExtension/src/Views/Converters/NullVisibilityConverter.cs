using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Tanzu.Toolkit.VisualStudio.Views.Converters
{
    public class NullVisibilityConverter : IValueConverter
    {
        public bool Reversed { get; set; }
        public bool ReserveSpace { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null
                ? Reversed ? Visibility.Visible : ReserveSpace ? Visibility.Hidden : (object)Visibility.Collapsed
                : Reversed
                    ? ReserveSpace ? Visibility.Hidden : (object)Visibility.Collapsed
                    : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new InvalidOperationException("Visibility Converter can only be used OneWay.");
        }
    }
}