using System;
using System.Globalization;
using System.Windows.Data;

namespace Tanzu.Toolkit.VisualStudio.Views.Converters
{
    public class IntToBoolConverter : IValueConverter
    {
        public int ExpectedInt { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value as int?) == ExpectedInt;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new InvalidOperationException("Visibility Converter can only be used OneWay.");
        }
    }
}
